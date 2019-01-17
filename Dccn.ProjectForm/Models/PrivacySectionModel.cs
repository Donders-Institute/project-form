using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "Form.Privacy.Title", Description = "Form.Privacy.Description")]
    public class PrivacySectionModel : SectionModelBase
    {
        [Display(Name = "Form.Privacy.Types.Label", Description = "Form.Privacy.Types.Description")]
        public IDictionary<string, PrivacyKeywordModel> DataTypes { get; set; } = new Dictionary<string, PrivacyKeywordModel>();
        public string CustomDataTypes { get; set; }

        [Display(Name = "Form.Privacy.Motivation.Label", Description = "Form.Privacy.Motivation.Description")]
        public IDictionary<string, PrivacyKeywordModel> Motivations { get; set; } = new Dictionary<string, PrivacyKeywordModel>();
        public string CustomMotivations { get; set; }

        [Display(Name = "Form.Privacy.Storage.Label", Description = "Form.Privacy.Storage.Description")]
        public IDictionary<string, PrivacyKeywordModel> StorageLocations { get; set; } = new Dictionary<string, PrivacyKeywordModel>();
        public string CustomStorageLocations { get; set; }

        [Display(Name = "Form.Privacy.Access.Label", Description = "Form.Privacy.Access.Description")]
        public IDictionary<string, PrivacyKeywordModel> DataAccessors { get; set; } = new Dictionary<string, PrivacyKeywordModel>();
        public string CustomDataAccessors { get; set; }

        [Display(Name = "Form.Privacy.Security.Label", Description = "Form.Privacy.Security.Description")]
        public IDictionary<string, PrivacyKeywordModel> SecurityMeasures { get; set; } = new Dictionary<string, PrivacyKeywordModel>();
        public string CustomSecurityMeasures { get; set; }

        [Display(Name = "Form.Privacy.Disposal.Label", Description = "Form.Privacy.Disposal.Description")]
        public int? DataDisposalTermDays { get; set; }
    }

    public class PrivacyKeywordModel
    {
        [BindNever] public string Name { get; set; }

        public bool Present { get; set; }
    }
}