using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace VLM.DAS2.Core.Extensions
{
    public static class TypeExtensions
    {
        public static IEnumerable<PropertyInfo> GetAllProperties(this Type type)
        {
            IEnumerable<PropertyInfo> propertyList = type.GetTypeInfo().DeclaredProperties;
            if (type.GetTypeInfo().BaseType != null)
            {
                propertyList = propertyList.Concat(GetAllProperties(type.GetTypeInfo().BaseType));
            }
            return propertyList;
        }

        public static IEnumerable<MethodInfo> GetAllMethods(this Type type)
        {
            IEnumerable<MethodInfo> methodList = type.GetTypeInfo().DeclaredMethods;
            if (type.GetTypeInfo().BaseType != null)
            {
                methodList = methodList.Concat(GetAllMethods(type.GetTypeInfo().BaseType));
            }
            return methodList;
        }

        public static IEnumerable<FieldInfo> GetAllFields(this Type type)
        {
            IEnumerable<FieldInfo> fieldList = type.GetTypeInfo().DeclaredFields;
            if (type.GetTypeInfo().BaseType != null)
            {
                fieldList = fieldList.Concat(GetAllFields(type.GetTypeInfo().BaseType));
            }
            return fieldList;
        }


        public static IEnumerable<PropertyInfo> GetPropertiesOfType<T>(this Type type)
        {
            var propertyInfos = new List<PropertyInfo>();

            var results = type.GetAllProperties()
                .Where(p => p.PropertyType.GetTypeInfo().IsSubclassOf(typeof(T)))
                .ToList() ;
            if (results.Any()) propertyInfos.AddRange(results);

            results = type.GetAllProperties()
                .Where(p => p.IsCollectionOf<T>())
                .ToList();
            if (results.Any()) propertyInfos.AddRange(results);

            return propertyInfos;
        }

        /// <summary>
        /// Returns all properties marked with the specified Attribute
        /// </summary>
        public static IEnumerable<PropertyInfo> GetPropertiesByAttribute<TAttribute>(this Type type) where TAttribute : Attribute
        {
            var attrProps = new List<PropertyInfo>();
            var properties = type.GetAllProperties().ToList();

            attrProps.AddRange(properties.Where(p => p.GetCustomAttributes(true).OfType<TAttribute>().Any()));

            return attrProps;
        }

        /// <summary>
        /// Returns property matching the given propertyName
        /// </summary>
        public static PropertyInfo GetPropertyByName(this Type type, string propertyName)
        {
            return type.GetAllProperties()
                       .FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns property matching the given FieldName
        /// </summary>
        public static FieldInfo GetFieldByName(this Type type, string fieldName)
        {
            return type.GetAllFields()
                       .FirstOrDefault(p => p.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns property matching the given FieldName
        /// </summary>
        public static MethodInfo GetMethodByName(this Type type, string methodName)
        {
            return type.GetAllMethods()
                       .FirstOrDefault(p => p.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns true in case when the specified type is a collection Type
        /// </summary>
        public static bool IsCollection(this Type type)
        {
            if (type == null) return false;
            var tCollection = typeof(ICollection<>);

            if (type.GetTypeInfo().IsGenericType && 
                tCollection.GetTypeInfo().IsAssignableFrom(type.GetGenericTypeDefinition().GetTypeInfo()) ||
                type.GetTypeInfo().ImplementedInterfaces.Any(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == tCollection))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Return true in case when the property is a collection of a specified type
        /// </summary>
        public static bool IsCollectionOf<T>(this Type type)
        {

            if (type.IsCollection() && 
                type.GetTypeInfo().GenericTypeArguments.Any(a => a.GetTypeInfo().IsSubclassOf(typeof(T))))
            {
                return true;
            }

            return false;
        }
    }
}
