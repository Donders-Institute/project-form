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

        public async Task SendEmailAsync(ClaimsPrincipal user, IEmailModel email, MailAddress recipient, bool replyToUser)
        {
            MailAddress userAddress = null;
            if (user != null)
            {
                userAddress = new MailAddress(_userManager.GetEmailAddress(user), _userManager.GetUserName(user));
            }

            if (_overrideRecipient.Enabled)
            {
                if (_overrideRecipient.Fixed == null)
                {
                    if (user == null)
                    {
                        return;
                    }

                    recipient = userAddress;
                }
                else
                {
                    recipient = new MailAddress(_overrideRecipient.Fixed.Address, _overrideRecipient.Fixed.DisplayName);
                }
            }

            await SendEmailAsync(email, recipient, replyToUser && userAddress != null ? userAddress : null);
        }

        public async Task SendEmailNoOverrideAsync(IEmailModel email, MailAddress recipient)
        {
            await SendEmailAsync(email, recipient, null);
        }

        private async Task SendEmailAsync(IEmailModel email, MailAddress recipient, MailAddress replyTo)
        {
            var templatePath = Path.ChangeExtension(email.TemplateName, "hbs");

            var body = await _renderService.RenderAsync(templatePath, email);
            var message = new MailMessage(_sender, recipient)
            {
                Subject = email.Subject,
                Body = body,
                IsBodyHtml = email.IsHtml
            };

            if (replyTo != null)
            {
                message.ReplyToList.Add(replyTo);
            }

            await _client.SendMailAsync(message);
        }
    }

    public interface IEmailService
    {
        Task SendEmailAsync(ClaimsPrincipal user, IEmailModel email, MailAddress recipient, bool replyToUser = false);
        Task SendEmailNoOverrideAsync(IEmailModel email, MailAddress recipient);
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