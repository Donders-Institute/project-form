namespace Dccn.ProjectForm.Email.Models
{
    public class ExceptionReport : EmailModelBase
    {
        public override string Subject => "Exception report " + RequestId;
        public string RequestId { get; set; }
        public string RequestUrl { get; set; }
        public string RequestMethod { get; set; }
        public string UserId { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
    }
}