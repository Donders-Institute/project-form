using System;
using System.Collections.Generic;
using System.IO;

namespace Dccn.ProjectForm.Extensions
{
    public static class StringExtensions
    {
        public static IEnumerable<string> NonEmptyLines(this string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            using (var reader = new StringReader(str))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        yield return line;
                    }
                }
            }
        }
    }
}