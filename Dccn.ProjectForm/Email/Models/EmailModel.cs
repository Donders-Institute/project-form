using System.Net.Mail;

namespace Dccn.ProjectForm.Email.Models
{
    public abstract class EmailModelBase : IEmailModel
    {
        public abstract string TemplateName { get; }
        public MailAddress Recipient { get; set; }
        public virtual string Subject { get; set; }

        public void OverrideRecipient(MailAddress address)
        {
            Recipient = address;
        }
    }

    public interface IEmailModel
    {
        string TemplateName { get; }
        MailAddress Recipient { get; }
        string Subject { get; }

        void OverrideRecipient(MailAddress address);
    }
}