using System.Collections.Generic;
using Microsoft.Practices.EnterpriseLibrary.Validation;

namespace VLM.DAS2.Model.Entities.Core
{
    public interface IValidate
    {
        void Invalidate();
        bool HasGraphValidationErrors();
        void AddValidationErrors(IEnumerable<ValidationResult> validationResults);
        void SetValidationError(ValidationResult validationResult);
        void ClearValidationErrors();
        void ClearValidationError(string propertyName);
    }
}
