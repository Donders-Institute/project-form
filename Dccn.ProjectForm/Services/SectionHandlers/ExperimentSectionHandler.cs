using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Extensions;
using Dccn.ProjectForm.Models;

namespace Dccn.ProjectForm.Services.SectionHandlers
{
    public class ExperimentSectionHandler : FormSectionHandlerBase<ExperimentSectionModel>
    {
        private readonly IUserManager _userManager;
        private readonly ILabProvider _labProvider;

        public ExperimentSectionHandler(IServiceProvider serviceProvider, IUserManager userManager, ILabProvider labProvider)
            : base(serviceProvider, m => m.Experiment)
        {
            _userManager = userManager;
            _labProvider = labProvider;
        }

        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => new []
        {
            ApprovalAuthorityRole.LabMri, ApprovalAuthorityRole.LabOther
        };

        public override bool IsAuthorityApplicable(Proposal proposal, ApprovalAuthorityRole authorityRole)
        {
            var hasAnyMri = proposal.Labs.Any(l => _labProvider.Labs[l.Modality].IsMri);
            var hasAnyNonMri = proposal.Labs.Any(l => !_labProvider.Labs[l.Modality].IsMri);
            switch (authorityRole)
            {
                case ApprovalAuthorityRole.LabMri:
                    return hasAnyMri;
                case ApprovalAuthorityRole.LabOther:
                    return hasAnyNonMri || !hasAnyMri;
                default:
                    throw new ArgumentOutOfRangeException(nameof(authorityRole), authorityRole, null);
            }
        }

        protected override async Task LoadAsync(ExperimentSectionModel model, Proposal proposal)
        {
            model.StartDate = proposal.StartDate;
            model.EndDate = proposal.EndDate;

            model.Labs = proposal.Labs
                .Select(lab => new LabModel
                {
                    Id = lab.Id,
                    Modality = _labProvider.Labs[lab.Modality],
                    SubjectCount = lab.SubjectCount,
                    ExtraSubjectCount = lab.ExtraSubjectCount,
                    SessionCount = lab.SessionCount,
                    SessionDurationMinutes = (int?) lab.SessionDuration?.TotalMinutes
                })
                .ToImmutableSortedDictionary(lab => lab.Id, lab => lab);

            if (proposal.CustomQuota)
            {
                model.StorageQuota = StorageQuotaModel.Custom;
                model.CustomStorageQuota = proposal.CustomQuotaAmount;
                model.CustomStorageQuotaMotivation = proposal.CustomQuotaMotivation;
            }
            else
            {
                model.StorageQuota = StorageQuotaModel.Standard;
            }

            model.MinimumStorageQuota = _labProvider.MinimumStorageQuota;

            model.Experimenters = await proposal.Experimenters
                .Select(async experimenter => new ExperimenterModel
                {
                    Id = experimenter.UserId,
                    Name = (await _userManager.GetUserByIdAsync(experimenter.UserId)).DisplayName
                })
                .ToImmutableSortedDictionaryAsync(experimenter => experimenter.Id);

            await base.LoadAsync(model, proposal);
        }

        protected override Task StoreAsync(ExperimentSectionModel model, Proposal proposal)
        {
            proposal.StartDate = model.StartDate;
            proposal.EndDate = model.EndDate;

            foreach (var labUpdate in model.Labs.Values)
            {
                var lab = proposal.Labs.FirstOrDefault(l => l.Id == labUpdate.Id);
                if (lab == null)
                {
                    continue;
                }

                lab.SubjectCount = labUpdate.SubjectCount;
                lab.ExtraSubjectCount = labUpdate.ExtraSubjectCount;
                lab.SessionCount = labUpdate.SessionCount;
                lab.SessionDuration = labUpdate.SessionDurationMinutes.HasValue
                    ? TimeSpan.FromMinutes(labUpdate.SessionDurationMinutes.Value)
                    : (TimeSpan?) null;
            }

            if (model.StorageQuota == StorageQuotaModel.Standard)
            {
                proposal.CustomQuota = false;
                proposal.CustomQuotaAmount = null;
                proposal.CustomQuotaMotivation = null;
            }
            else
            {
                proposal.CustomQuota = true;
                proposal.CustomQuotaAmount = model.CustomStorageQuota;
                proposal.CustomQuotaMotivation = model.CustomStorageQuotaMotivation;
            }

            proposal.Experimenters = model.Experimenters
                .Values
                .Select(experimenter => new Experimenter
                {
                    UserId = experimenter.Id
                })
                .ToList();

            return base.StoreAsync(model, proposal);
        }

        public override bool SectionEquals(Proposal x, Proposal y) =>
            x.StartDate == y.StartDate
            && x.EndDate == y.EndDate
            && CompareKeyedCollections(x.Labs, y.Labs, l => l.Id, (lx, ly) =>
                    lx.Id == ly.Id
                    && lx.Modality == ly.Modality
                    && lx.SubjectCount == ly.SubjectCount
                    && lx.ExtraSubjectCount == ly.ExtraSubjectCount
                    && lx.SessionCount == ly.SessionCount
                    && lx.SessionDuration == ly.SessionDuration)
            && x.CustomQuota == y.CustomQuota
            && x.CustomQuotaAmount == y.CustomQuotaAmount
            && x.CustomQuotaMotivation == y.CustomQuotaMotivation
            && CompareKeyedCollections(x.Experimenters, y.Experimenters, e => e.UserId, (ex, ey) =>
                    ex.UserId == ey.UserId)
            && base.SectionEquals(x, y);

    }
}