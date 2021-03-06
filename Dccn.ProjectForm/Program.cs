﻿using System;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Extensions;
using Dccn.ProjectForm.Services;
using FluentValidation;
using JetBrains.Annotations;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dccn.ProjectForm
{
    [UsedImplicitly]
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            await Task.WhenAll(
                InitProposalDbContextAsync(host.Services, logger),
                InitLabsProviderAsync(host.Services, logger));

            ValidatorOptions.DisplayNameResolver = (type, member, expression) =>
            {
                var metadataProvider = host.Services.GetRequiredService<IModelMetadataProvider>();
                return member == null
                    ? metadataProvider.GetMetadataForType(type).DisplayName
                    : metadataProvider.GetMetadataForProperty(member.DeclaringType, member.Name).DisplayName;
            };

            await host.RunAsync();
        }

        [PublicAPI]
        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(builder => builder.AddDockerSecrets())
                .UseStartup<Startup>();
        }

        private static async Task InitProposalDbContextAsync(IServiceProvider services, ILogger logger)
        {
            try
            {
                using (var scope = services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ProposalDbContext>();
                    await context.Database.MigrateAsync();
                }
            }
            catch (Exception e)
            {
                logger.LogCritical(e, "There was an error migrating the proposals database.");
                throw;
            }
        }

        private static async Task InitLabsProviderAsync(IServiceProvider services, ILogger logger)
        {
            try
            {
                using (var scope = services.CreateScope())
                {
                    var labsProvider = scope.ServiceProvider.GetRequiredService<ILabProvider>();
                    await labsProvider.InitializeAsync(scope.ServiceProvider);
                    logger.LogInformation("Initialized the lab information.");
                }
            }
            catch (Exception e)
            {
                logger.LogCritical(e, "There was an error initializing the lab information.");
                throw;
            }
        }
    }
}