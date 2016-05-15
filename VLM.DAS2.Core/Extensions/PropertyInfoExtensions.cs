using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace VLM.DAS2.Core.Extensions
{
    public static class PropertyInfoExtensions
    {
        /// <summary>
        /// Return true in case when the property is a collection of a specified type
        /// </summary>
        public static bool IsCollectionOf<T>(this PropertyInfo propertyInfo)
        {
            if (propertyInfo == null) return false;

            if (propertyInfo.PropertyType.IsCollection() && 
                propertyInfo.PropertyType.GetTypeInfo().GenericTypeArguments.Any(a => a.GetTypeInfo().IsSubclassOf(typeof(T))))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns an attribute collection by a given attribute derived type defined on a property level
        /// </summary>
        public static IEnumerable<T> GetAttributesOfType<T>(this PropertyInfo propertyInfo) where T:Attribute
        {
            return propertyInfo.GetCustomAttributes(true).OfType<T>();
        }
        
    }
}
