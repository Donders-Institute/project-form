namespace Dccn.ProjectForm.Configuration
{
    public class EmailOptions
    {
        public const string SectionName = "Email";

        public string Host { get; set; }
        public int Port { get; set; } = 587;

        public string UserName { get; set; }
        public string Password { get; set; }

        public EmailAddress Sender { get; set; }
        public bool OverrideRecipient { get; set; }
    }
}