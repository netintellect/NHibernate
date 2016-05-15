using System;
using Newtonsoft.Json;

namespace VLM.DAS2.Model.Entities.Core
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class TemporalEntity<TEntity> : ValidatedEntity<TEntity>, ITemporalEntity where TEntity : BaseEntity
    {
        #region state
        public static TimeSpan? TimeZoneOffet { get; set; }

        private string StartPropertyName => $"{typeof (TemporalInfo).Name}.{nameof(TemporalInfo.ValidFrom)}";
        private string EndPropertyName => $"{typeof(TemporalInfo).Name}.{nameof(TemporalInfo.ValidTo)}";
    
        private TemporalInfo _temporalInfo;
        [JsonProperty]
        public TemporalInfo TemporalInfo
        {
            get { return _temporalInfo; }
            set
            {
                var attach = _temporalInfo == null;
                _temporalInfo = value;
                if (attach && _temporalInfo != null)
                {
                    if (TimeZoneOffet != null) _temporalInfo.Offset = TimeZoneOffet;
                    TemporalInfo.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName.Equals(nameof(TemporalInfo.ValidFrom)))
                        {
                            SetChange(StartPropertyName, TemporalInfo.ValidFrom);
                            OnPropertyChanged(() => IsActive);
                        }
                    };
                }
            }
        }

        public bool IsActive => TemporalInfo.IsActive;

        #endregion

        #region behavior
        public TemporalEntity()
        {
            var offset = DeduceOffSet();
            TemporalInfo = new TemporalInfo(offset);
        }

        private TimeSpan DeduceOffSet()
        {
            if (TimeZoneOffet.HasValue) return TimeZoneOffet.Value;
            
            return new TimeSpan(1,0,0);
        }

        public override bool IsDeleted => !TemporalInfo.IsActive &&
                                          IsPropertyChanged(EndPropertyName);

        #endregion

        #region behavior
        public override void Delete()
        {
            TemporalInfo.ValidTo = DateTimeOffset.Now;
            TemporalInfo.IsDeleting = true;
            SetChange(EndPropertyName, TemporalInfo.ValidTo);
            OnPropertyChanged(() => IsActive);

            ActOnAllNestedEntities(dto => dto.Delete());
        }

        public override void UndoDelete()
        {
            CancelEdit();
            TemporalInfo.ValidTo = null;
            TemporalInfo.IsDeleting = false;
            ActOnAllNestedEntities(dto => dto.UndoDelete()); 
        }

        public override string ToString()
        {
            return TemporalInfo.ToString();
        }

        #endregion
    }
}
