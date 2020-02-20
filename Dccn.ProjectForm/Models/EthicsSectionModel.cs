using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "Form.Ethics.Title", Description = "Form.Ethics.Description")]
    public class EthicsSectionModel : SectionModelBase
    {
        public EthicsApprovalStatusModel Status { get; set; }

        [Display(Name = "Form.Ethics.Approved.Code.Label", Description = "Form.Ethics.Approved.Code.Description")]
        public string Code { get; set; }

        [Display(Name = "Form.Ethics.Pending.CorrespondenceNumber.Label" /*, Description = "Form.Ethics.Pending.CorrespondenceNumber.Description" */)]
        public string CorrespondenceNumber { get; set; }

        [BindNever]
        public IDictionary<string, string> Codes { get; set; }
    }
}