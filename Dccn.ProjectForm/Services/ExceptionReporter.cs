using System;
using System.Diagnostics;
using System.Net.Mail;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Email.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace Dccn.ProjectForm.Services
{
    public class ExceptionReporter
    {
        private readonly RequestDelegate _next;
        private readonly MailAddress _recipient;

        public ExceptionReporter(RequestDelegate next, MailAddress recipient)
        {
            _next = next;
            _recipient = recipient;
        }

        [UsedImplicitly]
        public async Task Invoke(HttpContext httpContext, IUserManager userManager, IEmailService emailService, ILogger<ExceptionReporter> logger)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception e)
            {
                try
                {
                    await emailService.SendEmailNoOverrideAsync(new ExceptionReportModel
                    {
                        Recipient = _recipient,

                        RequestId = Activity.Current?.Id ?? httpContext.TraceIdentifier,
                        RequestMethod = httpContext.Request.Method,
                        RequestUrl = httpContext.Request.GetDisplayUrl(),
                        UserId = userManager.GetUserId(httpContext.User),
                        ErrorMessage = e.Message,
                        StackTrace = e.StackTrace
                    });
                }
                catch (Exception e2)
                {
                    logger.LogError(e2, "An exception occurred while trying to report another exception");
                }

                throw;
            }
        }
    }

    public static class ExceptionReporterExtensions
    {
        public static IApplicationBuilder UseExceptionReporter(this IApplicationBuilder builder, MailAddress recipient)
        {
            return builder.UseMiddleware<ExceptionReporter>(recipient);
        }
    }
}
