using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    public class SectionModelBase : ISectionModel
    {
        public string Comments { get; set; }

        [BindNever] public string Id { get; set; }
        [BindNever] public IEnumerable<SectionApprovalModel> Approvals { get; set; }
        [BindNever] public bool CanEdit { get; set; }
        [BindNever] public bool CanApprove { get; set; }
        [BindNever] public bool CanSubmit { get; set; }
    }

    public interface ISectionModel
    {
        string Id { get; set; }
        string Comments { get; set; }
        IEnumerable<SectionApprovalModel> Approvals { get; set; }
        bool CanEdit { get; set; }
        bool CanApprove { get; set; }
        bool CanSubmit { get; set; }
    }
}