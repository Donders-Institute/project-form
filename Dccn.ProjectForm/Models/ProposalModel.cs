using System;
using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    public class ProposalModel
    {
        public int Id { get; set; }

        [Display(Name = "Title")]
        public string Title { get; set; }

        [Display(Name = "Project owner")]
        public string OwnerName { get; set; }

        [Display(Name = "Principal investigator")]
        public string SupervisorName { get; set; }

        [Display(Name = "Created on")]
        public DateTime CreatedOn { get; set; }

        [Display(Name = "Last edited on")]
        public DateTime LastEditedOn { get; set; }

        [Display(Name = "Last edited by")]
        public string LastEditedBy { get; set; }

        [Display(Name = "Project ID")]
        //[DisplayFormat(NullDisplayText = "<not set>")]
        public string ProjectId { get; set; }
    }
}