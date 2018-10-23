﻿using System.Text;
using JetBrains.Annotations;

namespace Dccn.ProjectForm.Data.Projects
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
    public class ProjectsUser
    {
        public string Id { get; private set; }

        public ProjectsGroup Group { get; private set; }
        public string GroupId { get; private set; }

        public string FirstName { get; private set; }
        public string MiddleName { get; private set; }
        public string LastName { get; private set; }
        public string Initials { get; private set; }

        public string Function { get; private set; }
        public string Email { get; private set; }
        public string InstituteId { get; private set; }

        public bool Hidden { get; private set; }

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(LastName))
                {
                    return $"<{Id}>";
                }

                var builder = new StringBuilder();
                builder.Append(FirstName);
                if (!string.IsNullOrEmpty(MiddleName))
                {
                    builder.Append(' ').Append(MiddleName);
                }
                builder.Append(' ').Append(LastName);

                return builder.ToString();
            }
        }

        public bool IsHead => Group.HeadId == Id;
        public bool IsPi => IsHead && Group.HeadIsPi;
    }
}
