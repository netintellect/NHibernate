using System;

namespace VLM.DAS2.Model.Entities.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class KeyAttribute : Attribute
    {
        #region state

        public bool IsIdentity { get; private set; }

        #endregion

        #region methods

        public KeyAttribute(bool isIdentity)
        {
            IsIdentity = isIdentity;
        }

        #endregion
    }
}
