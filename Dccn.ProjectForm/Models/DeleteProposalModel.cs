using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    public class DeleteProposalModel
    {
        [BindRequired]
        public int ProposalId { get; set; }
    }
}