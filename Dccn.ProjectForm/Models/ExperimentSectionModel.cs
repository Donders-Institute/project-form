using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "Experiment")]
    public class ExperimentSectionModel : SectionModelBase
    {
//        public ExperimentSectionModel()
//        {
//            Labs = Enumerable.Empty<LabModel>();
//            Experimenters = Enumerable.Empty<ExperimenterModel>();
//        }

        [Display(Name = "Start date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name ="End date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public StorageQuotaModel StorageQuota { get; set; }

        [Display(Name ="Storage amount", Description = "Storage amount in Gigabytes.")]
        public int? CustomQuotaAmount { get; set; }

        [Display(Name ="Motivation")]
        public string CustomQuotaMotivation { get; set; }

        [Display(Name = "Labs")]
        public IList<LabModel> Labs { get; set; } = new List<LabModel>();

        [Display(Name ="Experimenters")]
        public IList<ExperimenterModel> Experimenters { get; set; } = new List<ExperimenterModel>();
    }
}