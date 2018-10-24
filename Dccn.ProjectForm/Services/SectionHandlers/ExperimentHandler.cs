using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.Projects;
using Dccn.ProjectForm.Extensions;
using Dccn.ProjectForm.Models;
using Microsoft.EntityFrameworkCore;

namespace Dccn.ProjectForm.Services.SectionHandlers
{
    public class ExperimentHandler : FormSectionHandlerBase<Experiment>
    {
        private readonly ProjectsDbContext _projectsDbContext;
        private readonly IModalityProvider _modalityProvider;

        public ExperimentHandler(IAuthorityProvider authorityProvider, ProjectsDbContext projectsDbContext, IModalityProvider modalityProvider) : base(authorityProvider)
        {
            _projectsDbContext = projectsDbContext;
            _modalityProvider = modalityProvider;
        }

        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => new []
        {
            ApprovalAuthorityRole.LabMri, ApprovalAuthorityRole.LabOther
        };

        public override async Task LoadAsync(Experiment model, Proposal proposal, ProjectsUser owner, ProjectsUser supervisor)
        {
            model.StartDate = proposal.StartDate;
            model.EndDate = proposal.EndDate;

            model.Labs = proposal.Labs
                .Select(lab => new Models.Lab
                {
                    Id = lab.Id,
                    Modality = _modalityProvider[lab.Modality],
                    SubjectCount = lab.SubjectCount,
                    ExtraSubjectCount = lab.ExtraSubjectCount,
                    SessionCount = lab.SessionCount,
                    SessionDurationMinutes = (int?) lab.SessionDuration?.TotalMinutes
                })
                .ToDictionary(_ => Guid.NewGuid());

            if (proposal.CustomQuota)
            {
                model.StorageQuota = Experiment.StorageQuotaType.Custom;
                model.CustomQuotaAmount = proposal.CustomQuotaAmount;
                model.CustomQuotaMotivation = proposal.CustomQuotaMotivation;
            }
            else
            {
                model.StorageQuota = Experiment.StorageQuotaType.Standard;
            }

            model.Experimenters = await proposal.Experimenters
                .Select(async experimenter => new User
                {
                    Id = experimenter.UserId,
                    Name = (await _projectsDbContext.Users.FirstOrDefaultAsync(u => u.Id == experimenter.UserId)).DisplayName
                })
                .ToDictionaryAsync(_ => Guid.NewGuid());

            await base.LoadAsync(model, proposal, owner, supervisor);
        }

        public override Task StoreAsync(Experiment model, Proposal proposal)
        {
            proposal.StartDate = model.StartDate;
            proposal.EndDate = model.EndDate;

            proposal.Labs = (model.Labs?.Values ?? Enumerable.Empty<Models.Lab>())
                .Select(lab => new Data.Lab
                {
                    // FIXME: causes DELETE + INSERT instead of UPDATE
                    // Id = lab.Id.GetValueOrDefault(),
                    Modality = lab.Modality.Id,
                    SubjectCount = lab.SubjectCount,
                    ExtraSubjectCount = lab.ExtraSubjectCount,
                    SessionCount = lab.SessionCount,
                    SessionDuration = lab.SessionDurationMinutes.HasValue ? TimeSpan.FromMinutes(lab.SessionDurationMinutes.Value) : (TimeSpan?) null
                })
                .ToList();

            if (model.StorageQuota == Experiment.StorageQuotaType.Standard)
            {
                proposal.CustomQuota = false;
                proposal.CustomQuotaAmount = null;
                proposal.CustomQuotaMotivation = null;
            }
            else
            {
                proposal.CustomQuota = true;
                proposal.CustomQuotaAmount = model.CustomQuotaAmount;
                proposal.CustomQuotaMotivation = model.CustomQuotaMotivation;
            }

            proposal.Experimenters = (model.Experimenters?.Values ?? Enumerable.Empty<User>())
                .Select( experimenter => new Experimenter
                {
                    UserId = experimenter.Id
                })
                .ToList();

            return base.StoreAsync(model, proposal);
        }
    }
}