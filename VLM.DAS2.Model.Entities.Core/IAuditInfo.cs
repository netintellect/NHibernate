using System;

namespace VLM.DAS2.Model.Entities.Core
{
    public interface IAuditInfo
    {
        DateTimeOffset? CreatedOn { get; set; }
        string CreatedBy { get; set; }
        DateTimeOffset? ModifiedOn { get; set; }
        string ModifiedBy { get; set; }
        byte[] RowVersion { get; set; }
    }
}
