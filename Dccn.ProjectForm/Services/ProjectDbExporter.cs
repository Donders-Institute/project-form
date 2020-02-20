using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.ProjectDb;
using Microsoft.EntityFrameworkCore;

namespace Dccn.ProjectForm.Services
{
    public class ProjectDbExporter : IProjectDbExporter
    {
        private readonly ProjectDbContext _projectDbContext;
        private readonly ILabProvider _labProvider;

        public ProjectDbExporter(ProjectDbContext projectDbContext, ILabProvider labProvider)
        {
            _projectDbContext = projectDbContext;
            _labProvider = labProvider;
        }

        [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
        public async Task<bool> Export(Proposal proposal, string sourceId)
        {
            if (sourceId.Length != 7)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceId));
            }

            if (!await _projectDbContext.FundingSources.AnyAsync(s => s.Id == sourceId))
            {
                return false;
            }

            var suffixes = await _projectDbContext.Projects
                .Where(p => p.SourceId == sourceId)
                .Where(p => EF.Functions.Like(p.Id, $"{sourceId}.__"))
                .Select(p => p.Id.Substring(sourceId.Length + 1, 2))
                .ToListAsync();

            var maxSuffix = suffixes.Select(s => int.TryParse(s, out var i) ? i : 0).Append(0).Max();

            var projectId = $"{sourceId}.{maxSuffix + 1:D2}";

            var now = DateTime.Now;

            var startDate = proposal.StartDate.Value;
            var endDate = proposal.EndDate.Value;
            var finalEndDate = endDate.AddMonths(24);

            var members = proposal.StorageAccessRules
                .Select(rule => new ProjectDbProjectMember {
                    UserId = rule.UserId,
                    Created = now,
                    Updated = now,
                    Role = GetRoleId(rule.Role),
                    Action = "set"
                })
                .ToList();

            var experiments = proposal.Labs
                .Select(lab =>
                {
                    var subjectCount = lab.SubjectCount.Value;
                    var sessionCount = lab.SessionCount.Value;
                    var modality = _labProvider.Labs[lab.Modality];

                    return new ProjectDbExperiment
                    {
                        Created = now,
                        Updated = now,
                        ExperimenterId = proposal.OwnerId,
                        StartingDate = startDate,
                        EndingDate = endDate,
                        NoSubjects = subjectCount,
                        NoSessions = sessionCount,
                        ImagingMethodId = _labProvider.GetImagingMethodId(lab.Modality),
                        LabTime = (int) lab.SessionDuration.Value.TotalMinutes,
                        // PpmApprovedQuota = proposal.CustomQuotaAmount,
                        CalculatedQuota = modality.SessionStorageQuota is int sessionStorage ? subjectCount * sessionCount * sessionStorage / 1000 : 0,
                        Offset = 0,
                        ExperimentEndDate = finalEndDate
                    };
                })
                .ToList();

            if (!experiments.Any())
            {
                experiments.Add(new ProjectDbExperiment
                {
                    Created = now,
                    Updated = now,
                    ExperimenterId = proposal.OwnerId,
                    StartingDate = startDate,
                    EndingDate = endDate,
                    ImagingMethodId = "none",
                    LabTime = 0,
                    Offset = 0,
                    ExperimentEndDate = finalEndDate
                });
            }

            var calculatedQuota = experiments.Sum(experiment => experiment.CalculatedQuota);
            var customQuota = proposal.CustomQuotaAmount.GetValueOrDefault(_labProvider.MinimumStorageQuota / 1000);
            int totalQuota;
            if (customQuota > calculatedQuota)
            {
                if (experiments.Count == 1)
                {
                    experiments.Single().PpmApprovedQuota = customQuota;
                }
                else
                {
                    experiments.Add(new ProjectDbExperiment
                    {
                        Created = now,
                        Updated = now,
                        ExperimenterId = proposal.OwnerId,
                        StartingDate = startDate,
                        EndingDate = endDate,
                        ImagingMethodId = "none",
                        LabTime = 0,
                        Offset = 0,
                        PpmApprovedQuota = customQuota - calculatedQuota,
                        ExperimentEndDate = finalEndDate
                    });
                }

                totalQuota = customQuota;
            }
            else
            {
                totalQuota = calculatedQuota;
            }

            var experimenters = proposal.Experimenters
                .Where(experimenter => experimenter.UserId != proposal.OwnerId)
                .Select(experimenter => new ProjectDbExperiment
                {
                    Created = now,
                    Updated = now,
                    ExperimenterId = experimenter.UserId,
                    StartingDate = startDate,
                    EndingDate = endDate,
                    ImagingMethodId = "none",
                    LabTime = 0,
                    Offset = 0,
                    ExperimentEndDate = finalEndDate
                })
                .ToList();

            experiments.AddRange(experimenters);

            foreach (var (experiment, index) in experiments.Select((experiment, index) => (experiment, index)))
            {
                experiment.Suffix = $"{index + 1}";
            }

            var project = new ProjectDbProject
            {
                Id = projectId,
                Created = now,
                Updated = now,
                ProjectName = proposal.Title,
                OwnerId = proposal.OwnerId,
                SourceId = sourceId,
                StartingDate = startDate,
                EndingDate = endDate,
                // PpmApprovedQuota = proposal.CustomQuotaAmount.GetValueOrDefault(),
                EcApproved = proposal.EcApproved,
                EcRequestedBy = proposal.EcApproved ? proposal.EcCode : proposal.EcReference,
                AuthorshipSequence = string.Empty,
                AuthorshipRemarks = string.Empty,
                ProjectBased = true,
                CalculatedProjectSpace = totalQuota,
                ProjectEndDate = finalEndDate,

                Experiments = experiments,
                ProjectMembers = members
            };

            _projectDbContext.Projects.Add(project);

            await _projectDbContext.SaveChangesAsync();
            proposal.ProjectId = projectId;

            return true;
        }

        private string GetRoleId(StorageAccessRole role)
        {
            switch (role)
            {
                case StorageAccessRole.Manager:
                    return "manager";
                case StorageAccessRole.Contributor:
                    return "contributor";
                case StorageAccessRole.Viewer:
                    return "viewer";
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }
    }

    public interface IProjectDbExporter
    {
        Task<bool> Export(Proposal proposal, string sourceId);
    }
}
