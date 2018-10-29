using System;
using System.Diagnostics;
using System.Net.Mail;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Configuration;
using Dccn.ProjectForm.Email.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

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
        public async Task Invoke(HttpContext httpContext, IUserManager userManager, IEmailService emailService)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception e)
            {
                await emailService.SendEmailAsync(httpContext.User, new ExceptionReport
                {
                    Recipient = _recipient,

                    RequestId = Activity.Current?.Id ?? httpContext.TraceIdentifier,
                    RequestMethod = httpContext.Request.Method,
                    RequestUrl = httpContext.Request.GetDisplayUrl(),
                    UserId = userManager.GetUserId(httpContext.User),
                    ErrorMessage = e.Message,
                    StackTrace = e.StackTrace
                });

                throw;
            }
        }
    }

    public static class ExceptionReporterExtensions
    {
        [PublicAPI]
        public static IApplicationBuilder UseExceptionReporter(this IApplicationBuilder builder, EmailAddress recipient)
        {
            return builder.UseMiddleware<ExceptionReporter>(new MailAddress(recipient.Address, recipient.DisplayName));
        }
    }
}
