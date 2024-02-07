using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace Mvp24Hours.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Extension for 'Object' that copies the properties to a destination object.
        /// <see href="https://stackoverflow.com/questions/930433/apply-properties-values-from-one-object-to-another-of-the-same-type-automaticall"/>
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="PropertiesToIgnore">an optional list of property names which will NOT be copied</param>
        /// <param name="IgnoreNullProperties">when true will not update properties where the source is null</param>
        /// <param name="CoerceDataType">when true, will attempt to coerce the source property to the destination property (e.g. int to decimal) </param>
        /// <param name="ThrowOnTypeMismatch">when true, will throw a InvalidCastException if the data cannot be coerced</param>
        /// <exception cref="InvalidCastException">if there is a data type mismatch between source/destination and ThrowOnTypeMismatch is enabled and unable to coerce the data type.</exception>
        /// <returns>true if any properties were changed</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Low complexity")]
        public static bool CopyPropertiesTo<T, U>(this T source, U destination, IEnumerable<string> PropertiesToIgnore = null, bool IgnoreNullProperties = false, bool ThrowOnTypeMismatch = true, bool CoerceDataType = false)
            where T : class
            where U : class
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destination);

            // Getting the Types of the objects
            Type typeDest = destination.GetType();
            Type typeSrc = source.GetType();

            // Collect all the valid properties to map
            var results = (from srcProp in typeSrc.GetProperties()
                           let targetProperty = typeDest.GetProperty(srcProp.Name)
                           where srcProp.CanRead
                           && targetProperty != null
                           && (targetProperty.GetSetMethod(true) != null && !targetProperty.GetSetMethod(true).IsPrivate)
                           && (targetProperty.GetSetMethod().Attributes & MethodAttributes.Static) == 0
                           && !(
                               from i in PropertiesToIgnore ?? Enumerable.Empty<string>()
                               select i
                             ).Contains(srcProp.Name)
                           && (!IgnoreNullProperties || srcProp.GetValue(source, null) != null)
                           select new { sourceProperty = srcProp, targetProperty }).ToList();

            bool PropertyChanged = false;
            //map the properties
            foreach (var props in results)
            {
                var srcValue = props.sourceProperty.GetValue(source, null);
                var dstValue = props.targetProperty.GetValue(destination, null);
                if (props.targetProperty.PropertyType.IsAssignableFrom(props.sourceProperty.PropertyType))
                    props.targetProperty.SetValue(destination, srcValue, null);
                else
                {
                    try
                    {
                        if (!CoerceDataType)
                            throw new InvalidCastException($"Types do not match, source: {props.sourceProperty.PropertyType.FullName}, target: {props.targetProperty.PropertyType.FullName}.");

                        if (srcValue != null)
                        {
                            // determine if nullable type
                            Type tgtType = Nullable.GetUnderlyingType(props.targetProperty.PropertyType);
                            // if it is, use the underlying type
                            // without this we cannot convert int? -> decimal? when value is not null
                            if (tgtType != null)
                                props.targetProperty.SetValue(destination, Convert.ChangeType(srcValue, tgtType, CultureInfo.InvariantCulture), null);
                            else // otherwise use the original type
                                props.targetProperty.SetValue(destination, Convert.ChangeType(srcValue, props.targetProperty.PropertyType, CultureInfo.InvariantCulture), null);
                        }
                        else // if null we can just set it as null
                            props.targetProperty.SetValue(destination, null, null);
                    }
                    catch (Exception ex)
                    {
                        if (ThrowOnTypeMismatch)
                            throw new InvalidCastException($"Unable to copy property {props.sourceProperty.Name} with value {srcValue} from object of type ({typeSrc.FullName}) to type ({typeDest.FullName}), Error: {ex.Message}");
                        // else ignore update
                    }
                    var newdstValue = props.targetProperty.GetValue(destination, null);
                    if (newdstValue != null && dstValue != null && !newdstValue.Equals(dstValue))
                        PropertyChanged = true;
                }
            }

            return PropertyChanged;
        }

        public static T DeepClone<T>(this T objectToClone)
            where T : class
        {
            return ObjectHelper.Clone<T>(objectToClone);
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool InheritsOrImplements(this Type child, Type parent)
        {
            parent = ResolveGenericTypeDefinition(parent);

            var currentChild = child.IsGenericType
                                   ? child.GetGenericTypeDefinition()
                                   : child;

            while (currentChild != typeof(object))
            {
                if (parent == currentChild || HasAnyInterfaces(parent, currentChild))
                    return true;

                currentChild = currentChild.BaseType != null
                               && currentChild.BaseType.IsGenericType
                                   ? currentChild.BaseType.GetGenericTypeDefinition()
                                   : currentChild.BaseType;

                if (currentChild == null)
                    return false;
            }
            return false;
        }

        private static bool HasAnyInterfaces(Type parent, Type child)
        {
            return child.GetInterfaces()
                .AnySafe(childInterface =>
                {
                    var currentInterface = childInterface.IsGenericType
                        ? childInterface.GetGenericTypeDefinition()
                        : childInterface;

                    return currentInterface == parent;
                });
        }

        private static Type ResolveGenericTypeDefinition(Type parent)
        {
            var shouldUseGenericType = true;
            if (parent.IsGenericType && parent.GetGenericTypeDefinition() != parent)
                shouldUseGenericType = false;

            if (parent.IsGenericType && shouldUseGenericType)
                parent = parent.GetGenericTypeDefinition();
            return parent;
        }

        public static void SetPropValue(this object obj, string name, object value)
        {
            PropertyInfo property = obj.GetType().GetProperty(name);
            Type t = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            object safeValue = (value == null) ? null : Convert.ChangeType(value, t);

            property.SetValue(obj, safeValue, null);
        }

        public static object GetPropValue(this object obj, string name)
        {
            foreach (string part in name.Split('.'))
            {
                if (obj == null) { return null; }

                Type type = obj.GetType();
                PropertyInfo info = type.GetProperty(part);
                if (info == null) { return null; }

                obj = info.GetValue(obj, null);
            }
            return obj;
        }

        public static T GetPropValue<T>(this object obj, string name)
        {
            object retval = GetPropValue(obj, name);
            if (retval == null) { return default; }

            // throws InvalidCastException if types are incompatible
            return (T)retval;
        }
    }
}
