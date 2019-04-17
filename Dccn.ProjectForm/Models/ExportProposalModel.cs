using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    public class ExportProposalModel
    {
        [BindRequired]
        public int ProposalId { get; set; }

        [Required]
        [RegularExpression("^\\d{7}$", ErrorMessage = "Source ID must consist of 7 digits.")]
        [Display(Name = "Funding source", Description = "The funding source ID that will be used as the first part of the generated project ID.")]
        public string SourceId { get; set; }
    }
}