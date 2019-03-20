using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dccn.ProjectForm.Configuration;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Extensions;
using Dccn.ProjectForm.Models;
using Microsoft.Extensions.Options;

namespace Dccn.ProjectForm.Services.SectionHandlers
{
    public class PrivacySectionHandler : FormSectionHandlerBase<PrivacySectionModel>
    {
        private readonly FormOptions.PrivacyOptions _options;

        public PrivacySectionHandler(IServiceProvider serviceProvider, IOptionsSnapshot<FormOptions> options)
            : base(serviceProvider, m => m.Privacy)
        {
            _options = options.Value.Privacy;
        }

        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => new [] {ApprovalAuthorityRole.Privacy};

        protected override Task LoadAsync(PrivacySectionModel model, Proposal proposal)
        {
            (model.DataTypes, model.CustomDataTypes) = GetKeywordModels(proposal.PrivacyDataTypes, _options.DataTypes);
            (model.Motivations, model.CustomMotivations) = GetKeywordModels(proposal.PrivacyMotivations, _options.Motivations);
            (model.StorageLocations, model.CustomStorageLocations) = GetKeywordModels(proposal.PrivacyStorageLocations, _options.StorageLocations);
            (model.DataAccessors, model.CustomDataAccessors) = GetKeywordModels(proposal.PrivacyDataAccessors, _options.DataAccessors);

            model.SecurityMeasures = proposal.PrivacySecurityMeasures;

            model.DataDisposalTerm = proposal.PrivacyDataDisposalTerm;
            model.DataDisposalTerms = _options.DataDisposalTerms;

            return base.LoadAsync(model, proposal);
        }

        protected override Task StoreAsync(PrivacySectionModel model, Proposal proposal)
        {
            proposal.PrivacyDataTypes = GetKeywords(model.DataTypes, model.CustomDataTypes);
            proposal.PrivacyMotivations = GetKeywords(model.Motivations, model.CustomMotivations);
            proposal.PrivacyStorageLocations = GetKeywords(model.StorageLocations, model.CustomStorageLocations);
            proposal.PrivacyDataAccessors = GetKeywords(model.DataAccessors, model.CustomDataAccessors);

            proposal.PrivacySecurityMeasures = model.SecurityMeasures;

            proposal.PrivacyDataDisposalTerm = model.DataDisposalTerm;

//            proposal.PrivacyDataDisposalTerm = model.DataDisposalTermDays.HasValue
//                ? TimeSpan.FromDays(model.DataDisposalTermDays.Value)
//                : (TimeSpan?) null;

            return base.StoreAsync(model, proposal);
        }

        private static (IDictionary<string, PrivacyKeywordModel> Standard, string Custom) GetKeywordModels(ICollection<string> keywords, IDictionary<string, string> options)
        {
            var standard = new Dictionary<string, PrivacyKeywordModel>();
            var custom = new StringBuilder();

            foreach (var (keyword, name) in options)
            {
                standard.Add(keyword, new PrivacyKeywordModel
                {
                    Name = name,
                    Present = keywords.Contains("_" + keyword)
                });
            }

            foreach (var keyword in keywords)
            {
                if (!options.ContainsKey(Regex.Replace(keyword, "^_", string.Empty)))
                {
                    custom.AppendLine(keyword);
                }
            }

            return (standard, custom.ToString());
        }

        private static ICollection<string> GetKeywords(IDictionary<string, PrivacyKeywordModel> standard, string custom)
        {
            return standard
                .Where(k => k.Value.Present)
                .Select(k => "_" + k.Key)
                .Concat(custom?.NonEmptyLines().Select(k => k.Trim()) ?? Enumerable.Empty<string>())
                .Distinct()
                .ToList();
        }
    }
}