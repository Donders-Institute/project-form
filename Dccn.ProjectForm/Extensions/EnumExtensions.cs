using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.DataAnnotations;

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