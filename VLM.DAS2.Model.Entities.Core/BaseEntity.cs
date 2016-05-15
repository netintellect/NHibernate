using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using VLM.DAS2.Core;
using VLM.DAS2.Core.Extensions;
using VLM.DAS2.Model.Entities.Core.Attributes;
using VLM.DAS2.Model.Entities.Core.Repositories;

namespace VLM.DAS2.Model.Entities.Core
{
    public abstract class BaseEntity : NotificationObject, 
        IEditableObject, IBaseEntity
    {
        #region state
        private int _id;
        [JsonProperty, Key(true)]
        public int Id
        {
            get { return _id; }
            set { SetProperty(value, ref _id, () => Id); }
        }

        public static IRepositoryFinder RepositoryFinder { get; private set; }
        private bool _useRepositoryFinder = true;
        protected bool UseRepositoryFinder
        {
            get
            {
                if (!_useRepositoryFinder) return false;
                
                return (RepositoryFinder != null);
            }
            private set
            {
                _useRepositoryFinder = value;
            }
        }

        [JsonProperty]
        public IDictionary<string, object> ChangedPropertyNames { get; } = new Dictionary<string, object>();
        
        public bool IsNew
        {
            get
            {
                // check if there is a meta data type defined that 
                // describes which property is the key property
                if (KeyInfo == null)
                {
                    var idValue = GetProperty("Id");
                    int id;
                    if (idValue != null &&
                        int.TryParse(idValue.ToString(), out id))
                    {
                        return id < 1;
                    }
                    return false;
                }

                // check if the key property is an id
                var attribute = KeyInfo.GetAttributesOfType<KeyAttribute>()
                    .FirstOrDefault();
                if (attribute == null) return false;
                if (!attribute.IsIdentity) return false;

                // get the property that is decorated
                var propertyInfo = GetType().GetPropertyByName(KeyInfo.Name);

                var value = propertyInfo?.GetValue(this, null);
                if (value == null) return false;

                int identifier;
                if (int.TryParse(value.ToString(), out identifier))
                {
                    return (identifier < 1);
                }
                return false;
            }
        }

        [IgnoreOnMap]
        public bool IsModified => ChangedPropertyNames.Any();

        private bool _isDeleted;

        [JsonProperty, IgnoreOnMap]
        public virtual bool IsDeleted
        {
            get { return _isDeleted; }
            protected set { _isDeleted = value; }
        }

        public virtual bool IsReadOnly { get; set; }

        protected bool IsSerializing { get; set; }

        private PropertyInfo _keyInfo;
        protected PropertyInfo KeyInfo
        {
            get
            {
                return _keyInfo ??
                       (_keyInfo = GetType().GetAllProperties()
                           .FirstOrDefault(a => a.GetCustomAttributes(true).OfType<KeyAttribute>().Any()));
            }
        }

        private List<PropertyInfo> _handleOnNestingList;
        protected IEnumerable<PropertyInfo> HandleOnNestingList
        {
            get
            {
                return _handleOnNestingList ??
                       (_handleOnNestingList = GetType().GetAllProperties()
                           .Where(mp => mp.GetCustomAttributes(true).OfType<HandleOnNestingAttribute>().Any())
                           .ToList());
            }
        }

        private List<PropertyInfo> _sharedAggregations;
        protected IEnumerable<PropertyInfo> SharedAggregations
        {
            get
            {
                return _sharedAggregations ??
                       (_sharedAggregations = GetType().GetAllProperties()
                           .Where(mp => mp.GetCustomAttributes(true).OfType<AggregationAttribute>()
                               .Any(a => (a.IsShared && !a.IsIndependent)))
                           .ToList());
            }
        }

        private List<PropertyInfo> _compositeAggregations;
        private IEnumerable<PropertyInfo> CompositeAggregations
        {
            get
            {
                return _compositeAggregations ??
                       (_compositeAggregations = GetType().GetAllProperties()
                           .Where(mp => mp.GetCustomAttributes(true).OfType<AggregationAttribute>()
                               .Any(a => !a.IsShared))
                           .ToList());
            }
        }

        protected IDictionary<string, object> AggregationsStore;

        private readonly IDictionary<string, object> _removalsStore = new Dictionary<string, object>();
        
        private bool _isEditing;
        public bool IsEditing
        {
            get { return _isEditing; }
            set
            {
                _isEditing = value;
                OnPropertyChanged(() => IsEditing);
            }
        }

        public bool IsAutoEdit { get; set; } = true;

        private bool _isCurrent;
        public bool IsCurrent
        {
            get { return _isCurrent; }
            set
            {
                _isCurrent = value;
                OnPropertyChanged(() => IsCurrent);
            }
        }
        #endregion

        #region behavior

        public BaseEntity()
        {
        }

        public BaseEntity(PropertyInfo keyInfo)
        {
            _keyInfo = keyInfo;
        }

        public static void SetFindRepository(IRepositoryFinder repositoryFinder)
        {
            RepositoryFinder = repositoryFinder;
        }

        public override string ToString()
        {
            // check if default text is specified
            var defaultText = GetType().GetAllProperties()
                .FirstOrDefault(a => a.GetCustomAttributes(true).OfType<DefaultTextAttribute>().Any());

            var toString = defaultText != null ? defaultText.GetValue(this, null) as string : base.ToString();

            // if not use Key  info
            if (defaultText == null && KeyInfo != null)
            {
                toString = $"Type {GetType().Name} with keyinfo {KeyInfo.Name}: {GetKeyValue()}";
            }
            return toString ?? base.ToString();
        }

        public virtual void OnDeserializing(StreamingContext value)
        {
            UseRepositoryFinder = false;
        }

        public virtual void OnDeserialized(StreamingContext value)
        {
            UseRepositoryFinder = true;
        }

        public virtual void MarkForDeletion()
        {

        }

        #region setproperty

        public bool SetProperty<T>(T value, ref T field, Expression<Func<T>> propertyExpression, bool validate = true)
        {
            var body = propertyExpression?.Body as MemberExpression;
            if (body == null) return false;

            return SetProperty(value, ref field, body.Member.Name, validate);
        }

        public bool SetProperty<T>(T value, ref T field, string propertyName, bool validate = true)
        {
            ActOnPropertyChanging(propertyName);

            // check for trivial assignments
            var areSame = EqualityComparer<T>.Default.Equals(value, field);
            if (areSame && !(validate && HasValidationErrors))
                return false;

            // set field
            var oldValue = field;
            field = value;
            bool changed = false;

            if (!IsSerializing && !string.IsNullOrEmpty(propertyName))
            {
                // force the validation mechanism to refresh
                if (validate)
                    IsPropertyValid(propertyName, value);

                // Add change to history if propertychange is valid
                if (!areSame)
                {
                    SetChange(propertyName, oldValue);
                    changed = true;

                    if (!String.IsNullOrEmpty(propertyName))
                        OnPropertyChanged(propertyName);
                }
            }

            ActOnPropertyChanged(propertyName);

            return changed;
        }

        public void SetProperty(string propertyName, object value)
        {
            PropertyInfo propertyInfo = GetType().GetPropertyByName(propertyName);

            propertyInfo?.SetValue(this, value, null);
        }

        public void SetProperty<T>(Expression<Func<T>> propertyExpression, object value)
        {
            var body = propertyExpression?.Body as MemberExpression;
            if (body == null) return;

            SetProperty(body.Member.Name, value);
        }

        public void SetProperty<T>(Expression<Func<T, object>> propertyExpression, object value)
        {
            var body = propertyExpression?.Body as MemberExpression;
            if (body == null) return;

            SetProperty(body.Member.Name, value);
        }

        #endregion

        #region getproperty

        public object GetProperty(string propertyName)
        {
            PropertyInfo propertyInfo = GetType().GetPropertyByName(propertyName);

            return propertyInfo?.GetValue(this, null);
        }

        public object GetProperty<TProperty, TType>(Expression<Func<TProperty, TType>> propertyExpression)
        {
            var body = propertyExpression?.Body as MemberExpression;
            if (body == null) return false;

            return GetProperty(body.Member.Name);
        }

        #endregion

        #region getoriginalvalue

        public object GetOriginalValue<TProperty, TType>(Expression<Func<TProperty, TType>> propertyExpression)
        {
            var body = propertyExpression?.Body as MemberExpression;
            if (body == null) return false;

            return GetOriginalValue(body.Member.Name);
        }

        public object GetOriginalValue<T>(Expression<Func<T>> propertyExpression)
        {
            var body = propertyExpression?.Body as MemberExpression;
            if (body == null) return false;

            return GetOriginalValue(body.Member.Name);
        }

        public object GetOriginalValue(string propertyName)
        {
            // check if the original value has changed
            return ChangedPropertyNames.ContainsKey(propertyName) 
                ? ChangedPropertyNames[propertyName] 
                : GetProperty(propertyName);
        }

        #endregion

        #region ispropertychanged

        public bool IsPropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            var body = propertyExpression?.Body as MemberExpression;
            if (body == null) return false;

            return IsPropertyChanged(body.Member.Name);
        }

        public bool IsPropertyChanged<T>(Expression<Func<T, object>> propertyExpression)
        {
            var body = propertyExpression?.Body as MemberExpression;
            if (body == null) return false;

            return IsPropertyChanged(body.Member.Name);
        }

        public bool IsPropertyChanged(string propertyName)
        {
            // check if the original value has changed
            return ChangedPropertyNames.ContainsKey(propertyName);
        }

        public bool IsPersistableProperty(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return false;

            var propertyInfo = GetType().GetPropertyByName(propertyName);
            if (propertyInfo == null) return false;

            return !propertyInfo.GetCustomAttributes(typeof(IgnoreOnMapAttribute), true).Any();
        }

        #endregion

        public virtual bool HasValidationErrors => false;
        
        public bool HasCompositeAggregations => CompositeAggregations != null && CompositeAggregations.Any();

        public bool IsPropertyValid<T>(Expression<Func<T>> propertyExpression, 
            object value, bool forceValidation = false)
        {
            var body = propertyExpression?.Body as MemberExpression;
            if (body == null) return false;

            return IsPropertyValid(body.Member.Name, value, forceValidation);
        }

        public virtual bool IsPropertyValid(string propertyName, object value, bool forceValidation = false)
        {
            return true;
        }

        protected void SetChange(string propertyName, object value)
        {
            if (ChangedPropertyNames.All(kvp => kvp.Key != propertyName))
            {
                if (!ChangedPropertyNames.Any() && IsAutoEdit) BeginEdit();
                ChangedPropertyNames.Add(propertyName, value);
            }
        }

        public bool HasChanges()
        {
            bool hasChanges = IsModified || (IsNew && !IsDeleted) || (IsDeleted && !IsNew);
            if (hasChanges) return true;

            hasChanges = HaveNestedEntitiesChanges(dto => dto.HasChanges());
            if (hasChanges) return true;

            return false;
        }

        public virtual void Delete()
        {
            _isDeleted = true;

            MarkForDeletion();
            ActOnAllNestedEntities(dto => dto.Delete());
        }

        public virtual void UndoDelete()
        {
            _isDeleted = false;

            ActOnAllNestedEntities(dto => dto.UndoDelete());
        }

        /// <summary>
        /// For Mobile app => set the LocalId for added entities (those that have their id set to 0)
        /// </summary>
        protected virtual void ActOnPropertyChanging(string propertyName)
        {
        }

        protected virtual void ActOnPropertyChanged(string propertyName)
        {
        }

        protected virtual void SetIsValid()
        {
        }

        [OnDeserializing]
        public void OnDeserializating(StreamingContext value)
        {
            IsSerializing = true;

            OnDeserializing(value);
        }

        [OnDeserialized]
        public void OnDeserialization(StreamingContext value)
        {
            IsSerializing = false;

            OnDeserialized(value);
        }

        /// <summary>
        /// Method will perform a given action on all nested DTO (Recursive)
        /// </summary>
        protected void ActOnAllNestedEntities(Action<BaseEntity> action)
        {
            List<PropertyInfo> propertyInfos = GetType().GetPropertiesOfType<BaseEntity>()
                .ToList();

            foreach (var propertyInfo in propertyInfos)
            {
                if (!IsPartOfHandleOnNestingList(propertyInfo)) continue;

                if (isDeleteAction(action) &&
                    IsPartOfSharedAggregations(propertyInfo)) continue;
                
                object propertyValue = propertyInfo.GetValue(this, null);

                // check if we are dealing with a nested member
                var baseData = propertyValue as BaseEntity;
                if (baseData != null)
                {
                    action(baseData);
                }
                else
                {
                    // check if we are dealing with nested members
                    var values = propertyValue as IEnumerable;
                    if (values != null)
                    {
                        foreach (var item in values)
                        {
                            var baseEntity = item as BaseEntity;
                            if (baseEntity == null) continue;
                            action(baseEntity);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method will perform a given action on all nested DTO (Recursive)
        /// </summary>
        private bool HaveNestedEntitiesChanges(Func<BaseEntity, bool> action)
        {
            List<PropertyInfo> propertyInfos = GetType().GetPropertiesOfType<BaseEntity>()
                .ToList();

            var result = false;
            foreach (var propertyInfo in propertyInfos)
            {
                if (!IsPartOfHandleOnNestingList(propertyInfo)) continue;

                var propertyValue = propertyInfo.GetValue(this, null);

                var baseData = propertyValue as BaseEntity;
                if (baseData != null)
                {
                    if (action(baseData)) result = true;
                }
                else
                {
                    var values = propertyValue as IEnumerable;
                    if (values != null)
                    {
                        foreach (var item in values)
                        {
                            var baseEntity = item as BaseEntity;
                            if (baseEntity == null) continue;
                            if (action(baseEntity)) result = true;
                        }
                    }
                }
            }
            return result;
        }

        protected bool IsPartOfHandleOnNestingList(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null) return false;
            if (HandleOnNestingList == null) return false;

            return HandleOnNestingList.Any(item => item.PropertyType.FullName.Equals(propertyInfo.PropertyType.FullName));
        }

        private bool IsPartOfSharedAggregations(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null) return false;
            if (SharedAggregations == null) return false;

            return SharedAggregations.Any(item => item.PropertyType.FullName.Equals(propertyInfo.PropertyType.FullName));
        }

        public bool IsPartOfCompositeAggregations(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null) return false;
            if (CompositeAggregations == null) return false;

            return
                CompositeAggregations.Any(item => item.PropertyType.FullName.Equals(propertyInfo.PropertyType.FullName));
        }

        private bool isDeleteAction(Action<BaseEntity> action)
        {
            if (action == null) return false;

            return action.GetMethodInfo().Name.Contains("Delete");
        }

        public void ShowSharedAggregations()
        {
            ShowSharedNestedAggregations();

            AggregationsStore = null;
        }


        public void HideSharedAggregations()
        {
            AggregationsStore = new Dictionary<string, object>();
            HideSharedNestedAggregations();
        }

        private void HideSharedNestedAggregations()
        {
            SharedAggregations?.ToList()
                .ForEach(HideSharedAggregation);
            ActOnAllNestedEntities(e => e.HideSharedAggregations());
        }

        private void ShowSharedNestedAggregations()
        {
            SharedAggregations?.ToList()
                .ForEach(ShowSharedAggregation);
            ActOnAllNestedEntities(e => e.ShowSharedAggregations());
        }


        private void ShowSharedAggregation(PropertyInfo metadataPropertyInfo)
        {
            if (metadataPropertyInfo == null) return;

            var propertyInfo = GetType().GetPropertyByName(metadataPropertyInfo.Name);
            if (propertyInfo == null) return;

            var baseEntity = propertyInfo.GetValue(this, null) as BaseEntity;
            if (baseEntity != null) return;

            object storedEntity;
            if (AggregationsStore != null &&
                AggregationsStore.TryGetValue(metadataPropertyInfo.Name, out storedEntity))
            {
                propertyInfo.SetValue(this, storedEntity, null);
            }
        }

        private void HideSharedAggregation(PropertyInfo metadataPropertyInfo)
        {
            if (metadataPropertyInfo == null) return;

            var propertyInfo = GetType().GetPropertyByName(metadataPropertyInfo.Name);
            if (propertyInfo == null) return;

            object entity = null;
            var baseEntity = propertyInfo.GetValue(this, null) as BaseEntity;
            if (baseEntity != null) entity = baseEntity;
            var baseEntities = propertyInfo.GetValue(this, null) as ICollection;
            if (baseEntities != null) entity = baseEntities;

            if (AggregationsStore.ContainsKey(metadataPropertyInfo.Name)) return;

            AggregationsStore.Add(metadataPropertyInfo.Name, entity);

            propertyInfo.SetValue(this, null, null);
        }

        protected void RemoveChangedPropertyName(string propertyName)
        {
            if (ChangedPropertyNames == null) return;
            if (!ChangedPropertyNames.Any()) return;
            if (!ChangedPropertyNames.ContainsKey(propertyName)) return;

            ChangedPropertyNames.Remove(propertyName);
        }

        public IEnumerable<PropertyInfo> GetCompositeAggregations()
        {
            return CompositeAggregations;
        }

        public IEnumerable<string> GetChangedPropertyNames()
        {
            return ChangedPropertyNames.Keys;
        }

        public BaseEntity DeepClone()
        {
            return MemberwiseClone() as BaseEntity;
        }

        public TEntity ShallowClone<TEntity>() where TEntity : BaseEntity, new()
        {
            var clonedEntity = new TEntity();

            foreach (var propertyInfo in GetType().GetAllProperties()
                .Where(p => p.GetCustomAttributes(true).OfType<JsonPropertyAttribute>().Any()))
            {
                if (propertyInfo.IsCollectionOf<BaseEntity>()) continue;
                if (!propertyInfo.PropertyType.GetTypeInfo().IsValueType) continue;
                if (!propertyInfo.CanRead || !propertyInfo.CanWrite) continue;

                clonedEntity.SetProperty(propertyInfo.Name, GetProperty(propertyInfo.Name));
            }

            return clonedEntity;
        }

        public TEntity ShallowMerge<TEntity>(TEntity targetEntity) where TEntity : BaseEntity, new()
        {
            foreach (var propertyInfo in GetType().GetAllProperties()
                                                  .Where(p => p.GetCustomAttributes(true).OfType<JsonPropertyAttribute>().Any()))
            {
                if (propertyInfo.IsCollectionOf<BaseEntity>()) continue;
                if (!propertyInfo.PropertyType.GetTypeInfo().IsValueType) continue;
                if (!propertyInfo.CanRead || !propertyInfo.CanWrite) continue;
                if (!(propertyInfo.SetMethod?.IsPublic ?? false)) continue;
                targetEntity.SetProperty(propertyInfo.Name, GetProperty(propertyInfo.Name));
            }
            targetEntity.EndEdit();

            return targetEntity;
        }

        public object GetKeyValue()
        {
            var keyInfo = KeyInfo;
            if (keyInfo == null) return null;

            return GetProperty(keyInfo.Name);
        }

        private void CancelEditProperties()
        {
            var propertyNames = ChangedPropertyNames.Keys;
            foreach (var propertyName in propertyNames)
            {
                object value = ChangedPropertyNames[propertyName];

                SetProperty(propertyName, value);
            }
        }

        private void CancelEditMembers()
        {
            CompositeAggregations.ToList()
                                 .ForEach(pi =>
                                 {
                                    var collection = GetProperty(pi.Name) as IList;
                                    if (collection != null)
                                    {
                                        var newEntities = collection.Cast<object>().OfType<BaseEntity>()
                                                                    .Where(entity => entity.IsNew)
                                                                    .ToList();
                                        newEntities.ForEach(collection.Remove);
                                    }
                                 });
        }

    #endregion

        #region IEditableObject methods
        public virtual void BeginEdit()
        {
            IsEditing = true;
            if (IsAutoEdit)
                ActOnAllNestedEntities(e => e.BeginEdit());
        }

        public virtual void CancelEdit()
        {
            // change the properties back to their original value
            CancelEditProperties();
            
            // check if deleted, if yes rollback
            if (_isDeleted) _isDeleted = false;
           
            if (IsAutoEdit)
                ActOnAllNestedEntities(e => e.CancelEdit());

            // all the state of the members is cancelled - now handle the collection 
            // (only add - delete is handle by entity.)
            CancelEditMembers();

            EndEdit();
        }

        public virtual void EndEdit()
        {
            ChangedPropertyNames.Clear();
            _removalsStore.Clear();
            
            IsEditing = false;
            if (IsAutoEdit)
                ActOnAllNestedEntities(e => e.EndEdit());
        }
        #endregion
    }
}
