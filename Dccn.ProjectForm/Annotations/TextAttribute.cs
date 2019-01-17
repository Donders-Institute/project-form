using System;
using Microsoft.Extensions.Localization;

namespace Dccn.ProjectForm.Annotations
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TextAttribute : Attribute
    {
        public TextAttribute(string key)
        {
            Key = key;
        }

        public string Key { get; }
    }
}