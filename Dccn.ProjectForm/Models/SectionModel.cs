using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    public class SectionModelBase : ISectionModel
    {
        public string Comments { get; set; }
        [BindNever] public IEnumerable<SectionApprovalModel> Approvals { get; set; }
    }

    public interface ISectionModel
    {
        string Comments { get; set; }
        IEnumerable<SectionApprovalModel> Approvals { get; set; }
    }
}