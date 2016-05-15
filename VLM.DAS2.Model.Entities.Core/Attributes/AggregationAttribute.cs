using System;

namespace VLM.DAS2.Model.Entities.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class AggregationAttribute : Attribute
    {
        #region state
        public bool IsShared { get; }
        public bool IsIndependent { get; }
        public bool DoesParticipate { get; }
        public string Key { get; }
        #endregion

        #region behavior

        public AggregationAttribute(bool isComposite = false, bool isIndependent = false, 
            bool doesParticipate = false, string key = null)
        {
            IsShared = !isComposite;
            DoesParticipate = doesParticipate;
            IsIndependent = isIndependent;
            Key = key;
        }

        #endregion
    }
}
