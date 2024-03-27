using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Backend.Common.Interfaces.DataAccess;
using Backend.Entities;
using Microsoft.Extensions.Configuration;

namespace Backend.DataAccess
{
    /// <inheritdoc/>
    public class DataAccess : IDisposable, IDataAccess
    {
        private DatabaseContext _context;
        private bool disposed = false;
        private string storageConnectionString;
        private readonly BlobServiceClient _blobServiceClient;


        /// <inheritdoc/>
        public IRepository<Document> Documents { get; }

        /// <summary>
        /// Gets the configuration
        /// </summary>
        public DataAccess(IConfiguration configuration)
        {
            _context = new DatabaseContext(configuration);
            Documents = new Repository<Document>(_context);
            this._context.Database.EnsureCreated();
            storageConnectionString = configuration["StorageConnectionString"];
            _blobServiceClient = new BlobServiceClient(this.storageConnectionString);
        }

        /// <inheritdoc/>
        public Task<int> SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            disposed = true;
        }

        /// <inheritdoc/>
        public string GetSasToken(string container, int expiresOnMinutes)
        {
            // Generates the token for this account
            var accountKey = string.Empty;
            var accountName = string.Empty;
            var connectionStringValues = this.storageConnectionString.Split(';')
                .Select(s => s.Split(new char[] { '=' }, 2))
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

        public async Task CreateStorageFolderIfNotExist(string folderName)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(folderName);
            await containerClient.CreateIfNotExistsAsync();
        }
    }
}