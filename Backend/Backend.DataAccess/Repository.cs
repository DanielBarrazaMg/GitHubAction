using Backend.Common.Interfaces.DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Backend.DataAccess
{
    /// <summary>
    /// Generic class for a repository 
    /// </summary>
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private DatabaseContext _context;
        private DbSet<TEntity> _dbSet;

        /// <summary>
        /// Receives the context by Di
        /// </summary>
        public Repository(DatabaseContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }

        public virtual async Task<int> CountAsync()
        {
            IQueryable<TEntity> query = _dbSet;
            return await query.CountAsync();
        }

        public virtual async Task<IEnumerable<TEntity>> GetAsync(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            string includeProperties = "", int? page = null, int? pageSize = null)
        {
            IQueryable<TEntity> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }
            foreach (var includeProperty in includeProperties.Split
                (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }
            if (orderBy != null)
            {
                return (page != null && pageSize != null) ?
                   await orderBy(query).Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value).ToListAsync()
                 : await orderBy(query).ToListAsync();

            }
            else
            {
                return await query.ToListAsync();
            }
        }

        public virtual async Task<IEnumerable<TResult>> GetAsync<TResult>(
            Expression<Func<TEntity, TResult>> select,
            Expression<Func<TEntity, bool>> filter = null,

            Expression<Func<TEntity, TEntity>> keySelector = null,


            Func<IQueryable<TResult>, IOrderedQueryable<TResult>> orderBy = null,
            string includeProperties = "", int? page = null, int? pageSize = null)
        {
            IQueryable<TEntity> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var includeProperty in includeProperties.Split
                (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            var result = query.Select(select);

            var ordered = query.OrderBy(keySelector);

            if (orderBy != null)
            {
                var orderd = await orderBy(result).ToListAsync();

                return (page != null && pageSize != null)
                    ? await orderBy(result).Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value).ToListAsync()
                    : await orderBy(result).ToListAsync();
            }
            else
            {
                return await result.ToListAsync();
            }
        }

        public virtual async Task<TEntity> GetAsync(Guid entityId)
        {
            var entity = await _dbSet.FindAsync(entityId);
            return entity;
        }

        public virtual async Task InsertAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public virtual void Update(TEntity entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }

        public virtual void Delete(Guid id)
        {
            TEntity entity = _dbSet.Find(id);
            Delete(entity);
        }

        public virtual void Delete(TEntity entity)
        {
            if (_context.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
            _dbSet.Remove(entity);
        }
    }
}
