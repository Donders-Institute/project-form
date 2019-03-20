using System;
using System.Globalization;
using System.Net.Mail;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Authorization;
using Dccn.ProjectForm.Configuration;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.ProjectDb;
using Dccn.ProjectForm.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
            services.AddDbContext<ProjectDbContext>(options =>
            {
                options.UseMySql(_configuration.GetConnectionString("ProjectDb"));
                options.ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.QueryClientEvaluationWarning));
            });

            services.AddDbContext<ProposalDbContext>(options =>
            {
                options.UseSqlServer(_configuration.GetConnectionString("ProposalDb"));
                options.ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.QueryClientEvaluationWarning));
            });

            services.AddMemoryCache();

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
//                .AddViewOptions(options =>
//                {
//                    options.HtmlHelperOptions.ClientValidationEnabled = false;
//                    options.ClientModelValidatorProviders.Clear();
//                });
                .AddFluentValidation(options =>
                {
                    options.RunDefaultMvcValidationAfterFluentValidationExecutes = true;
                    options.LocalizationEnabled = true;
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
                .AddScoped<IAuthorizationHandler, ApprovalAuthorizationHandler>()
                .AddSingleton<ILabProvider, LabProvider>()
                .AddTransient<IProjectDbExporter, ProjectDbExporter>()
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
                options.SupportedCultures = new [] { new CultureInfo("en") };
                options.SupportedUICultures = new [] { new CultureInfo("en") };
                options.DefaultRequestCulture = new RequestCulture("en");
            });

            app.UseStaticFiles();
            app.UseAuthentication();

            app.UseMvc();
        }
    }
}