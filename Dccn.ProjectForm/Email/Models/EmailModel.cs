namespace Dccn.ProjectForm.Email.Models
{
    public abstract class EmailModelBase : IEmailModel
    {
        public string TemplateName => GetType().Name;
        public abstract string Subject { get; }
        public virtual bool IsHtml => false;
    }

    public interface IEmailModel
    {
        string TemplateName { get; }
        string Subject { get; }
        bool IsHtml { get; }
    }
}