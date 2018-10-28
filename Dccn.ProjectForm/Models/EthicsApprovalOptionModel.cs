using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public enum EthicsApprovalOptionModel
    {
        [Display(Name = "Blanket", Description = "CMO2014/288")]
        Blanket,

        [Display(Name = "Children", Description = "CMO2012/012")]
        Children,

        [Display(Name = "Other")]
        Other
    }
}