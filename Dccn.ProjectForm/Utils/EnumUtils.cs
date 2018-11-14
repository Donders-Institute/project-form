using System;
using System.Collections.Generic;

namespace Dccn.ProjectForm.Utils
{
    public static class EnumUtils
    {
        public static IEnumerable<TEnum> GetValues<TEnum>() where TEnum : Enum
        {
            return (TEnum[]) Enum.GetValues(typeof(TEnum));
        }
    }
}