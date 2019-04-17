using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public enum ProposalStatusModel
    {
        [Display(Name = "In progress")]
        InProgress,

        [Display(Name = "Approved")]
        Approved,

        [Display(Name = "Completed")]
        Completed
    }
}