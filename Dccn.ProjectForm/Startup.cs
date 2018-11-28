using System;
using System.Net.Http.Headers;
using System.Text;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Authorization;
using Dccn.ProjectForm.Configuration;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.Projects;
using Dccn.ProjectForm.Services;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            });

            services.AddDbContext<ProposalsDbContext>(options =>
            {
                options.UseSqlServer(_configuration.GetConnectionString("ProposalsDatabase"));
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

            services.AddLocalization(options => options.ResourcesPath = "Resources");

            services
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
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

            services.AddHttpClient<IRepositoryApiClient, RepositoryApiClient>(client =>
            {
                var options = _configuration.GetSection(RepositoryApiOptions.SectionName).Get<RepositoryApiOptions>();
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{options.UserName}:{options.Password}"));

                client.BaseAddress = new Uri(options.BaseUri);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            });

            services
                .Configure<LdapOptions>(_configuration.GetSection(LdapOptions.SectionName))
                .Configure<EmailOptions>(_configuration.GetSection(EmailOptions.SectionName))
                .Configure<FormOptions>(_configuration.GetSection(FormOptions.SectionName));

            services
                .AddTransient<IUserManager, UserManager>()
                .AddTransient<ISignInManager, SignInManager>()
                .AddScoped<IAuthorizationHandler, FormAuthorizationHandler>()
                .AddScoped<IAuthorizationHandler, FormSectionAuthorizationHandler>()
                .AddTransient<IModalityProvider, ModalityProvider>()
                .AddTransient<IAuthorityProvider, AuthorityProvider>()
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
                app.UseExceptionReporter(_configuration.GetSection("ExceptionReporter").Get<EmailAddress>());
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