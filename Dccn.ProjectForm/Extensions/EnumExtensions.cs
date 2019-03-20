using System;

namespace Dccn.ProjectForm.Extensions
{
    public static class EnumExtensions
    {
        public static string GetName<TEnum>(this TEnum @enum) where TEnum : Enum
        {
            return Enum.GetName(typeof(TEnum), @enum);
        }
    }
}