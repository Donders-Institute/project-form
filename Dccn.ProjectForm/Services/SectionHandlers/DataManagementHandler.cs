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
    public class DataManagementHandler : FormSectionHandlerBase<DataManagement>
    {
        private readonly ProjectsDbContext _projectsDbContext;

        public DataManagementHandler(IAuthorityProvider authorityProvider, ProjectsDbContext projectsDbContext) : base(authorityProvider)
        {
            _projectsDbContext = projectsDbContext;
        }

        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => Enumerable.Empty<ApprovalAuthorityRole>();

        public override async Task LoadAsync(DataManagement model, Proposal proposal, ProjectsUser owner, ProjectsUser supervisor)
        {
            model.StorageAccessRules = await proposal.DataAccessRules
                .Select(async access => new DataManagement.StorageAccessRule
                {
                    User = new User
                    {
                        Id = access.UserId,
                        Name = (await _projectsDbContext.Users.FirstOrDefaultAsync(u => u.Id == access.UserId)).DisplayName
                    },
                    Role = access.Role,
                    CanRemove = access.UserId != owner.Id && access.UserId != supervisor.Id,
                    CanEdit = access.UserId != owner.Id
                })
                .ToDictionaryAsync(_ => Guid.NewGuid());

            if (proposal.ExternalPreservation)
            {
                model.Preservation = DataManagement.PreservationType.External;
                model.ExternalPreservation = new DataManagement.ExternalPreservationType
                {
                    Location = proposal.ExternalPreservationLocation,
                    SupervisorName = proposal.ExternalPreservationSupervisor,
                    Reference = proposal.ExternalPreservationReference
                };
            }
            else
            {
                model.Preservation = DataManagement.PreservationType.Repository;
            }

            model.OwnerId = owner.Id;
            model.OwnerName = owner.DisplayName;
            model.OwnerEmail = owner.Email;
            model.SupervisorName = supervisor.DisplayName;

            await base.LoadAsync(model, proposal, owner, supervisor);
        }

        public override Task StoreAsync(DataManagement model, Proposal proposal)
        {
            proposal.DataAccessRules = (model.StorageAccessRules?.Values ?? Enumerable.Empty<DataManagement.StorageAccessRule>())
                .Select(access => new StorageAccessRule
                {
                    UserId = access.User.Id,
                    Role = access.Role
                })
                .ToList();

            if (model.Preservation == DataManagement.PreservationType.External)
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