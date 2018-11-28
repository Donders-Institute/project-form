using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Dccn.ProjectForm.Extensions
{
    public static class EnumExtensions
    {
        public static string GetName<TEnum>(this TEnum @enum) where TEnum : Enum
        {
            return Enum.GetName(typeof(TEnum), @enum);
        }

        public static string GetDisplayName<TEnum>(this TEnum @enum) where TEnum : Enum
        {
            return GetDisplayAttribute(@enum)?.GetName() ?? @enum.GetName();
        }

        public static string GetDisplayDescription<TEnum>(this TEnum @enum) where TEnum : Enum
        {
            return GetDisplayAttribute(@enum)?.GetDescription();
        }

        private static DisplayAttribute GetDisplayAttribute<TEnum>(TEnum @enum) where TEnum : Enum
        {
            return typeof(TEnum)
                .GetMember(@enum.GetName())
                .First()
                .GetCustomAttribute<DisplayAttribute>();
        }
    }
}