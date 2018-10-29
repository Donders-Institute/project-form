using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "Data management")]
    public class DataSectionModel : SectionModelBase
    {
        [Display(Name = "Project Storage access roles")]
        public IDictionary<string, StorageAccessRuleModel> StorageAccessRules { get; set; }

        [Display(Name = "Data preservation")]
        public DataPreservationModel Preservation { get; set; }

        [Display(Name = "External storage")]
        public ExternalPreservationModel ExternalPreservation { get; set; }

        [BindNever] public string OwnerId { get; set; }
        [BindNever] public string OwnerName { get; set; }
        [BindNever] public string OwnerEmail { get; set; }
        [BindNever] public string SupervisorName { get; set; }
    }
}