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
    public class DataManagementHandler : FormSectionHandlerBase<DataSectionModel>
    {
        private readonly ProjectsDbContext _projectsDbContext;

        public DataManagementHandler(IAuthorityProvider authorityProvider, ProjectsDbContext projectsDbContext)
            : base(authorityProvider, m => m.Data)
        {
            _projectsDbContext = projectsDbContext;
        }

        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => Enumerable.Empty<ApprovalAuthorityRole>();

        protected override async Task LoadAsync(DataSectionModel model, Proposal proposal, ProjectsUser owner, ProjectsUser supervisor)
        {
            model.StorageAccessRules = await proposal.DataAccessRules
                .Select(async access => new StorageAccessRuleModel
                {
                    User = new UserModel
                    {
                        Id = access.UserId,
                        Name = (await _projectsDbContext.Users.FirstOrDefaultAsync(u => u.Id == access.UserId)).DisplayName
                    },
                    Role = (StorageAccessRoleModel) access.Role,
                    CanRemove = access.UserId != owner.Id && access.UserId != supervisor.Id,
                    CanEdit = access.UserId != owner.Id
                })
                .ToDictionaryAsync(_ => Guid.NewGuid());

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

            await base.LoadAsync(model, proposal, owner, supervisor);
        }

        protected override Task StoreAsync(DataSectionModel model, Proposal proposal)
        {
            proposal.DataAccessRules = (model.StorageAccessRules?.Values ?? Enumerable.Empty<StorageAccessRuleModel>())
                .Select(access => new StorageAccessRule
                {
                    UserId = access.User.Id,
                    Role = (StorageAccessRole) access.Role
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