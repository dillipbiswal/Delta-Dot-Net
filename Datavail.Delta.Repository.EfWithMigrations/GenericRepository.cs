using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Design.PluralizationServices;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Infrastructure.Specification;

namespace Datavail.Delta.Repository.EfWithMigrations
{
    /// <summary>
    /// Generic repository
    /// </summary>
    
    public class GenericRepository : IRepository
    {
        protected DbContext Context;
        private static readonly PluralizationService Pluralizer = PluralizationService.CreateService(CultureInfo.GetCultureInfo("en"));
        private readonly IDeltaLogger _logger;

        public GenericRepository(DbContext context, IDeltaLogger logger)
        {
            _logger = logger;
            if (context == null)
                throw new ArgumentNullException("context");
            Context = context;
        }

        public TEntity GetByKey<TEntity>(object keyValue) where TEntity : class
        {
            var key = GetEntityKey<TEntity>(keyValue);

            object originalItem;
            if (((IObjectContextAdapter)Context).ObjectContext.TryGetObjectByKey(key, out originalItem))
            {
                return (TEntity)originalItem;
            }
            return default(TEntity);
        }

        public IQueryable<TEntity> GetQuery<TEntity>() where TEntity : class
        {
            var entityName = GetEntityName<TEntity>();
            return ((IObjectContextAdapter)Context).ObjectContext.CreateQuery<TEntity>(entityName);
        }

        public IQueryable<TEntity> GetQuery<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            return GetQuery<TEntity>().Where(predicate);
        }

        public IQueryable<TEntity> GetQuery<TEntity>(ISpecification<TEntity> criteria) where TEntity : class
        {
            return criteria.SatisfyingEntitiesFrom(GetQuery<TEntity>());
        }

        public IEnumerable<TEntity> Get<TEntity>(Expression<Func<TEntity, string>> orderBy, int pageIndex, int pageSize, SortOrder sortOrder = SortOrder.Ascending) where TEntity : class
        {
            var recordsToSkip = (pageIndex - 1) * pageSize;
            if (sortOrder == SortOrder.Ascending)
            {
                return GetQuery<TEntity>().OrderBy(orderBy).Skip(recordsToSkip).Take(pageSize).AsEnumerable();
            }
            return GetQuery<TEntity>().OrderByDescending(orderBy).Skip(recordsToSkip).Take(pageSize).AsEnumerable();
        }

        public IEnumerable<TEntity> Get<TEntity>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, string>> orderBy, int pageIndex, int pageSize, SortOrder sortOrder = SortOrder.Ascending) where TEntity : class
        {
            var recordsToSkip = (pageIndex - 1) * pageSize;
            if (sortOrder == SortOrder.Ascending)
            {
                return GetQuery<TEntity>().Where(predicate).OrderBy(orderBy).Skip(recordsToSkip).Take(pageSize).AsEnumerable();
            }
            return GetQuery<TEntity>().Where(predicate).OrderByDescending(orderBy).Skip(recordsToSkip).Take(pageSize).AsEnumerable();
        }

        public IEnumerable<TEntity> Get<TEntity>(ISpecification<TEntity> specification, Expression<Func<TEntity, string>> orderBy, int pageIndex, int pageSize, SortOrder sortOrder = SortOrder.Ascending) where TEntity : class
        {
            var recordsToSkip = (pageIndex - 1) * pageSize;
            if (sortOrder == SortOrder.Ascending)
            {
                return specification.SatisfyingEntitiesFrom(GetQuery<TEntity>()).OrderBy(orderBy).Skip(recordsToSkip).Take(pageSize).AsEnumerable();
            }
            return specification.SatisfyingEntitiesFrom(GetQuery<TEntity>()).OrderByDescending(orderBy).Skip(recordsToSkip).Take(pageSize).AsEnumerable();
        }

        public TEntity Single<TEntity>(Expression<Func<TEntity, bool>> criteria) where TEntity : class
        {
            return GetQuery<TEntity>().Single<TEntity>(criteria);
        }

        public TEntity Single<TEntity>(ISpecification<TEntity> criteria) where TEntity : class
        {
            return criteria.SatisfyingEntityFrom(GetQuery<TEntity>());
        }

        public TEntity First<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class { return GetQuery<TEntity>().First(predicate); }

        public TEntity First<TEntity>(ISpecification<TEntity> criteria) where TEntity : class
        {
            return criteria.SatisfyingEntitiesFrom(GetQuery<TEntity>()).First();
        }

        public void Add<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            Context.Set<TEntity>().Add(entity);
        }

        public void Attach<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            Context.Set<TEntity>().Attach(entity);
        }

        public void Delete<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            Context.Set<TEntity>().Remove(entity);
        }

        public void Delete<TEntity>(Expression<Func<TEntity, bool>> criteria) where TEntity : class
        {
            var records = Find(criteria);

            foreach (var record in records)
            {
                Delete(record);
            }
        }

        public void Delete<TEntity>(ISpecification<TEntity> criteria) where TEntity : class
        {
            var records = Find(criteria);
            foreach (var record in records)
            {
                Delete(record);
            }
        }

        public IEnumerable<TEntity> GetAll<TEntity>() where TEntity : class
        {
            return GetQuery<TEntity>().AsEnumerable();
        }

        public TEntity Save<TEntity>(TEntity entity) where TEntity : class
        {
            Add(entity);
            Context.SaveChanges();
            return entity;
        }

        public void Update<TEntity>(TEntity entity) where TEntity : class
        {
            //Attach the entity if it is disconnected
            if (Context.Entry(entity).State == EntityState.Detached)
            {
                Attach(entity);
                Context.Entry(entity).State = EntityState.Modified;
            }
        }

        public IEnumerable<TEntity> Find<TEntity>(Expression<Func<TEntity, bool>> criteria) where TEntity : class
        {
            return GetQuery<TEntity>().Where(criteria);
        }

        public TEntity FindOne<TEntity>(Expression<Func<TEntity, bool>> criteria) where TEntity : class { return GetQuery<TEntity>().Where(criteria).FirstOrDefault(); }

        public TEntity FindOne<TEntity>(ISpecification<TEntity> criteria) where TEntity : class
        {
            return criteria.SatisfyingEntityFrom(GetQuery<TEntity>());
        }

        public IEnumerable<TEntity> Find<TEntity>(ISpecification<TEntity> criteria) where TEntity : class
        {
            return criteria.SatisfyingEntitiesFrom(GetQuery<TEntity>()).AsEnumerable();
        }

        public int Count<TEntity>() where TEntity : class
        {
            return GetQuery<TEntity>().Count();
        }

        public int Count<TEntity>(Expression<Func<TEntity, bool>> criteria) where TEntity : class
        {
            return GetQuery<TEntity>().Count(criteria);
        }

        public int Count<TEntity>(ISpecification<TEntity> criteria) where TEntity : class
        {
            return criteria.SatisfyingEntitiesFrom(GetQuery<TEntity>()).Count();
        }

        public IUnitOfWork UnitOfWork
        {
            get { return _unitOfWork ?? (_unitOfWork = new UnitOfWork(Context)); }
        }

        private EntityKey GetEntityKey<TEntity>(object keyValue) where TEntity : class
        {
            var entitySetName = GetEntityName<TEntity>();
            var objectSet = ((IObjectContextAdapter)Context).ObjectContext.CreateObjectSet<TEntity>();
            var keyPropertyName = objectSet.EntitySet.ElementType.KeyMembers[0].ToString();
            var entityKey = new EntityKey(entitySetName, new[] { new EntityKeyMember(keyPropertyName, keyValue) });
            return entityKey;
        }

        private string GetEntityName<TEntity>() where TEntity : class
        {
            return string.Format("{0}.{1}", ((IObjectContextAdapter)Context).ObjectContext.DefaultContainerName, Pluralizer.Pluralize(typeof(TEntity).Name));
        }

        private IUnitOfWork _unitOfWork;
    }
}