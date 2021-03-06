﻿using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dccn.ProjectForm.Models
{
    public abstract class UserModel
    {
        public string Id { get; set; }

        [BindNever]
        public string Name { get; set; }
    }
}