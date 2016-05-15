using System;

namespace VLM.DAS2.Model.Entities.Core.Repositories
{
    public interface IBaseEntityRepository
    {
        bool IsDirty();
        bool Handles(Type type);
        string LocalId { get; }
        void UnregisterKey(string key);
    }
}
