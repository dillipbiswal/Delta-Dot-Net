using System.Linq;

namespace Datavail.Delta.Infrastructure.Specification
{
    public abstract class CompositeSpecification<TEntity> : ISpecification<TEntity>
    {
        protected readonly Specification<TEntity> LeftSide;
        protected readonly Specification<TEntity> RightSide;

        protected CompositeSpecification(Specification<TEntity> leftSide, Specification<TEntity> rightSide)
        {
            LeftSide = leftSide;
            RightSide = rightSide;
        }

        public abstract TEntity SatisfyingEntityFrom(IQueryable<TEntity> query);

        public abstract IQueryable<TEntity> SatisfyingEntitiesFrom(IQueryable<TEntity> query);
    }
}
