using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "Form.Data.Title", Description = "Form.Data.Description")]
    public class DataSectionModel : SectionModelBase
    {
        [Display(Name = "Form.Data.ProjectStorage.Label", Description = "Form.Data.ProjectStorage.Description")]
        public IDictionary<string, StorageAccessRuleModel> StorageAccessRules { get; set; } = new Dictionary<string, StorageAccessRuleModel>();

        [Display(Name = "Form.Data.Preservation.Label", Description = "Form.Data.Preservation.Description")]
        public DataPreservationModel Preservation { get; set; }

        public ExternalPreservationModel ExternalPreservation { get; set; }

        [BindNever] public string OwnerId { get; set; }
        [BindNever] public string OwnerName { get; set; }
        [BindNever] public string OwnerEmail { get; set; }
        [BindNever] public string SupervisorName { get; set; }
    }
}