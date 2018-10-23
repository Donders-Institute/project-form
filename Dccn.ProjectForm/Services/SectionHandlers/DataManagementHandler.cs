using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.Projects;
using Dccn.ProjectForm.Extensions;
using Dccn.ProjectForm.Models;

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

        public override Task LoadAsync(DataManagement model, Proposal proposal, ProjectsUser owner, ProjectsUser supervisor)
        {
            model.StorageAccessRules = proposal.DataAccessRules
                .Select(access => new DataManagement.StorageAccessRule
                {
                    User = new User
                    {
                        Id = access.User.Id,
                        Name = access.User.DisplayName
                    },
                    Role = access.Role,
                    CanRemove = access.User.Id != owner.Id && access.User.Id != supervisor.Id,
                    CanEdit = access.User.Id != owner.Id
                })
                .ToDictionary(_ => Guid.NewGuid());

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

            return base.LoadAsync(model, proposal, owner, supervisor);
        }

        public override async Task StoreAsync(DataManagement model, Proposal proposal)
        {
            proposal.DataAccessRules = await (model.StorageAccessRules?.Values ?? Enumerable.Empty<DataManagement.StorageAccessRule>())
                .Select(async access => new StorageAccessRule
                {
                    User = access.User.Id == null
                        ? new UserReference { DisplayName = access.User.Name }
                        : await UserReference.FromExistingAsync(access.User.Id, _projectsDbContext),
                    Role = access.Role
                })
                .ToListAsync();

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

            await base.StoreAsync(model, proposal);
        }
    }
}