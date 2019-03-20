using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dccn.ProjectForm.Extensions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Nett;

namespace Dccn.ProjectForm.Services
{
    public class TomlStringLocalizer<T> : TomlStringLocalizer, IStringLocalizer<T>
    {
        public TomlStringLocalizer(IStringLocalizerFactory factory, IHostingEnvironment environment) : base(factory, environment)
        {
        }
//
//        protected override IEnumerable<string> Prefix { get; } =
//            typeof(T).GetCustomAttribute<TextAttribute>()?.Key.Split('.') ?? Enumerable.Empty<string>();
    }

    public class TomlStringLocalizer : IStringLocalizer
    {
        private readonly TomlTable _root;
        private readonly IHostingEnvironment _environment;

        [UsedImplicitly]
        public TomlStringLocalizer(IStringLocalizerFactory factory, IHostingEnvironment environment)
        {
            _root = ((TomlStringLocalizerFactory) factory).Root;
            _environment = environment;
        }

        protected virtual IEnumerable<string> Prefix => Enumerable.Empty<string>();

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return Recurse(Enumerable.Empty<string>(), _root);

            IEnumerable<LocalizedString> Recurse(IEnumerable<string> name, TomlObject value)
            {
                switch (value)
                {
                    case TomlTable table:
                        return table.SelectMany(entry => Recurse(name.Append(entry.Key), entry.Value));
                    case TomlString str:
                        return new LocalizedString(string.Join('.', name), str.Value).Yield();
                    default:
                        return Enumerable.Empty<LocalizedString>();
                }
            }
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            return this;
        }

        public LocalizedString this[string name]
        {
            get
            {
                var value = FindString(name);
                return value == null
                    ? new LocalizedString(name, name, true) // _environment.IsDevelopment() ? $"MISSING: {name}" : name, true)
                    : new LocalizedString(name, value);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var value = FindString(name);
                return value == null
                    ? new LocalizedString(name, name, true) //_environment.IsDevelopment() ? $"MISSING: {name}" : name, true)
                    : new LocalizedString(name, string.Format(value, arguments));
            }
        }

        private string FindString(string name)
        {
            var path = Prefix.Concat(name.Split('.')).ToList();
            var table = _root;
            foreach (var segment in path.SkipLast(1))
            {
                if (!table.TryGetValue(segment, out var next))
                {
                    return null;
                }

                if ((table = next as TomlTable) == null)
                {
                    return null;
                }
            }

            return table.TryGetValue(path.Last(), out var value) ? (value as TomlString)?.Value?.Trim() : null;
        }
    }

    public class TomlStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public TomlStringLocalizerFactory(IServiceProvider serviceProvider, string path)
        {
            Root = Toml.ReadFile(path);
            Root.Freeze();
            _serviceProvider = serviceProvider;
        }

        public TomlTable Root { get; }

        public IStringLocalizer Create(Type resourceSource)
        {
            return (IStringLocalizer) _serviceProvider.GetRequiredService(typeof(IStringLocalizer<>).MakeGenericType(resourceSource));
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            //return (IStringLocalizer) _serviceProvider.GetRequiredService(typeof(IStringLocalizer).MakeGenericType());
            throw new InvalidOperationException();
        }
    }
}