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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dccn.ProjectForm.Services
{
    public class EmailService : IEmailService
    {
        private readonly ITemplateRenderService _renderService;
        private readonly IUserManager _userManager;
        private readonly SmtpClient _client;
        private readonly MailAddress _sender;
        private readonly EmailOptions.OverrideRecipientOptions _overrideRecipient;

        public EmailService(ITemplateRenderService renderService, IUserManager userManager, IOptionsSnapshot<EmailOptions> optionsAccessor)
        {
            _renderService = renderService;
            _userManager = userManager;

            var options = optionsAccessor.Value;

            _client = new SmtpClient(options.Host, options.Port);
            if (options.UserName != null)
            {
                _client.Credentials = new NetworkCredential(options.UserName, options.Password);
            }

            _sender = new MailAddress(options.Sender.Address, options.Sender.DisplayName);
            _overrideRecipient = options.OverrideRecipient ?? new EmailOptions.OverrideRecipientOptions { Enabled = false };
        }

        public async Task SendEmailAsync(ClaimsPrincipal user, IEmailModel email, params MailAddress[] recipients)
        {
            if (_overrideRecipient.Enabled)
            {
                string displayName, emailAddress;

                if (_overrideRecipient.Fixed == null)
                {
                    if (user == null)
                    {
                        return;
                    }

                    displayName = _userManager.GetUserName(user);
                    emailAddress = _userManager.GetEmailAddress(user);
                }
                else
                {
                    displayName = _overrideRecipient.Fixed.DisplayName;
                    emailAddress = _overrideRecipient.Fixed.Address;
                }

                recipients = new [] {new MailAddress(emailAddress, displayName)};
            }

            await SendEmailNoOverrideAsync(email, recipients);
        }

        public async Task SendEmailNoOverrideAsync(IEmailModel email, params MailAddress[] recipients)
        {
            var templatePath = Path.ChangeExtension(email.TemplateName, "hbs");

            var body = await _renderService.RenderAsync(templatePath, email);
            var message = new MailMessage
            {
                From = _sender,
                Subject = email.Subject,
                Body = body,
                IsBodyHtml = email.IsHtml
            };

            foreach (var recipient in recipients)
            {
                message.To.Add(recipient);
            }

            await _client.SendMailAsync(message);
        }
    }

    public interface IEmailService
    {
        Task SendEmailAsync(ClaimsPrincipal user, IEmailModel email, params MailAddress[] recipients);
        Task SendEmailNoOverrideAsync(IEmailModel email, params MailAddress[] recipients);
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
                    var environment = provider.GetRequiredService<IHostingEnvironment>();
                    var handlebars = Handlebars.Create(new HandlebarsConfiguration
                    {
                        FileSystem = new TemplateFileSystem(environment, templatePath)
                    });
                    return new TemplateRenderService(handlebars, provider.GetRequiredService<IMemoryCache>());
                });
        }
    }
}