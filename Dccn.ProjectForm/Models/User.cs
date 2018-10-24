using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    public class User
    {
        public string Id { get; set; }

        [BindNever]
        public string Name { get; set; }
    }
}