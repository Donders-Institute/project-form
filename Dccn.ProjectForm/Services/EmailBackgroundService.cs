using System;
using System.Linq.Expressions;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Dccn.ProjectForm.Configuration;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dccn.ProjectForm.Services
{
    [UsedImplicitly]
    public class EmailBackgroundService : BackgroundService
    {
        private const int MaxRetries = 5;

        private readonly EmailOptions _options;
        private readonly IEmailService _emailService;
        private readonly ILogger _logger;

        public EmailBackgroundService(IOptions<EmailOptions> options, IEmailService emailService, ILogger<EmailBackgroundService> logger)
        {
            _emailService = emailService;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var message = await _emailService.PollMessageAsync(stoppingToken);

                for (var attempt = 0; attempt <= MaxRetries; attempt++)
                {
                    try
                    {
                        using (var client = new SmtpClient(_options.Host, _options.Port))
                        {
                            if (_options.UserName != null && _options.Password != null)
                            {
                                client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
                            }

                            await client.SendMailAsync(message);
                            _logger.LogInformation($"E-mail sent to {message.To}.");
                            break;
                        }
                    }
                    catch (SmtpException e)
                    {
                        if (attempt == MaxRetries)
                        {
                            _logger.LogError(e, "Error sending e-mail. Maximum amount of retries reached.");
                        }
                        else
                        {
                            var delaySeconds = 5 * (1 << attempt);
                            _logger.LogWarning(e, $"Error sending e-mail. Retrying after a {delaySeconds} seconds.");
                            await Task.Delay(delaySeconds * 1000, stoppingToken);
                        }
                    }
                }
            }
        }
    }
}