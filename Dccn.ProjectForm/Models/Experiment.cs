using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "Experiment")]
    public class Experiment : SectionModelBase, IValidatableObject
    {
        public enum StorageQuotaType
        {
            Standard, Custom
        }

        [Display(Name = "Start date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name ="End date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public StorageQuotaType StorageQuota { get; set; }

        [Display(Name ="Storage amount", Description = "Storage amount in Gigabytes.")]
        public int? CustomQuotaAmount { get; set; }

        [Display(Name ="Motivation")]
        public string CustomQuotaMotivation { get; set; }

        [Display(Name = "Labs")]
        public IDictionary<Guid, Lab> Labs { get; set; }

        [Display(Name ="Experimenters")]
        public IDictionary<Guid, User> Experimenters { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate < StartDate)
            {
                yield return new ValidationResult("End date must come after start date.", new []{nameof(EndDate)});
            }

            if ((Labs == null || !Labs.Any()) && StorageQuota != StorageQuotaType.Custom)
            {
                yield return new ValidationResult("Must specify overruling storage quota when there is no lab usage.", new []{nameof(StorageQuota)});
            }
        }
    }
}