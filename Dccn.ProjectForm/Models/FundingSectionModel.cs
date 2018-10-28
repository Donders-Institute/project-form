using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "Funding")]
    public class FundingSectionModel : SectionModelBase
    {
        [Display(Name = "Name", Description = "Name of the person responsible for the project's funding.")]
        public string ContactName { get; set; }

        [Display(Name = "Email address", Description = "E-mail address of the contact person.")]
        [EmailAddress]
        public string ContactEmail { get; set; }

        [Display(Name = "Financial code", Description = "Financial code or reference.")]
        public string FinancialCode { get; set; }
    }
}