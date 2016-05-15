using Microsoft.Practices.EnterpriseLibrary.Validation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using VLM.DAS2.Core.Extensions;
using ValidationResult = Microsoft.Practices.EnterpriseLibrary.Validation.ValidationResult;

namespace VLM.DAS2.Model.Entities.Core
{
    public abstract class ValidatedEntity<TEntity> : AuditEntity, IValidate, INotifyDataErrorInfo  where TEntity : BaseEntity
    {
        #region state
        private Validator<TEntity> _entityValidator;
        protected Validator<TEntity> EntityValidator
        {
            get
            {
                return _entityValidator = _entityValidator ?? ValidationFactory.CreateValidator<TEntity>();
            }    
        }

        private DataErrorInfoHelper _dataErrorInfoHelper;

        protected DataErrorInfoHelper DataErrorInfoHelper
        {
            get { return _dataErrorInfoHelper = _dataErrorInfoHelper ?? new DataErrorInfoHelper(this); }
        }

        private ValidationResults _validationResults { get; set; }

        private readonly List<ValidationResult> _validationErrors = new List<ValidationResult>();
        public IEnumerable<ValidationResult> ValidationErrors => _validationErrors;

        public override bool HasValidationErrors => ValidationErrors.Any();

        public bool HasGraphValidationErrors()
        {
            if (HasValidationErrors) return true;

            var hasErrors = HaveNestedEntitiesValidationErrors(entity => entity?.HasGraphValidationErrors() ?? false);
            if (hasErrors) return true;

            return false;
        }

        public override void EndEdit()
        {
            base.EndEdit();
            _validationErrors.Clear();
        }
        
        public bool IgnoreChangedErrors { get; set; }
        public virtual bool IsValid
        {
            get
            {
                Invalidate();
                return !HasGraphErrors;
            }
        }
        #endregion

        #region behavior

        protected ValidatedEntity() 
        {
            _validationResults = new ValidationResults();
            AuditInfo = new AuditInfo();
        }

        public override bool IsPropertyValid(string propertyName, object value, 
                                             bool forceValidation = false)
        {
            // check if we want to force the validation of the entity
            if (forceValidation)
            {
                Invalidate();
                return HasValidationErrors;
            }

            // check if there is an invalid property for this particular
            // property
            var results =  _validationErrors?.FindAll(vr => vr.Key.Equals(propertyName));
            if (results == null ||
                results.Count == 0)
            {
                ClearValidationError(propertyName);
                return true;
            }
            foreach (var result in results)
            {
                SetValidationError(result);
            }
            return false;
        }
        
        public virtual void Invalidate()
        {
            ClearValidationErrors();

            var results = EntityValidator.Validate(this);
            foreach (var result in results)
            {
                if (result.Key == null) continue;

                var validateEntity = result.Target as IValidate;
                validateEntity?.AddValidationErrors(new List<ValidationResult>() { result });
            }
            OnPropertyChanged(() => HasErrors);
            OnPropertyChanged(() => HasGraphErrors);
        }

        public void ClearValidationError(string propertyName)
        {
            var error = ValidationErrors.FirstOrDefault(e => e.Key != null &&
                                                             e.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
            if (error == null) return;

            _validationErrors.Remove(error);

            NotifyErrorsChanged(propertyName);
        }

        public void ClearValidationErrors()
        {
            var errors = ValidationErrors.ToArray();
            foreach(var error in errors)
            {
                ClearValidationError(error.Key);
            }
        }

        public void SetValidationError(ValidationResult validationResult)
        {
            var error = ValidationErrors.FirstOrDefault(e => e.Key != null &&
                                                             e.Key.Equals(validationResult.Key, StringComparison.OrdinalIgnoreCase) &&
                                                             e.Target.ToString().Equals(validationResult.Target.ToString()));
            if (error != null) return;

            _validationErrors.Add(validationResult);
            
            NotifyErrorsChanged(validationResult.Key);
        }

        public void AddValidationErrors(IEnumerable<ValidationResult> validationResults)
        {
            foreach (var validationResult in validationResults)
            {
                var error = ValidationErrors.FirstOrDefault(e => e.Key != null && 
                                                                 e.Key.Equals(validationResult.Key, StringComparison.OrdinalIgnoreCase) &&
                                                                 e.Target.ToString().Equals(validationResult.Target.ToString(), StringComparison.OrdinalIgnoreCase));
                if (error != null) continue;

                SetValidationError(validationResult);
            }
        }
        #endregion

        #region INotifyDataErrorInfo
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        protected void NotifyErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            OnPropertyChanged(() => HasErrors,
                              () => HasGraphErrors);
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return null;

            return ValidationErrors.Where(e => e.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
        }

        public bool HasErrors => HasValidationErrors;

        public bool HasGraphErrors => HasGraphValidationErrors();

        /// <summary>
        /// Method will perform a given action on all nested DTO (Recursive)
        /// </summary>
        private bool HaveNestedEntitiesValidationErrors(Func<IValidate, bool> action)
        {
            List<PropertyInfo> propertyInfos = GetType().GetPropertiesOfType<BaseEntity>()
                                                        .ToList();
            
            var result = false;
            foreach (var propertyInfo in propertyInfos)
            {
                if (!IsPartOfHandleOnNestingList(propertyInfo)) continue;
                
                var propertyValue = propertyInfo.GetValue(this, null);

                var validatedEntity = propertyValue as IValidate;
                if (validatedEntity != null)
                {
                    if (action(validatedEntity)) result = true;
                }
                else
                {
                    var values = propertyValue as IEnumerable;
                    if (values != null)
                    {
                        foreach (var item in values)
                        {
                            if (action((IValidate)item)) result = true;
                        }
                    }
                }
            }
            return result;
        }
        #endregion
    }
}
