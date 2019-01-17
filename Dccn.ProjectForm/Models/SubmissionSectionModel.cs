using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "Form.Submission.Title", Description = "Form.Submission.Description")]
    public class SubmissionSectionModel : SectionModelBase
    {
        [BindNever] public IEnumerable<RequiredApprovalModel> NeedsApprovalBy { get; set; }
    }
}