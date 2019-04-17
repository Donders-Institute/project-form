using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "Form.Experiment.Title", Description = "Form.Experiment.Description")]
    public class ExperimentSectionModel : SectionModelBase
    {
        [Display(Name = "Form.Experiment.StartDate.Label", Description = "Form.Experiment.StartDate.Description")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name ="Form.Experiment.EndDate.Label", Description = "Form.Experiment.EndDate.Description")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public StorageQuotaModel StorageQuota { get; set; }

        [Display(Name = "Form.Experiment.CustomQuotaAmount.Label", Description = "Form.Experiment.CustomQuotaAmount.Description")]
        public int? CustomStorageQuota { get; set; }

        [Display(Name = "Form.Experiment.CustomStorageQuotaMotivation.Label", Description = "Form.Experiment.CustomStorageQuotaMotivation.Description")]
        public string CustomStorageQuotaMotivation { get; set; }

        [Display(Name = "Form.Experiment.Labs.Label", Description = "Form.Experiment.Labs.Description")]
        public IDictionary<int, LabModel> Labs { get; set; } = new Dictionary<int, LabModel>();

        [Display(Name = "Form.Experiment.Experimenters.Label")]
        public IDictionary<string, ExperimenterModel> Experimenters { get; set; } = new Dictionary<string, ExperimenterModel>();

        [BindNever]
        public int MinimumStorageQuota { get; set; }
    }
}