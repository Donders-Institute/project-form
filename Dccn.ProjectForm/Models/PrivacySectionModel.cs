using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "Subject privacy")]
    public class PrivacySectionModel : SectionModelBase
    {
        [Display(Name = "Types of data", Description = "Which personal data will be processed.")]
        public IDictionary<string, PrivacyKeywordModel> DataTypes { get; set; } = new Dictionary<string, PrivacyKeywordModel>();
        public string CustomDataTypes { get; set; }

        [Display(Name = "Motivation", Description = "Why the data will be processed.")]
        public IDictionary<string, PrivacyKeywordModel> Motivations { get; set; } = new Dictionary<string, PrivacyKeywordModel>();
        public string CustomMotivations { get; set; }

        [Display(Name = "Storage location", Description = "Where the data will be stored.")]
        public IDictionary<string, PrivacyKeywordModel> StorageLocations { get; set; } = new Dictionary<string, PrivacyKeywordModel>();
        public string CustomStorageLocations { get; set; }

        [Display(Name = "Data access", Description = "Who will have access to the data.")]
        public IDictionary<string, PrivacyKeywordModel> DataAccessors { get; set; } = new Dictionary<string, PrivacyKeywordModel>();
        public string CustomDataAccessors { get; set; }

        [Display(Name = "Security measures", Description = "How the data will be secured.")]
        public IDictionary<string, PrivacyKeywordModel> SecurityMeasures { get; set; } = new Dictionary<string, PrivacyKeywordModel>();
        public string CustomSecurityMeasures { get; set; }

        [Display(Name = "Data disposal term", Description = "The time from data acquisition until the moment the data will be disposed.")]
        public int? DataDisposalTermDays { get; set; }
    }

    public class PrivacyKeywordModel
    {
        [BindNever] public string Name { get; set; }

        public bool Present { get; set; }
    }
}