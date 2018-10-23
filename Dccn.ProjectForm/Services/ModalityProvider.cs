using System.Collections.Generic;
using System.Linq;
using Dccn.ProjectForm.Configuration;
using Dccn.ProjectForm.Models;
using Microsoft.Extensions.Options;

namespace Dccn.ProjectForm.Services
{
    public class ModalityProvider : IModalityProvider
    {
        private readonly IDictionary<string, Modality> _values;

        public ModalityProvider(IOptionsSnapshot<FormOptions> options)
        {
            _values = options.Value.Labs
                .Select(entry => new Modality
                {
                    Id = entry.Key,
                    DisplayName = entry.Value.DisplayName ?? entry.Key,
                    FixedStorage = entry.Value.Storage.Fixed.GetValueOrDefault(),
                    SessionStorage = entry.Value.Storage.Session.GetValueOrDefault()
                })
                .ToDictionary(modality => modality.Id);
        }

        public IEnumerable<Modality> Values => _values.Values;

        public bool Exists(string key)
        {
            return _values.ContainsKey(key);
        }

        public bool TryGetValue(string key, out Modality value)
        {
            return _values.TryGetValue(key, out value);
        }

        public Modality this[string key] => _values[key];
    }

    public interface IModalityProvider
    {
        IEnumerable<Modality> Values { get; }
        bool Exists(string key);
        bool TryGetValue(string key, out Modality value);
        Modality this[string key] { get; }
    }
}
