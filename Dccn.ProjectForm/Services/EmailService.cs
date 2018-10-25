using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Configuration;
using Dccn.ProjectForm.Email.Models;
using HandlebarsDotNet;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dccn.ProjectForm.Services
{
    public class EmailService : IEmailService
    {
        private readonly ITemplateRenderService _renderService;
        private readonly IHostingEnvironment _environment;
        private readonly IUserManager _userManager;
        private readonly SmtpClient _client;
        private readonly MailAddress _sender;
        private readonly bool _overrideRecipient;

        public EmailService(ITemplateRenderService renderService, IHostingEnvironment environment, IUserManager userManager, IOptionsSnapshot<EmailOptions> optionsAccessor)
        {
            _renderService = renderService;
            _environment = environment;
            _userManager = userManager;

            var options = optionsAccessor.Value;

            _client = new SmtpClient(options.Host, options.Port);
            if (options.UserName != null)
            {
                _client.Credentials = new NetworkCredential(options.UserName, options.Password);
            }

            _sender = new MailAddress(options.Sender.Address, options.Sender.DisplayName);
            _overrideRecipient = options.OverrideRecipient;
        }

        public async Task SendEmailAsync(IEmailModel email, ClaimsPrincipal user)
        {
            MailAddress recipientOverride = null;
            if (_overrideRecipient)
            {
                if (user == null)
                {
                    return;
                }

                var userName = _userManager.GetUserName(user);
                var userEmail = _userManager.GetUserEmail(user);
                recipientOverride = new MailAddress(userName, userEmail);
            }

            var templatePath = Path.ChangeExtension(email.TemplateName, "hbs");
            var model = new
            {
                Environment = new
                {
                    Name = _environment.EnvironmentName,

                    Development = _environment.IsDevelopment(),
                    Staging = _environment.IsStaging(),
                    Production = _environment.IsProduction()
                },
                Email = email
            };

            var body = await _renderService.RenderAsync(templatePath, model);
            var message = new MailMessage(_sender, recipientOverride ?? email.Recipient)
            {
                Subject = email.Subject,
                Body = body,
                IsBodyHtml = true
            };

            await _client.SendMailAsync(message);
        }
    }

    public interface IEmailService
    {
        Task SendEmailAsync(IEmailModel email, ClaimsPrincipal user);
    }

    public static class EmailServiceExtensions
    {
        [PublicAPI]
        public static IServiceCollection AddEmail(this IServiceCollection services, string templatePath)
        {
            return services
                .AddTransient<IEmailService, EmailService>()
                .AddSingleton<ITemplateRenderService>(provider =>
                {
                    var environment = provider.GetService<IHostingEnvironment>();
                    var handlebars = Handlebars.Create(new HandlebarsConfiguration
                    {
                        FileSystem = new TemplateFileSystem(environment, templatePath),
                        Helpers =
                        {
                            {
                                "includeStyle", (output, context, arguments) =>
                                {
                                    var path = (string) arguments[0];
                                    output.WriteLine(@"<style type=""text/css"">");
                                    output.WriteLine(File.ReadAllText(environment.WebRootFileProvider.GetFileInfo(path)                                        .PhysicalPath));
                                    output.WriteLine("</style>");
                                }
                            }
                        }
                    });
                    return new TemplateRenderService(handlebars);
                });
        }
    }
}