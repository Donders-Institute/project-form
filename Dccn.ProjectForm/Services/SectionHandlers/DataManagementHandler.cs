﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Extensions;
using Dccn.ProjectForm.Models;

namespace Dccn.ProjectForm.Services.SectionHandlers
{
    public class DataManagementHandler : FormSectionHandlerBase<DataSectionModel>
    {
        private readonly IUserManager _userManager;

        public DataManagementHandler(IServiceProvider serviceProvider, IUserManager userManager)
            : base(serviceProvider, m => m.Data)
        {
            _userManager = userManager;
        }

        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => Enumerable.Empty<ApprovalAuthorityRole>();

        protected override async Task LoadAsync(DataSectionModel model, Proposal proposal)
        {
            var owner = await _userManager.GetUserByIdAsync(proposal.OwnerId);
            var supervisor = await _userManager.GetUserByIdAsync(proposal.OwnerId);

            model.StorageAccessRules = await proposal.DataAccessRules
                .Select(async rule => new StorageAccessRuleModel
                {
                    Id = rule.UserId,
                    Name = (await _userManager.GetUserByIdAsync(rule.UserId)).DisplayName,
                    Role = (StorageAccessRoleModel) rule.Role,
                    CanRemove = rule.UserId != owner.Id && rule.UserId != supervisor.Id,
                    CanEdit = rule.UserId != owner.Id
                })
                .ToDictionaryAsync(rule => rule.Id);

            if (proposal.ExternalPreservation)
            {
                model.Preservation = DataPreservationModel.External;
                model.ExternalPreservation = new ExternalPreservationModel
                {
                    Location = proposal.ExternalPreservationLocation,
                    SupervisorName = proposal.ExternalPreservationSupervisor,
                    Reference = proposal.ExternalPreservationReference
                };
            }
            else
            {
                model.Preservation = DataPreservationModel.Repository;
            }

            model.OwnerId = owner.Id;
            model.OwnerName = owner.DisplayName;
            model.OwnerEmail = owner.Email;
            model.SupervisorName = supervisor.DisplayName;

            await base.LoadAsync(model, proposal);
        }

        protected override Task StoreAsync(DataSectionModel model, Proposal proposal)
        {
            proposal.DataAccessRules = (model.StorageAccessRules?.Values ?? Enumerable.Empty<StorageAccessRuleModel>())
                .Select(rule => new StorageAccessRule
                {
                    UserId = rule.Id,
                    Role = (StorageAccessRole) rule.Role
                })
                .ToList();

            if (model.Preservation == DataPreservationModel.External)
            {
                proposal.ExternalPreservation = true;
                proposal.ExternalPreservationLocation = model.ExternalPreservation.Location;
                proposal.ExternalPreservationSupervisor = model.ExternalPreservation.SupervisorName;
                proposal.ExternalPreservationReference = model.ExternalPreservation.Reference;
            }
            else
            {
                proposal.ExternalPreservation = false;
                proposal.ExternalPreservationLocation = null;
                proposal.ExternalPreservationSupervisor = null;
                proposal.ExternalPreservationReference = null;
            }

            return base.StoreAsync(model, proposal);
        }
    }
}