using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.ProjectDb;
using JetBrains.Annotations;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dccn.ProjectForm.Services
{
    [UsedImplicitly]
    public class ProposalDbChangeListener : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IHubContext<FormHub, IFormHubClient> _hubContext;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private string _connectionString;
        private IDictionary<int, Proposal> _proposalCache;

        public ProposalDbChangeListener(IServiceScopeFactory serviceScopeFactory, IHubContext<FormHub, IFormHubClient> hubContext, ILogger<ProposalDbChangeListener> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ProposalDbContext>();
                _connectionString = context.Database.GetDbConnection().ConnectionString;
            }
            SqlDependency.Start(_connectionString);
            _proposalCache = new Dictionary<int, Proposal>();
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                return base.StopAsync(cancellationToken);
            }
            finally
            {
                _proposalCache = null;
                SqlDependency.Stop(_connectionString);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync(cancellationToken);
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            var changedSignal = new TaskCompletionSource<object>();
                            using (var command = new SqlCommand("SELECT [Id], [Timestamp] FROM [dbo].[Proposals]", connection))
                            {
                                var depency = new SqlDependency(command);
                                depency.OnChange += (sender, args) => changedSignal.SetResult(null);

                                using (cancellationToken.Register(() => changedSignal.SetCanceled()))
                                {
                                    await Task.WhenAll(
                                        ProcessUpdatesAsync(command, cancellationToken),
                                        changedSignal.Task,
                                        Task.Delay(1000, cancellationToken)
                                    );
                                }
                            }
                        }
                    }
                }
                catch (SqlException e)
                {
                    _logger.LogError(e, "An exception occurred while listening for database changes. Retrying in 5 seconds.");
                    await Task.Delay(5000, cancellationToken);
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
        }

        private async Task ProcessUpdatesAsync(SqlCommand command, CancellationToken cancellationToken)
        {
            IDictionary<int, (Change Type, Proposal Cached)> changes = _proposalCache
                .ToDictionary(entry => entry.Key, entry => (Change.Removed, entry.Value));

            using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var id = reader.GetInt32(0);
                    var timestamp = reader.GetSqlBinary(1).Value;

                    var cached = _proposalCache.TryGetValue(id, out var cachedProposal);
                    if (cached)
                    {
                        if (timestamp.SequenceEqual(cachedProposal.Timestamp))
                        {
                            changes.Remove(id);
                        }
                        else
                        {
                            changes[id] = (Change.Updated, cachedProposal);
                        }
                    }
                    else
                    {
                        changes[id] = (Change.Added, null);
                    }
                }
            }

            var notRemovedIds = changes
                .Where(entry => entry.Value.Type != Change.Removed)
                .Select(entry => entry.Key);

            using (var scope = _serviceScopeFactory.CreateScope())
            using (var dbContext = scope.ServiceProvider.GetRequiredService<ProposalDbContext>())
            {
                var proposals = await dbContext.Proposals
                    .AsNoTracking()
                    .Include(p => p.Labs)
                    .Include(p => p.Experimenters)
                    .Include(p => p.StorageAccessRules)
                    .Include(p => p.Approvals)
                    .Include(p => p.Comments)
                    .Where(p => notRemovedIds.Contains(p.Id))
                    .ToListAsync(cancellationToken);

                var resolvedChanges = changes
                    .GroupJoin(proposals, change => change.Key, proposal => proposal.Id, (change, matchingProposals) =>
                    {
                        var (id, (type, cached)) = change;
                        var proposal = matchingProposals.SingleOrDefault();
                        return new
                        {
                            Id = id,
                            CachedProposal = cached,
                            Type = proposal == null ? Change.Removed : type,
                            Proposal = proposal
                        };
                    })
                    .GroupBy(change => change.Type)
                    .ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());

                if (resolvedChanges.TryGetValue(Change.Added, out var adds))
                {
                    _logger.LogInformation($"Proposals added: [{string.Join(", ", adds.Select(c => c.Id))}].");
                    foreach (var add in adds)
                    {
                        _proposalCache[add.Id] = add.Proposal;
                    }
                }

                if (resolvedChanges.TryGetValue(Change.Removed, out var removes))
                {
                    _logger.LogInformation($"Proposals removed: [{string.Join(", ", removes.Select(c => c.Id))}].");
                    foreach (var remove in removes)
                    {
                        _proposalCache.Remove(remove.Id);
                    }
                }

                if (resolvedChanges.TryGetValue(Change.Updated, out var updates))
                {
                    var sectionHandlers = scope.ServiceProvider.GetServices<IFormSectionHandler>().ToList();
                    var userManager = scope.ServiceProvider.GetRequiredService<IUserManager>();

                    var userNames = await userManager.GetUserNamesForIdsAsync(updates.Select(update => update.Proposal.LastEditedBy), cancellationToken);

                    foreach (var update in updates)
                    {
                        var changedSections = sectionHandlers
                            .Where(sectionHandler => !sectionHandler.SectionEquals(update.CachedProposal, update.Proposal))
                            .Select(sectionHandler => sectionHandler.Id)
                            .ToList();

                        _ = _hubContext.Clients
                            .Group(update.Id.ToString())
                            .UpdateForm(update.Id, update.Proposal.Timestamp, userNames[update.Proposal.LastEditedBy], update.Proposal.LastEditedOn, changedSections);

                        _logger.LogInformation($"Proposal updated: {update.Id}. Changed sections: [{string.Join(", ", changedSections)}]");
                        _proposalCache[update.Id] = update.Proposal;
                    }
                }
            }
        }

        private enum Change
        {
            Added,
            Removed,
            Updated
        }
    }
}