using System;
using System.Collections.Generic;
using System.Linq;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Repository;

namespace Datavail.Delta.Repository.Mock
{
    public class RepositoryBase<T> : IRepository<T, Guid>, IUnitOfWork where T : DomainBase
    {
        protected List<T> EntityList;
        protected List<T> TemporaryEntityList;
        protected List<T> ToRemoveEntityList;

        public RepositoryBase()
        {
            EntityList = new List<T>();
            TemporaryEntityList = new List<T>();
            ToRemoveEntityList = new List<T>();
        }

        public void Add(T entity)
        {
            if (!TemporaryEntityList.Contains(entity))
            {
                TemporaryEntityList.Add(entity);
            }
        }

        public void Update(T entity)
        {
            var fromDb = TemporaryEntityList.Where(e => e.Id == entity.Id).FirstOrDefault();
            TemporaryEntityList.Remove(fromDb);
            TemporaryEntityList.Add(entity);
        }

        public void Remove(T entity)
        {
            ToRemoveEntityList.Add(entity);
        }

        public ICollection<T> GetAll()
        {
            return EntityList;
        }

        public T GetById(Guid id)
        {
            var fromDb = EntityList.Where(e => e.Id == id).FirstOrDefault();
            if (fromDb == null)
                return null;
            var clone = (T)fromDb.Clone();
            return clone;
        }

        public void Commit()
        {
            foreach (var entity in TemporaryEntityList)
            {
                var fromDb = EntityList.Where(e => e.Id == entity.Id).FirstOrDefault();
                if (fromDb==null)
                {
                    EntityList.Add(entity);
                }
                else
                {
                    EntityList.Remove(fromDb);
                    EntityList.Add(entity);
                }
            }

            foreach (var entity in ToRemoveEntityList)
            {
                var fromdb = EntityList.Where(e => e.Id == entity.Id).FirstOrDefault();
                EntityList.Remove(fromdb);
            }
        }
    }
}