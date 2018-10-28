using System;
using System.Collections.Generic;

namespace Dccn.ProjectForm.Utils
{
    public abstract class EnumClassUtils<TClass> where TClass : class
    {
        public static IEnumerable<TEnum> GetValues<TEnum>() where TEnum : TClass
        {
            return (TEnum[]) Enum.GetValues(typeof(TEnum));
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class EnumUtils : EnumClassUtils<Enum>
    {
        private EnumUtils()
        {
        }
    }
}