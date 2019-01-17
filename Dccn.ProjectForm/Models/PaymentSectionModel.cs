using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "Form.Payment.Title", Description = "Form.Payment.Description")]
    public class PaymentSectionModel : SectionModelBase
    {
        [Display(Name = "Form.Payment.Subjects.Label", Description = "Form.Payment.Subjects.Description")]
        public int? SubjectCount { get; set; }

        [Display(Name = "Form.Payment.AverageCost.Label", Description = "Form.Payment.AverageCost.Description")]
        [DataType(DataType.Currency)]
        public decimal? AverageSubjectCost { get; set; }

//        [Display(Name = "Predicted costs")]
//        [BindNever]
//        public decimal? PredictedCosts { get; set; }

        [Display(Name = "Form.Payment.MaximumBudget.Label", Description = "Form.Payment.MaximumBudget.Description")]
        [DataType(DataType.Currency)]
        public decimal? MaxTotalCost { get; set; }
    }
}