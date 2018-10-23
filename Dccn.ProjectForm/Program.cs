using System;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Extensions;
using JetBrains.Annotations;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dccn.ProjectForm
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();
            var logger = host.Services.GetService<ILogger<Startup>>();

            try
            {
                using (var scope = host.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ProposalsDbContext>();
                    if (context.Database.EnsureCreated())
                    {
                        logger.LogInformation("Initialized the database.");
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogCritical(e, "There was an error initializing the database.");
                throw;
            }

            host.Run();
        }

        [UsedImplicitly]
        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(builder => builder.AddDockerSecrets())
                .UseStartup<Startup>();
        }
    }
}