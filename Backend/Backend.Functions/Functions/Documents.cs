using Backend.Common.Extensions;
using Backend.Common.Services;
using Backend.Models.In;
using Backend.Models.Out;
using HttpMultipartParser;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;


namespace Backend.Functions.Functions
{
    /// <summary>
    /// Documents backend API
    /// </summary>
    public class Documents
    {
        private readonly ILogger<Documents> _logger;
        private readonly IDocumentService _documentService;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Receive all the dependencies by DI
        /// </summary>        
        public Documents(IDocumentService businessLogic, ILogger<Documents> logger, IConfiguration configuration)
        {
            _logger = logger;
            _documentService = businessLogic;
            _configuration = configuration;
        }

        /// <summary>
        /// Upload a document to process
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Function(nameof(UploadDocument))]
        [OpenApiOperation(operationId: "run", tags: ["multipartformdata"], Summary = "Upload document to process", Description = "Upload document to process", Visibility = OpenApiVisibilityType.Advanced)]
        [OpenApiSecurity("X-Functions-Key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header, Description = "The function key to access the API")]
        [OpenApiRequestBody(contentType: "multipart/form-data", bodyType: typeof(DocumentUploadIn), Required = true, Description = "Document to upload")]
        public async Task<HttpResponseData> UploadDocument([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "documents")] HttpRequestData request)
        {
            try
            {
                var body = await MultipartFormDataParser.ParseAsync(request.Body);
                var file = body.Files[0];

                var result = await _documentService.UploadDocumentAsync(file.FileName, file.Data, file.ContentType);
                return await request.CreateResponseAsync(result, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Process a new email uploaded to the storage container.
        /// </summary>
        [Function(nameof(ProcessFileFromStorageAsync))]
        //TODO: Si no es posible obtener el connection string desde el App Configuration, es mejor unificar la lógica para no tener código que lee desde app configuration y otro desde el archivo de configuración.
        // Pasa lo mismo con el nombre del contenedor (documents-pending)
        public async Task ProcessFileFromStorageAsync([BlobTrigger(blobPath: "documents-pending/{filename}", Connection = "AzureWebJobsStorage")] Stream file, string filename)
        {
            await _documentService.ProcessDocumentAsync(filename);
        }


        /// <summary>
        /// Get all documents
        /// </summary>
        [Function(nameof(GetDocuments))]
        [OpenApiOperation(operationId: "run", tags: new[] { "document" }, Summary = "Get all documents", Description = "Get all documents", Visibility = OpenApiVisibilityType.Advanced)]
        [OpenApiSecurity("X-Functions-Key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header, Description = "The function key to access the API")]
        [OpenApiParameter(name: "page", In = ParameterLocation.Query, Required = true, Type = typeof(int), Summary = "page", Description = "page")]
        [OpenApiParameter(name: "pageSize", In = ParameterLocation.Query, Required = true, Type = typeof(int), Summary = "pageSize", Description = "pageSize")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(DocumentModelOut[]), Summary = "Documents retrieved", Description = "The documents have been successfully retrieved")]
        public async Task<HttpResponseData> GetDocuments([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents")] HttpRequestData request, int page, int pageSize)
        {
            try
            {
                var result = await _documentService.GetDocumentsAsync(page, pageSize);
                return await request.CreateResponseAsync(result, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents");
                return request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Get a document by id
        /// </summary>
        /// <param name="request"></param>
        /// <param name="documentId"></param>
        /// <returns></returns>
        [Function(nameof(GetDocument))]
        [OpenApiOperation(operationId: "run", tags: new[] { "document" }, Summary = "Get document by id", Description = "Get document by id", Visibility = OpenApiVisibilityType.Advanced)]
        [OpenApiSecurity("X-Functions-Key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header, Description = "The function key to access the API")]
        [OpenApiParameter(name: "documentId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "Document Id", Description = "The document id")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(DocumentModelOut), Summary = "Document retrieved", Description = "The document has been successfully retrieved")]
        public async Task<HttpResponseData> GetDocument([HttpTrigger(AuthorizationLevel.Function, "get", Route = "documents/{documentId}")] HttpRequestData request, Guid documentId)
        {
            try
            {
                var result = await _documentService.GetDocumentAsync(documentId);
                return await request.CreateResponseAsync(result, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document");
                return request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }
    }
}