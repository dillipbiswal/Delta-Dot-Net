using Datavail.Delta.Infrastructure;
using Datavail.Delta.Infrastructure.Repository;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Datavail.Delta.Repository.MongoDb
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : IEntity
    {
        /// <summary>
        /// MongoCollection field.
        /// </summary>
        private readonly MongoCollection<TEntity> _collection;

        #region ctor
        public GenericRepository()
            : this(Util.GetDefaultConnectionString())
        {
        }

        public GenericRepository(string connectionString)
        {
            _collection = Util.GetCollectionFromConnectionString<TEntity>(connectionString);
        }
        #endregion

        #region CREATE
        public void Add(TEntity entity)
        {
            _collection.Insert<TEntity>(entity);
        }

        public void Add(IEnumerable<TEntity> entities)
        {
            _collection.InsertBatch<TEntity>(entities);
        }
        #endregion

        #region READ
        public IEnumerable<TEntity> GetAll()
        {
            return _collection.AsQueryable().AsEnumerable();
        }

        public TEntity GetByKey(Guid keyValue)
        {
            return _collection.FindOneByIdAs<TEntity>(new ObjectId(keyValue.ToString()));
        }

        public IQueryable<TEntity> GetQuery()
        {
            return _collection.AsQueryable();
        }

        public IQueryable<TEntity> GetQuery(System.Linq.Expressions.Expression<Func<TEntity, bool>> criteria)
        {
            return _collection.AsQueryable().Where(criteria);
        }

        public IQueryable<TEntity> GetQuery(Infrastructure.Specification.ISpecification<TEntity> specification)
        {
            return specification.SatisfyingEntitiesFrom(GetQuery());
        }


        public IEnumerable<TEntity> Get(System.Linq.Expressions.Expression<Func<TEntity, bool>> criteria)
        {
            return _collection.AsQueryable().Where(criteria).AsEnumerable();
        }

        public IEnumerable<TEntity> Get(Infrastructure.Specification.ISpecification<TEntity> specification)
        {
            return specification.SatisfyingEntitiesFrom(GetQuery()).AsEnumerable();
        }

        #region PAGING
        public IEnumerable<TEntity> Get(System.Linq.Expressions.Expression<Func<TEntity, string>> orderBy, int pageIndex, int pageSize, SortOrder sortOrder = SortOrder.Ascending)
        {
            var recordsToSkip = (pageIndex - 1) * pageSize;
            if (sortOrder == SortOrder.Ascending)
            {
                return GetQuery().OrderBy(orderBy).Skip(recordsToSkip).Take(pageSize).AsEnumerable();
            }
            return GetQuery().OrderByDescending(orderBy).Skip(recordsToSkip).Take(pageSize).AsEnumerable();
        }

        public IEnumerable<TEntity> Get(System.Linq.Expressions.Expression<Func<TEntity, bool>> criteria, System.Linq.Expressions.Expression<Func<TEntity, string>> orderBy, int pageIndex, int pageSize, SortOrder sortOrder = SortOrder.Ascending)
        {
            var recordsToSkip = (pageIndex - 1) * pageSize;
            if (sortOrder == SortOrder.Ascending)
            {
                return GetQuery().Where(criteria).OrderBy(orderBy).Skip(recordsToSkip).Take(pageSize).AsEnumerable();
            }
            return GetQuery().Where(criteria).OrderByDescending(orderBy).Skip(recordsToSkip).Take(pageSize).AsEnumerable();
        }

        public IEnumerable<TEntity> Get(Infrastructure.Specification.ISpecification<TEntity> specification, System.Linq.Expressions.Expression<Func<TEntity, string>> orderBy, int pageIndex, int pageSize, SortOrder sortOrder = SortOrder.Ascending)
        {
            var recordsToSkip = (pageIndex - 1) * pageSize;
            if (sortOrder == SortOrder.Ascending)
            {
                return specification.SatisfyingEntitiesFrom(GetQuery()).OrderBy(orderBy).Skip(recordsToSkip).Take(pageSize).AsEnumerable();
            }
            return specification.SatisfyingEntitiesFrom(GetQuery()).OrderByDescending(orderBy).Skip(recordsToSkip).Take(pageSize).AsEnumerable();
        }
        #endregion


        public TEntity Single(System.Linq.Expressions.Expression<Func<TEntity, bool>> criteria)
        {
            return GetQuery().Single(criteria);
        }

        public TEntity Single(Infrastructure.Specification.ISpecification<TEntity> specification)
        {
            return specification.SatisfyingEntityFrom(GetQuery());
        }

        public TEntity First(System.Linq.Expressions.Expression<Func<TEntity, bool>> criteria)
        {
            return GetQuery().Where(criteria).FirstOrDefault();
        }

        public TEntity First(Infrastructure.Specification.ISpecification<TEntity> specification)
        {
            return specification.SatisfyingEntitiesFrom(GetQuery()).First();
        }

        #endregion

        #region UPDATE
        public TEntity Update(TEntity entity)
        {
            _collection.Save(entity);
            return entity;
        }

        public void Update(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                _collection.Save(entity);
            }
        }

        #endregion

        #region DELETE
        public void Delete(Guid id)
        {
            _collection.Remove(Query.EQ("_id", new ObjectId(id.ToString())));
        }

        public void Delete(TEntity entity)
        {
            Delete(entity.Id);
        }

        public void Delete(System.Linq.Expressions.Expression<Func<TEntity, bool>> criteria)
        {
            foreach (var entity in _collection.AsQueryable().Where(criteria))
            {
                Delete(entity);
            }
        }

        public void Delete(Infrastructure.Specification.ISpecification<TEntity> specification)
        {
            foreach (var entity in GetQuery(specification))
            {
                Delete((entity));
            }
        }
        #endregion

        #region COUNT
        public long Count()
        {
            return _collection.Count();
        }

        public long Count(System.Linq.Expressions.Expression<Func<TEntity, bool>> criteria)
        {
            return GetQuery(criteria).Count();
        }

        public long Count(Infrastructure.Specification.ISpecification<TEntity> specification)
        {
            return GetQuery(specification).Count();
        }
        #endregion
    }
}