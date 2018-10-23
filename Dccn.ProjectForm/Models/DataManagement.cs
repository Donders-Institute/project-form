using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dccn.ProjectForm.Data;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "Data management")]
    public class DataManagement : SectionModelBase
    {
        public enum PreservationType
        {
            Repository, External
        }

        [Display(Name = "Project Storage access roles")]
        public IDictionary<Guid, StorageAccessRule> StorageAccessRules { get; set; }

        [Display(Name = "Data preservation")]
        public PreservationType Preservation { get; set; }

        [Display(Name = "External storage")]
        public ExternalPreservationType ExternalPreservation { get; set; }

        [BindNever] public string OwnerId { get; set; }
        [BindNever] public string OwnerName { get; set; }
        [BindNever] public string OwnerEmail { get; set; }
        [BindNever] public string SupervisorName { get; set; }

        public class StorageAccessRule
        {
            public User User { get; set; }
            public StorageAccessRole Role { get; set; }
            [BindNever] public bool CanEdit { get; set; }
            [BindNever] public bool CanRemove { get; set; }
        }

        public class ExternalPreservationType
        {
            [Display(Name = "Location", Description = "The location where the external data will be preserved.")]
            public string Location { get; set; }

            [Display(Name = "Reference", Description = "Project number or reference at external location.")]
            public string Reference { get; set; }

            [Display(Name = "Principal Investigator", Description = "The responsible PI at the external location.")]
            public string SupervisorName { get; set; }
        }
    }
}