using Backend.Common.Models;
using Backend.Models.Out;

namespace Backend.Common.Services
{
    /// <summary>
    /// Document services
    /// </summary>
    public interface IDocumentService
    {
        /// <summary>
        /// Upload a document to be processed
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="data"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        Task<Result<string>> UploadDocumentAsync(string fileName, Stream data, string contentType);

        /// <summary>
        /// Process a document
        /// </summary>
        /// <param name="fileUri"></param>
        /// <returns></returns>
        Task<Result> ProcessDocumentAsync(string filename);

        /// <summary>
        /// Get a document by id
        /// </summary>
        /// <param name="documentId"></param>
        /// <returns></returns>
        Task<Result<DocumentModelOut>> GetDocumentAsync(Guid documentId);

        /// <summary>
        /// Get all documents
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<Result<IEnumerable<DocumentModelOut>>> GetDocumentsAsync(int page, int pageSize);
    }
}