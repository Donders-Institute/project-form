﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dccn.ProjectForm.Models
{
    public class NewProposalModel
    {
        [Display(Name = "Title", Description = "The title of your project proposal.")]
        [Required(ErrorMessage = "A title is required.")]
        [MinLength(10, ErrorMessage = "The title must contain at least {0} characters.")]
        public string Title { get; set; }

        [Display(Name = "Principal investigator", Description = "The principal investigator responsible for project supervision. This cannot be changed later.")]
        [Required]
        public string SupervisorId { get; set; }

        [BindNever]
        public IList<SelectListItem> Supervisors { get; set; }

        [BindNever]
        public bool Submitted { get; set; }
    }
}