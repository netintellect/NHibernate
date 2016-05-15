using System;
using Newtonsoft.Json;
using VLM.DAS2.Core;
using VLM.DAS2.Model.Entities.Core.Attributes;

namespace VLM.DAS2.Model.Entities.Core
{
    [JsonObject(MemberSerialization.OptIn)]
    public class TemporalInfo : NotificationObject, ITemporalInfo
    {
        #region state
        [JsonProperty]
        public TimeSpan? Offset { get; set; }
        private DateTimeOffset _validFrom = DateTimeOffset.MinValue;
        [JsonProperty]
        public DateTimeOffset ValidFrom
        {
            get { return _validFrom; }
            set
            {
                _validFrom = value;
                if (Offset != null)
                    _validFrom = _validFrom.ToOffset(Offset.Value);
                OnPropertyChanged(() => ValidFrom);
            }
        }

        private DateTimeOffset? _validTo;
        [JsonProperty]
        public DateTimeOffset? ValidTo
        {
            get { return _validTo; }
            set
            {
                _validTo = value;
                if (Offset != null)
                    _validTo = _validTo?.ToOffset(Offset.Value);
                OnPropertyChanged(() => ValidTo);
            }
        }

        [JsonProperty, IgnoreOnMap]
        public bool IsDeleting { get; set; } = false;

        public bool IsActive
        {
            get
            {
                if (ValidTo == null) return true;
                if (IsDeleting) return false;

                return (Offset.HasValue
                        ? ValidTo >= DateTimeOffset.Now.ToOffset(Offset.Value)
                        : ValidTo >= DateTimeOffset.Now.ToOffset(new TimeSpan(1, 0, 0)));
            }
        } 
        #endregion

        #region behavior

        public TemporalInfo()
        {
            Offset = new TimeSpan(1, 0, 0);
        }
        public TemporalInfo(TimeSpan offSet)
        {
            Offset = offSet;
        }
        public void Activate()
        {
            ValidFrom = Offset == null 
                ? new DateTimeOffset(DateTime.Now, new TimeSpan(1, 0, 0)) 
                : new DateTimeOffset(DateTime.Now, Offset.Value);
            ValidTo = null;
        }

        public void Deactivate()
        {
            ValidTo = Offset == null
                ? new DateTimeOffset(DateTime.Now)
                : new DateTimeOffset(DateTime.Now, Offset ?? new TimeSpan(1, 0, 0));
        }

        public override string ToString()
        {
            return $"{GetType().Name} with validity from {ValidFrom} to {ValidTo}";
        }

        #endregion
    }
}
