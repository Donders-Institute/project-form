using System.IO;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
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
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly MailAddress _sender;
        private readonly EmailOptions.OverrideRecipientOptions _overrideRecipient;
        private readonly BufferBlock<MailMessage> _queue;

        public EmailService(ITemplateRenderService renderService, IOptions<EmailOptions> optionsAccessor, IServiceScopeFactory scopeFactory)
        {
            _renderService = renderService;
            _scopeFactory = scopeFactory;

            var options = optionsAccessor.Value;
            _sender = new MailAddress(options.Sender.Address, options.Sender.DisplayName);
            _overrideRecipient = options.OverrideRecipient ?? new EmailOptions.OverrideRecipientOptions { Enabled = false };
            _queue = new BufferBlock<MailMessage>();
        }

        public async Task QueueMessageAsync(ClaimsPrincipal user, IEmailModel email, MailAddress recipient, bool replyToUser)
        {
            MailAddress userAddress = null;
            if (user != null)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var userManager = scope.ServiceProvider.GetRequiredService<IUserManager>();
                    userAddress = new MailAddress(userManager.GetEmailAddress(user), userManager.GetUserName(user));
                }
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

        public async Task QueueMessageNoOverrideAsync(IEmailModel email, MailAddress recipient)
        {
            await SendEmailAsync(email, recipient, null);
        }

        public Task<MailMessage> PollMessageAsync(CancellationToken cancellationToken)
        {
            return _queue.ReceiveAsync(cancellationToken);
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

            await _queue.SendAsync(message);
        }
    }

    public interface IEmailService
    {
        Task QueueMessageAsync(ClaimsPrincipal user, IEmailModel email, MailAddress recipient, bool replyToUser = false);
        Task QueueMessageNoOverrideAsync(IEmailModel email, MailAddress recipient);
        Task<MailMessage> PollMessageAsync(CancellationToken cancellationToken = default);
    }

    public static class EmailServiceExtensions
    {
        [PublicAPI]
        public static IServiceCollection AddEmail(this IServiceCollection services, string templatePath)
        {
            return services
                .AddHostedService<EmailBackgroundService>()
                .AddSingleton<IEmailService, EmailService>()
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