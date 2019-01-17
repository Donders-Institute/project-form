using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "Form.Funding.Title")]
    public class FundingSectionModel : SectionModelBase
    {
        [Display(Name = "Form.Funding.ContactName.Label", Description = "Form.Funding.ContactName.Description")]
        public string ContactName { get; set; }

        [Display(Name = "Form.Funding.ContactEmail.Label", Description = "Form.Funding.ContactEmail.Description")]
        public string ContactEmail { get; set; }

        [Display(Name = "Form.Funding.FinancialCode.Label", Description = "Form.Funding.FinancialCode.Description")]
        public string FinancialCode { get; set; }
    }
}