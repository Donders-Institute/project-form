using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    public class RejectSectionModel
    {
        [Display(Name = "Reason", Description = "Reason for rejecting this section to send to the applicant.")]
        [Required]
        public string Reason { get; set; }

        [BindRequired]
        public ApprovalAuthorityRoleModel Role { get; set; }
    }
}