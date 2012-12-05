using System;
using System.Collections.Generic;
using System.Linq;
using Datavail.Delta.Infrastructure;
using Datavail.Delta.Infrastructure.Repository;

namespace Datavail.Delta.Repository.MongoDb
{
    public interface IGenericRepository<TEntity> where TEntity : IEntity
    {
        //CREATE
        void Add(TEntity entity);
        void Add(IEnumerable<TEntity> entities);

        //READ
        IEnumerable<TEntity> GetAll();
        TEntity GetByKey(Guid keyValue);

        IQueryable<TEntity> GetQuery();
        IQueryable<TEntity> GetQuery(System.Linq.Expressions.Expression<Func<TEntity, bool>> criteria);
        IQueryable<TEntity> GetQuery(Infrastructure.Specification.ISpecification<TEntity> specification);

        IEnumerable<TEntity> Get(System.Linq.Expressions.Expression<Func<TEntity, bool>> criteria);
        IEnumerable<TEntity> Get(Infrastructure.Specification.ISpecification<TEntity> specification);
        IEnumerable<TEntity> Get(System.Linq.Expressions.Expression<Func<TEntity, string>> orderBy, int pageIndex, int pageSize, SortOrder sortOrder = SortOrder.Ascending);
        IEnumerable<TEntity> Get(System.Linq.Expressions.Expression<Func<TEntity, bool>> criteria, System.Linq.Expressions.Expression<Func<TEntity, string>> orderBy, int pageIndex, int pageSize, SortOrder sortOrder = SortOrder.Ascending);
        IEnumerable<TEntity> Get(Infrastructure.Specification.ISpecification<TEntity> specification, System.Linq.Expressions.Expression<Func<TEntity, string>> orderBy, int pageIndex, int pageSize, SortOrder sortOrder = SortOrder.Ascending);

        TEntity Single(System.Linq.Expressions.Expression<Func<TEntity, bool>> criteria);
        TEntity Single(Infrastructure.Specification.ISpecification<TEntity> specification);

        TEntity First(System.Linq.Expressions.Expression<Func<TEntity, bool>> criteria);
        TEntity First(Infrastructure.Specification.ISpecification<TEntity> specification);
        
        //UPDATE
        TEntity Update(TEntity entity);
        void Update(IEnumerable<TEntity> entities);
        
        //DELETE
        void Delete(Guid id);
        void Delete(TEntity entity);
        void Delete(System.Linq.Expressions.Expression<Func<TEntity, bool>> criteria);
        void Delete(Infrastructure.Specification.ISpecification<TEntity> specification);
        
        //COUNT
        long Count();
        long Count(System.Linq.Expressions.Expression<Func<TEntity, bool>> criteria);
        long Count(Infrastructure.Specification.ISpecification<TEntity> specification);
    }
}