using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "Experiment")]
    public class ExperimentSectionModel : SectionModelBase, IValidatableObject
    {
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
        public IDictionary<Guid, LabModel> Labs { get; set; }

        [Display(Name ="Experimenters")]
        public IDictionary<Guid, UserModel> Experimenters { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate < StartDate)
            {
                yield return new ValidationResult("End date must come after start date.", new []{nameof(EndDate)});
            }

            if ((Labs == null || !Labs.Any()) && StorageQuota != StorageQuotaModel.Custom)
            {
                yield return new ValidationResult("Must specify overruling storage quota when there is no lab usage.", new []{nameof(StorageQuota)});
            }
        }
    }

    public enum StorageQuotaModel
    {
        Standard, Custom
    }
}