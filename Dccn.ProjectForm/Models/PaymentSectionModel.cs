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
        [Range(0, 999.99, ErrorMessage = "Value must be between {1} and {2}.")]
        public decimal? AverageSubjectCost { get; set; }

//        [Display(Name = "Predicted costs")]
//        [BindNever]
//        public decimal? PredictedCosts { get; set; }

        [Display(Name = "Maximum budget")]
        [DataType(DataType.Currency)]
        [Range(0, 9999.99, ErrorMessage = "Value must be between {1} and {2}.")]
        public decimal? MaxTotalCost { get; set; }
    }
}