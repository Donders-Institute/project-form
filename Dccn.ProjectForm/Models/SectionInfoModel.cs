using System;
using System.Linq.Expressions;
using Dccn.ProjectForm.Pages;

namespace Dccn.ProjectForm.Models
{
    public class SectionInfoModel
    {
        public string Id { get; set; }
        public Expression<Func<FormModel, ISectionModel>> Expression { get; set; }
        public ISectionModel Model { get; set; }
        public bool CanEdit { get; set; }
        public bool CanApprove { get; set; }
        public bool CanSubmit { get; set; }
    }
}