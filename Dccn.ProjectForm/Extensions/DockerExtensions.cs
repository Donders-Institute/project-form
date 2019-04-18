using System;
using System.IO;
using JetBrains.Annotations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dccn.ProjectForm.Extensions
{
    public static class DockerExtensions
    {
        [PublicAPI]
        public static IConfigurationBuilder AddDockerSecrets(this IConfigurationBuilder builder, string fileName = "secrets.json")
        {
            if (IsRunningInContainer())
            {
                builder.AddJsonFile(Path.Combine("/run/secrets", fileName), true);
            }

            return builder;
        }

        [PublicAPI]
        public static IServiceCollection AddDockerDataProtection(this IServiceCollection services, string path)
        {
            if (IsRunningInContainer())
            {
                services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(path));
            }

            return services;
        }

        private static bool IsRunningInContainer()
        {
            var value = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
            return value != null && (value == "1" || value == "true");
        }
    }
}