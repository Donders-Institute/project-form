using System.Net.Mail;

namespace Dccn.ProjectForm.Email.Models
{
    public abstract class EmailModelBase : IEmailModel
    {
        public virtual string TemplateName => GetType().Name;
        public MailAddress Recipient { get; set; }
        public virtual string Subject { get; set; }
    }

    public interface IEmailModel
    {
        string TemplateName { get; }
        MailAddress Recipient { get; }
        string Subject { get; }
    }
}