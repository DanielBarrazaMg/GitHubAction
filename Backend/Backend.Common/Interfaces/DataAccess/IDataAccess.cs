using Backend.Entities;

namespace Backend.Common.Interfaces.DataAccess
{
    /// <summary>
    /// Data Access interface
    /// </summary>
    public interface IDataAccess
    {
        /// <summary>
        /// Documents collection
        /// </summary>
        IRepository<Document> Documents { get; }

        /// <summary>
        /// Clean up resources
        /// </summary>
        void Dispose();

        /// <summary>
        /// Saves all the changess
        /// </summary>
        Task<int> SaveChangesAsync();

        /// <summary>
        /// Creates a storage folder if it does not exist
        /// </summary>
        /// <param name="folerName"></param>
        /// <returns></returns>
        Task CreateStorageFolderIfNotExist(string folderName);
    }
}
