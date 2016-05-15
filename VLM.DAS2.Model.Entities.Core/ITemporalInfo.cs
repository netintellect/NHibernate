using System;

namespace VLM.DAS2.Model.Entities.Core
{
    public interface ITemporalInfo
    {
        TimeSpan? Offset { get;  }
        DateTimeOffset ValidFrom { get; set; }
        DateTimeOffset? ValidTo { get; set; }
        bool IsActive { get; }
        void Activate();
        void Deactivate();
    }
}
