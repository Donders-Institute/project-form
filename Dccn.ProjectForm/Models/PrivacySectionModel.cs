using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "Subject privacy")]
    public class PrivacySectionModel : SectionModelBase
    {
        public enum DataType
        {
            Name, Address, Email, Phone, BirthDay, SonaId
        }

        public enum Motivation
        {
            ExperimentScheduling, IncidentalFindings
        }

        public enum StorageLocation
        {
            MDrive, PDrive, Dcc
        }

        public enum DataAccessRole
        {
            Manager, Contributor
        }

        public enum SecurityMeasure
        {
            Password, Keyfile
        }

        public enum DataDisposalTermType
        {
            OneMonth, OneYear, Other
        }

        [Display(Name = "Types of data", Description = "Which personal data will be processed.")]
        public ISet<DataType> DataTypes { get; set; }
        public string CustomDataType { get; set; }

        [Display(Name = "Motivation", Description = "Why the data will be processed.")]
        public ISet<Motivation> Motivations { get; set; }
        public string CustomMotivation { get; set; }

        [Display(Name = "Storage location", Description = "Where the data will be stored.")]
        public ISet<StorageLocation> StorageLocations { get; set; }
        public string CustomStorageLocation { get; set; }

        [Display(Name = "Data access", Description = "Who will have access to the data.")]
        public ISet<DataAccessRole> Accessors { get; set; }
        public string CustomAccessor { get; set; }

        [Display(Name = "Security measures", Description = "How the data will be secured.")]
        public ISet<SecurityMeasure> SecurityMeasures { get; set; }
        public string CustomSecurityMeasure { get; set; }

        [Display(Name = "Data disposal", Description = "The time from data acquisition until the moment the data will be disposed.")]
        public DataDisposalTermType DataDisposalTerm { get; set; }
        public string CustomDataDisposalTerm { get; set; }
    }
}