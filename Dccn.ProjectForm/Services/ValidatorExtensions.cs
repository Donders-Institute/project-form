using Dccn.ProjectForm.Models;
using Dccn.ProjectForm.Services.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Dccn.ProjectForm.Services
{
    public static class ValidatorExtensions
    {
        public static IServiceCollection AddValidators(this IServiceCollection services)
        {
            services
                .AddTransient<IValidator<GeneralSectionModel>, GeneralSectionValidator>()
                .AddTransient<IValidator<FundingSectionModel>, FundingSectionValidator>()
                .AddTransient<IValidator<EthicsSectionModel>, EthicsSectionValidator>()
                .AddTransient<IValidator<ExperimentSectionModel>, ExperimentSectionValidator>();

            // TODO: Add more

            return services;
        }
    }
}