using System.ComponentModel.DataAnnotations;

namespace Dccn.ProjectForm.Models
{
    [Display(Name = "Subject payment")]
    public class PaymentSectionModel : SectionModelBase
    {
        [Display(Name = "Subjects")]
        public int? SubjectCount { get; set; }

        [Display(Name = "Average cost")]
        [DataType(DataType.Currency)]
        public decimal? AverageSubjectCost { get; set; }

//        [Display(Name = "Predicted costs")]
//        [BindNever]
//        public decimal? PredictedCosts { get; set; }

        [Display(Name = "Maximum budget")]
        [DataType(DataType.Currency)]
        public decimal? MaxTotalCost { get; set; }
    }
}