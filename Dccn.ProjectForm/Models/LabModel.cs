using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    public class LabModel
    {
        public int Id { get; set; }

        [Display(Name = "Form.Experiment.Labs.SubjectCount.Label", Description = "Form.Experiment.Labs.SubjectCount.Description")]
        public int? SubjectCount { get; set; }

        [Display(Name = "Form.Experiment.Labs.ExtraSubjectCount.Label", Description = "Form.Experiment.Labs.ExtraSubjectCount.Description")]
        public int? ExtraSubjectCount { get; set; }

        [Display(Name = "Form.Experiment.Labs.SessionCount.Label", Description = "Form.Experiment.Labs.SessionCount.Description")]
        public int? SessionCount { get; set; }

        [Display(Name = "Form.Experiment.Labs.SessionDurationMinutes.Label", Description = "Form.Experiment.Labs.SessionDurationMinutes.Description")]
        public int? SessionDurationMinutes { get; set; }

        [Display(Name = "Form.Experiment.Labs.Modality.Label", Description = "Form.Experiment.Labs.Modality.Description")]
        [BindNever]
        public ModalityModel Modality { get; set; }
    }
}