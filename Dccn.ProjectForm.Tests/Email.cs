using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Dccn.ProjectForm.Configuration;
using Dccn.ProjectForm.Email.Models;
using Dccn.ProjectForm.Models;
using Dccn.ProjectForm.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dccn.ProjectForm.Tests
{
    public class Email
    {
        private readonly IEmailService _service;

        public Email()
        {
            var services = new ServiceCollection();

            services
                .Configure<EmailOptions>(options =>
                {
                    options.Host = "smtp.ru.nl";
                    options.Port = 25;
                    options.Sender = new EmailAddress
                    {
                        Address = "no-reply@donders.ru.nl",
                        DisplayName = "noreply"
                    };
                });

            services.AddEmail("/Email/Templates");

            var serviceProvider = services.BuildServiceProvider();

            _service = serviceProvider.GetService<IEmailService>();
        }

        [Fact]
        public async Task SendEmail()
        {
            await _service.SendEmailAsync(new Model
            {
                Subject = "Test",
                Recipient = new MailAddress("j.neutelings@donders.ru.nl", "Jascha Neutelings"),
                Foo = "foo",
                Bar = "BAR"
            }, null);
        }

        public class Model : EmailModelBase
        {
            public override string TemplateName => "EmailTemplate";
            public string Foo { get; set; }
            public string Bar { get; set; }
        }
    }
}
