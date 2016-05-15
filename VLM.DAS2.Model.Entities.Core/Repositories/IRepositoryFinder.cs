using System;

namespace VLM.DAS2.Model.Entities.Core.Repositories
{
    public interface IRepositoryFinder
    {
        IBaseEntityRepository FindRepository<TEntity>() where TEntity : BaseEntity;
        IBaseEntityRepository FindRepository(Type type);
        
        IBaseEntityRepository FindRepository(string localId);

        IBaseEntityRepository GetRepository<TInterface>() where TInterface : IBaseEntityRepository;
    }
}
