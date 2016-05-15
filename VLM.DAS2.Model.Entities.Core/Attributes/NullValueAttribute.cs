using System;

namespace VLM.DAS2.Model.Entities.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class NullValueAttribute : Attribute
    {
        #region state
        public bool AllowEmptyStrings { get; set; }
        public object NullableSymbol { get; set; }
        #endregion

        #region behavior
        public NullValueAttribute() { }
        #endregion
    }

}
