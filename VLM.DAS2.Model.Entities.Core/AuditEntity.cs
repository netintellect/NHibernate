using System;
using Newtonsoft.Json;

namespace VLM.DAS2.Model.Entities.Core
{
    [JsonObject(MemberSerialization.OptIn)]
    public class AuditEntity : BaseEntity 
    {
        
        [JsonProperty]
        public AuditInfo AuditInfo { get; set; }
        
        public virtual void SetAuditInfo(string login)
        {
            if (string.IsNullOrEmpty(login)) return;
            
            if (IsNew)
            {
                AuditInfo.CreatedOn = AuditInfo.ModifiedOn = DateTime.Now;
                AuditInfo.CreatedBy = AuditInfo.ModifiedBy = login;
            }
            else
            {
                AuditInfo.ModifiedOn = DateTime.Now;
                AuditInfo.ModifiedBy = login;
            }
        }

        public virtual void SetUnmodified(AuditEntity baseEntity)
        {
            if (baseEntity != null &&
                baseEntity.GetType().FullName.Equals(GetType().FullName))
            {
                var oriAuditInfo = AuditInfo;
                var newAuditInfo = baseEntity.AuditInfo;

                oriAuditInfo.CreatedBy = newAuditInfo.CreatedBy;
                oriAuditInfo.CreatedOn = newAuditInfo.CreatedOn;
                oriAuditInfo.ModifiedBy = newAuditInfo.ModifiedBy;
                oriAuditInfo.ModifiedOn = newAuditInfo.ModifiedOn;
                oriAuditInfo.RowVersion = newAuditInfo.RowVersion;
            }
            EndEdit();
        }
    }
}
