//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Mvp24Hours.Extensions
{
    public static class EnumExtensions
    {
        public static string GetEnumDescription<TEnum>(string value)
        {
            Type type = typeof(TEnum);
            var name = Enum.GetNames(type).Where(f => f.Equals(value, StringComparison.CurrentCultureIgnoreCase)).Select(d => d).FirstOrDefault();

            if (name == null)
            {
                return string.Empty;
            }
            var field = type.GetField(name);
            var customAttribute = field.GetCustomAttributes(typeof(DisplayAttribute), false);
            return customAttribute.Length > 0 ? ((DisplayAttribute)customAttribute[0]).Description : name;
        }

        public static string GetEnumValue<TEnum>(string value)
        {
            Type type = typeof(TEnum);
            var name = Enum.GetNames(type).Where(f => f.Equals(value, StringComparison.CurrentCultureIgnoreCase)).Select(d => d).FirstOrDefault();

            if (name == null)
            {
                return string.Empty;
            }
            var field = type.GetField(name);

            return field.GetRawConstantValue().ToString();
        }

        public static string GetGroupName(this Enum value)
        {
            var type = value.GetType();
            if (!type.IsEnum)
            {
                throw new ArgumentException(String.Format("Type '{0}' is not Enum", type));
            }

            var members = type.GetMember(value.ToString());
            if (members.Length == 0)
            {
                throw new ArgumentException(String.Format("Member '{0}' not found in type '{1}'", value, type.Name));
            }

            var member = members[0];
            var attributes = member.GetCustomAttributes(typeof(DisplayAttribute), false);
            if (attributes.Length == 0)
            {
                return value.ToString();
            }

            var attribute = (DisplayAttribute)attributes[0];
            var groupName = attribute.GetGroupName();
            return string.IsNullOrEmpty(groupName) ? value.ToString() : groupName;
        }

        public static string GetDisplayName(this Enum value)
        {
            var type = value.GetType();
            if (!type.IsEnum)
            {
                throw new ArgumentException(String.Format("Type '{0}' is not Enum", type));
            }

            var members = type.GetMember(value.ToString());
            if (members.Length == 0)
            {
                throw new ArgumentException(String.Format("Member '{0}' not found in type '{1}'", value, type.Name));
            }

            var member = members[0];
            var attributes = member.GetCustomAttributes(typeof(DisplayAttribute), false);
            if (attributes.Length == 0)
            {
                return value.ToString();
            }

            var attribute = (DisplayAttribute)attributes[0];
            return attribute.GetName();
        }
    }
}
