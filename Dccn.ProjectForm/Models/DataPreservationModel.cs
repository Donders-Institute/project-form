using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public enum DataPreservationModel
    {
        [Display(Name = "Form.Data.Preservation.DondersRepository.Label", Description = "Form.Data.Preservation.DondersRepository.Description")]
        Repository,

        [Display(Name = "Form.Data.Preservation.External.Label", Description = "Form.Data.Preservation.External.Description")]
        External
    }
}