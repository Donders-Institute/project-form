using System;
using System.Net.Mail;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Authorization;
using Dccn.ProjectForm.Configuration;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.Projects;
using Dccn.ProjectForm.Services;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Dccn.ProjectForm
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ProjectsDbContext>(options =>
            {
                options.UseMySql(_configuration.GetConnectionString("ProjectsDatabase"));
                options.ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.QueryClientEvaluationWarning));
            });

            services.AddDbContext<ProposalsDbContext>(options =>
            {
                options.UseSqlServer(_configuration.GetConnectionString("ProposalsDatabase"));
                options.ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.QueryClientEvaluationWarning));
            });

            services
                .AddAuthentication(SignInManager.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromDays(1);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                });

            services.AddLocalization(options =>
            {
                options.ResourcesPath = "Resources";
            });

            services
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddDataAnnotationsLocalization()
                .AddMvcOptions(options =>
                {
                    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
                    options.Filters.Add(new AuthorizeFilter(policy));
                })
                .AddFluentValidation(options =>
                {
                    options.LocalizationEnabled = true;
                    // options.RunDefaultMvcValidationAfterFluentValidationExecutes = false;
                });

            services
                .Configure<LdapOptions>(_configuration.GetSection(LdapOptions.SectionName))
                .Configure<EmailOptions>(_configuration.GetSection(EmailOptions.SectionName))
                .Configure<FormOptions>(_configuration.GetSection(FormOptions.SectionName))
                .Configure<RepositoryApiOptions>(_configuration.GetSection(RepositoryApiOptions.SectionName));

            services
                .AddTransient<IUserManager, UserManager>()
                .AddTransient<ISignInManager, SignInManager>()
                .AddScoped<IAuthorizationHandler, FormAuthorizationHandler>()
                .AddScoped<IAuthorizationHandler, FormSectionAuthorizationHandler>()
                .AddTransient<IModalityProvider, ModalityProvider>()
                .AddTransient<IAuthorityProvider, AuthorityProvider>()
                .AddSingleton<IStringLocalizerFactory, TomlStringLocalizerFactory>(s => new TomlStringLocalizerFactory(s, "Messages.toml"))
                .AddTransient(typeof(IStringLocalizer<>), typeof(TomlStringLocalizer<>))
                .AddTransient<IStringLocalizer, TomlStringLocalizer>()
                .AddRepositoryApiClient()
                .AddFormSectionHandlers()
                .AddFormSectionValidators()
                .AddEmail("/Email/Templates");
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                var reportAddress = _configuration.GetSection("ExceptionReporter")?.Get<EmailAddress>();
                if (reportAddress != null)
                {
                    app.UseExceptionReporter(new MailAddress(reportAddress.Address, reportAddress.DisplayName));
                }
            }

            app.UseStatusCodePages();

            app.UseRequestLocalization(options =>
            {
                options.AddSupportedCultures("nl-NL", "en-US");
                options.AddSupportedUICultures("en-US");
            });

            app.UseStaticFiles();
            app.UseAuthentication();

            app.UseMvc();
        }
    }
}