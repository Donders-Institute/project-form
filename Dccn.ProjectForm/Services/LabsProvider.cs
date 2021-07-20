using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Configuration;
using Dccn.ProjectForm.Data.ProjectDb;
using Dccn.ProjectForm.Extensions;
using Dccn.ProjectForm.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dccn.ProjectForm.Services
{
    public class LabProvider : ILabProvider
    {
        private readonly FormOptions.LabOptions _labOptions;

        public LabProvider(IOptions<FormOptions> options)
        {
            _labOptions = options.Value.Labs;
        }

        public async Task InitializeAsync(IServiceProvider services)
        {
            var projectsDbContext = services.GetRequiredService<ProjectDbContext>();

            var queryResults = await _labOptions.Modalities
                .Select(async entry =>
                {
                    var (id, modality) = entry;
                    return new
                    {
                        Id = id,
                        Config = modality,
                        Db = await projectsDbContext.ImagingMethods.FindAsync(modality.MethodId)
                    };
                })
                .ToListAsync();

            Labs = queryResults
                .Select(entry => new ModalityModel
                {
                    Id = entry.Id,
                    DisplayName = entry.Config.DisplayName ?? entry.Db.Description,
                    IsMri = entry.Config.IsMri,
                    SessionStorageQuota = entry.Db.SessionQuota,
                    Hidden = entry.Config.Hidden
                })
                .ToImmutableDictionary(modality => modality.Id);
        }

        public int MinimumStorageQuota => _labOptions.MinimumStorageQuota;
        public IReadOnlyDictionary<string, ModalityModel> Labs { get; private set; }

        public string GetImagingMethodId(string id)
        {
            return _labOptions.Modalities[id]?.MethodId;
        }

        public bool ModalityExists(string id)
        {
            return _labOptions.Modalities.ContainsKey(id);
        }
    }

    public interface ILabProvider
    {
        Task InitializeAsync(IServiceProvider services);
        int MinimumStorageQuota { get; }
        IReadOnlyDictionary<string, ModalityModel> Labs { get; }
        string GetImagingMethodId(string id);
        bool ModalityExists(string id);
    }
}
