using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Dccn.ProjectForm.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum @enum)
        {
            return GetDisplayAttribute(@enum)?.GetName() ?? Enum.GetName(@enum.GetType(), @enum);
        }

        public static string GetDisplayDescription(this Enum @enum)
        {
            return GetDisplayAttribute(@enum)?.GetDescription();
        }

        private static DisplayAttribute GetDisplayAttribute(Enum @enum)
        {
            return @enum.GetType()
                .GetMember(@enum.ToString())
                .First()
                .GetCustomAttribute<DisplayAttribute>();
        }
    }
}