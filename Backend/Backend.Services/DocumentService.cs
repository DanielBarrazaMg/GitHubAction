using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Backend.Common.Interfaces.DataAccess;
using Backend.Common.Models;
using Backend.Common.Services;
using Backend.Entities;
using Backend.Models.Out;
using Backend.Services.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Backend.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDataAccess _dataAccess;
        private readonly ILogger<DocumentService> _logger;
        private readonly string _storageConnectionString;
        private readonly string _pendingContainer;
        private readonly string _processedContainer;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _documentAIEndpoint;
        private readonly string _documentAIKey;
        private readonly string _documentAIModelName;
        private const int SAS_TOKEN_EXPIRATION_IN_MINUTES = 60;

        /// <inheritdoc/>
        public DocumentService(IDataAccess dataAccess, ILogger<DocumentService> logger, IConfiguration configuration)
        {
            _dataAccess = dataAccess;
            _logger = logger;

            // TODO: Validar
            //_storageConnectionString = configuration.GetValue<string>("blob:ConnectionString"); // Lee desde el AppConfiguration 
            _storageConnectionString = configuration.GetValue<string>("StorageConnectionString"); // Lee desde el AppSettings
            _blobServiceClient = new BlobServiceClient(_storageConnectionString);
            _pendingContainer = configuration.GetValue<string>("blob:PendingFolder");
            _processedContainer = configuration.GetValue<string>("blob:ProcessedFolder");
            _documentAIEndpoint = configuration.GetValue<string>("documentAI:Endpoint");
            _documentAIKey = configuration.GetValue<string>("documentAI:APIKey");
            _documentAIModelName = configuration.GetValue<string>("documentAI:ModelName");
        }

        /// <inheritdoc/>
        public async Task<Result<string>> UploadDocumentAsync(string fileName, Stream fileStream, string contentType)
        {
            try
            {
                // Save entity in the database
                var documentId = Guid.NewGuid();
                var document = new Document
                {
                    Id = documentId,
                    Name = fileName
                };
                await _dataAccess.Documents.InsertAsync(document);
                await _dataAccess.SaveChangesAsync();

                // Updload file to the storage
                var sasToken = GetSasToken(_pendingContainer, SAS_TOKEN_EXPIRATION_IN_MINUTES);
                var uri = await SaveFileAsyncAsync(_pendingContainer, documentId.ToString(), fileStream, contentType, Path.GetExtension(fileName));

                // Update entity with the uri
                document.Url = uri;
                _dataAccess.Documents.Update(document);
                await _dataAccess.SaveChangesAsync();

                // Create Response
                return new Result<string> { Success = true, Message = "Document uploaded successfully.", Data = uri};
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return ex.CreateResultFromException<string>();
            }
        }

        /// <inheritdoc/>
        public async Task<Result> ProcessDocumentAsync(string filename)
        {
            try
            {
                Uri fileUri = GetFileUri(filename, _pendingContainer);

                // Analyze the document by calling Document AI
                var fields = AnalyzeDocumentAsync(fileUri);

                var documentId = GetDocumentId(fileUri.ToString());
                var documentEntity = await _dataAccess.Documents.GetAsync(documentId);

                var serializerOptions = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var serializedFields = JsonSerializer.Serialize(fields?.Result, serializerOptions);
                documentEntity.Fields = serializedFields;                

                // Move the file to the processed container
                var newUri = await MoveFileToProcessedContainerAsync(filename, _processedContainer);

                // Update the document entity with the fields
                documentEntity.Status = DocumentStatus.Processed;
                documentEntity.Url = newUri;
                _dataAccess.Documents.Update(documentEntity);
                await _dataAccess.SaveChangesAsync();

                // Create Response
                return new Result<string> { Success = true, Message = "Document processed successfully." };

            }
            catch (Exception ex)
            {
                // Log the error that occurred while processing the document
                _logger.LogError($"An error occurred while processing the document: {filename}. {ex.Message}", ex);
                return ex.CreateResultFromException<string>();
            }
        }

        /// <inheritdoc/>
        public async Task<Result<DocumentModelOut>> GetDocumentAsync(Guid documentId)
        {
            var document = await _dataAccess.Documents.GetAsync(documentId);

            var sasToken = GetSasToken(GetContainerName(document.Url), SAS_TOKEN_EXPIRATION_IN_MINUTES);

            var documentModel = new DocumentModelOut
            {
                Name = document.Name,
                Status = document.Status.ToString(),
                Url = $"{document.Url}?{sasToken}",
                Fields = document.Fields
            };

            return new Result<DocumentModelOut> { Success = true, Data = documentModel };
        }

        /// <inheritdoc/>
        public async Task<Result<IEnumerable<DocumentModelOut>>> GetDocumentsAsync(int page, int pageSize)
        {
            // Get all documents from the database and paginate the results using page and pageSize
            var documents = await _dataAccess.Documents
                .GetAsync(filter: null, orderBy: x => x.OrderBy(y => y.Id), includeProperties: string.Empty, page: page,  pageSize: pageSize);

            var docuemntModel = documents
                .Select(x => new DocumentModelOut
                {
                    Fields = x.Fields,
                    Name  = x.Name,
                    Status = x.Status.ToString(),
                    Url = x.Url,
                });

            return new Result<IEnumerable<DocumentModelOut>> { Success = true, Data = docuemntModel };
        }

        private async Task<string> MoveFileToProcessedContainerAsync(string filename, string processedContainer)
        {
            // Get references to the source and destination containers
            var pendingContainerClient = _blobServiceClient.GetBlobContainerClient(_pendingContainer);
            var processedContainerClient = _blobServiceClient.GetBlobContainerClient(processedContainer);

            await processedContainerClient.CreateIfNotExistsAsync();

            // Get a reference to the blob to be moved
            var blobClient = pendingContainerClient.GetBlobClient(filename);

            // Check if the blob exists in the source container
            if (await blobClient.ExistsAsync())
            {
                // Copy the blob to the destination container
                var newBlobClient = processedContainerClient.GetBlobClient(filename);
                await newBlobClient.StartCopyFromUriAsync(blobClient.Uri);

                // Delete the original blob from the source container
                await blobClient.DeleteIfExistsAsync();

                _logger.LogInformation($"Blob '{filename}' moved from '{_pendingContainer}' to '{processedContainer}'.");

                return newBlobClient.Uri?.ToString();
            }
            else
            {
                _logger.LogWarning($"Blob '{filename}' not found in container '{_pendingContainer}'.");
                throw new ArgumentNullException($"Blob '{filename}' not found in container '{_pendingContainer}'.");
            }
        }

        private string GetSasToken(string container, int expiresOnMinutes)
        {
            var accountKey = string.Empty;
            var accountName = string.Empty;
            var connectionStringValues = _storageConnectionString.Split(';')
                .Select(s => s.Split(['='], 2))
                .ToDictionary(s => s[0], s => s[1]);
            if (connectionStringValues.TryGetValue("AccountName", out var accountNameValue) && !string.IsNullOrWhiteSpace(accountNameValue)
                && connectionStringValues.TryGetValue("AccountKey", out var accountKeyValue) && !string.IsNullOrWhiteSpace(accountKeyValue))
            {
                accountKey = accountKeyValue;
                accountName = accountNameValue;

                var storageSharedKeyCredential = new StorageSharedKeyCredential(accountName, accountKey);
                var blobSasBuilder = new BlobSasBuilder()
                {
                    BlobContainerName = container,
                    ExpiresOn = DateTime.UtcNow + TimeSpan.FromMinutes(expiresOnMinutes)
                };

                blobSasBuilder.SetPermissions(BlobAccountSasPermissions.All);
                var queryParams = blobSasBuilder.ToSasQueryParameters(storageSharedKeyCredential);
                var sasToken = queryParams.ToString();
                return sasToken;
            }
            return string.Empty;
        }

        private async Task<string> SaveFileAsyncAsync(string container, string fileName, Stream file, string contentType, string extension)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            var imagesContainerClient = _blobServiceClient.GetBlobContainerClient(container);
            await imagesContainerClient.CreateIfNotExistsAsync();

            var blobClient = imagesContainerClient.GetBlobClient($"{fileName}{extension}");

            // Asegurarse de que la posición del stream esté al inicio
            file.Position = 0;

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders()
            };
            uploadOptions.HttpHeaders.ContentType = contentType;

            await blobClient.UploadAsync(file, uploadOptions);
            return blobClient.Uri?.ToString();
        }

        private Uri GetFileUri(string fileName, string containerName)
        {
            // Get the SAS token for the container
            string sasToken = GetSasToken(containerName, SAS_TOKEN_EXPIRATION_IN_MINUTES);

            // Construct the URI for the blob including the SAS token
            UriBuilder uriBuilder = new()
            {
                Scheme = "https", // Use HTTPS scheme for secure communication
                Host = $"{_blobServiceClient.AccountName}.blob.core.windows.net", // Specify the Azure Blob Storage account host
                Path = $"{containerName}/{fileName}", // Combine the container name and file name to form the path
                Query = sasToken // Append the SAS token as query parameter to the URI
            };

            return uriBuilder.Uri; // Return the constructed URI
        }

        public async Task<Dictionary<string, object>> AnalyzeDocumentAsync(Uri fileUri)
        {
            var camposVariables = new Dictionary<string, object>();

            string modelId = _documentAIModelName;
            // TODO: Existe otro modelo para las declaraciones. El nombre del modelo es: DecJurada1915012

            AzureKeyCredential credential = new AzureKeyCredential(_documentAIKey);
            DocumentAnalysisClient client = new DocumentAnalysisClient(new Uri(_documentAIEndpoint), credential);

            AnalyzeDocumentOperation operation = await client.AnalyzeDocumentFromUriAsync(WaitUntil.Completed, modelId, fileUri);
            AnalyzeResult result = operation.Value;

            _logger.LogInformation($"Document was analyzed with model with ID: {result.ModelId}");

            foreach (AnalyzedDocument document in result.Documents)
            {
                Console.WriteLine($"Document of type: {document.DocumentType}");

                foreach (KeyValuePair<string, DocumentField> fieldKvp in document.Fields)
                {
                    string fieldName = fieldKvp.Key;
                    DocumentField field = fieldKvp.Value;

                    _logger.LogInformation($"Field '{fieldName}': ");

                    _logger.LogInformation($"  Content: '{field.Content}'");
                    _logger.LogInformation($"  Confidence: '{field.Confidence}'");

                    camposVariables.Add(fieldName, field.Content);
                }
            }

            return camposVariables;
        }

        private Guid GetDocumentId(string fileUrl)
        {
            var lastSlashIndex = fileUrl.LastIndexOf('/');
            var lastDotIndex = fileUrl.LastIndexOf('.');
            var uniqueId = fileUrl.Substring(lastSlashIndex + 1, lastDotIndex - lastSlashIndex - 1);
            return Guid.Parse(uniqueId);
        }

        private string GetContainerName(string fileUrl)
        {
            Uri uri = new Uri(fileUrl);
            string[] segments = uri.Segments;
            if (segments.Length >= 2)
            {
                return segments[^2].TrimEnd('/');
            }
            else
            {
                throw new ArgumentException("URL no válida");
            }
        }
    }
}