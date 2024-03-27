using System.Linq.Expressions;

namespace Backend.Common.Interfaces.DataAccess
{
    /// <summary>
    /// Generic repository interface
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IRepository<TEntity> where TEntity : class
    {
        /// <summary>
        /// Returns the amount of elements in the repository
        /// </summary>
        /// <returns></returns>
        Task<int> CountAsync();

        /// <summary>
        /// Insert an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task InsertAsync(TEntity entity);

        /// <summary>
        /// Updates an entity
        /// </summary>
        /// <param name="entity"></param>
        void Update(TEntity entity);


        /// <summary>
        /// Get an entity
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        Task<TEntity> GetAsync(Guid entityId);

        /// <summary>
        /// Get a collecion of the entity
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="orderBy"></param>
        /// <param name="includeProperties"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<IEnumerable<TEntity>> GetAsync(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            string includeProperties = "", int? page = null, int? pageSize = null);

        /// <summary>
        /// Get a collecion of the entity
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="select"></param>
        /// <param name="filter"></param>
        /// <param name="keySelector"></param>
        /// <param name="orderBy"></param>
        /// <param name="includeProperties"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<IEnumerable<TResult>> GetAsync<TResult>(
          Expression<Func<TEntity, TResult>> select = null,
          Expression<Func<TEntity, bool>> filter = null,
          Expression<Func<TEntity, TEntity>> keySelector = null,
        Func<IQueryable<TResult>, IOrderedQueryable<TResult>> orderBy = null,
        string includeProperties = "", int? page = null, int? pageSize = null);

        /// <summary>
        /// Deletes an entity
        /// </summary>
        /// <param name="id"></param>
        void Delete(Guid id);

        /// <summary>
        /// Deletes an entity
        /// </summary>
        /// <param name="entity"></param>
        void Delete(TEntity entity);
    }
}
