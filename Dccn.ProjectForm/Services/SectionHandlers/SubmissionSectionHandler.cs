﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Dccn.ProjectForm.Services.SectionHandlers
{
    public class SubmissionSectionHandler : FormSectionHandlerBase<SubmissionSectionModel>
    {
        private readonly IServiceProvider _serviceProvider;

        public SubmissionSectionHandler(IServiceProvider serviceProvider) : base(serviceProvider, m => m.Submission)
        {
            _serviceProvider = serviceProvider;
        }

        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => new [] {ApprovalAuthorityRole.Director};

        protected override IEnumerable<ApprovalAuthorityRole> RequiredApprovalRoles => new []
        {
            ApprovalAuthorityRole.Supervisor,
            // ApprovalAuthorityRole.Funding, TODO: is this right?
            ApprovalAuthorityRole.Ethics,
            ApprovalAuthorityRole.LabMri,
            ApprovalAuthorityRole.LabOther,
            ApprovalAuthorityRole.DataManager,
            ApprovalAuthorityRole.Privacy,
            ApprovalAuthorityRole.Payment
        };

        protected override async Task LoadAsync(SubmissionSectionModel model, Proposal proposal)
        {
            var sectionHandlers = _serviceProvider.GetServices<IFormSectionHandler>();

            model.NeedsApprovalBy = proposal.Approvals
                .Where(a => RequiredApprovalRoles.Contains(a.AuthorityRole))
                .Where(a => a.Status != ApprovalStatus.Approved && a.Status != ApprovalStatus.NotApplicable)
                .Select(a => a.AuthorityRole)
                .SelectMany(r => sectionHandlers.Where(h => h.HasApprovalAuthorityRole(r)).Select(h => new RequiredApprovalModel
                {
                    SectionId = h.Id,
                    SectionName = h.DisplayName
                }))
                .GroupBy(a => a.SectionId)
                .Select(g => g.First());

            await base.LoadAsync(model, proposal);
        }
    }
}