using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public enum StorageQuotaModel
    {
        [Display(Name = "Form.Experiment.StorageQuota.Standard")]
        Standard,

        [Display(Name = "Form.Experiment.StorageQuota.Custom")]
        Custom
    }
}