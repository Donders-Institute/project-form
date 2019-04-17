using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    public class ApproveSectionModel
    {
        [Display(Name = "Remarks", Description = "Optional remark to send to the applicant.")]
        public string Remarks { get; set; }

        [BindRequired]
        public ApprovalAuthorityRoleModel Role { get; set; }
    }
}