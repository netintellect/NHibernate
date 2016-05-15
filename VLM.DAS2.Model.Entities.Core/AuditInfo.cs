using System;
using Newtonsoft.Json;

namespace VLM.DAS2.Model.Entities.Core
{
    [JsonObject(MemberSerialization.OptIn)]
    public class AuditInfo : IAuditInfo
    {
        [JsonProperty]
		public byte[] RowVersion { get; set; }
        [JsonProperty]
		public DateTimeOffset? CreatedOn { get; set; }
        [JsonProperty]
		public string CreatedBy { get; set; }
        [JsonProperty]
		public DateTimeOffset? ModifiedOn { get; set; }
        [JsonProperty]
		public string ModifiedBy { get; set; }
    }
}
