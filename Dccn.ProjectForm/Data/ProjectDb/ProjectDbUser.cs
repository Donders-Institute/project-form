using System.Text;
using JetBrains.Annotations;

namespace Dccn.ProjectForm.Data.ProjectDb
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
    public class ProjectDbUser
    {
        public string Id { get; private set; }

        public ProjectDbGroup Group { get; private set; }
        public string GroupId { get; private set; }

        public string FirstName { get; private set; }
        public string MiddleName { get; private set; }
        public string LastName { get; private set; }
        public string Initials { get; private set; }

        public string Function { get; private set; }
        public string Email { get; private set; }
        public string InstituteId { get; private set; }

        public CheckinStatus Status { get; private set; }

        public string DisplayName => GetDisplayName(Id, FirstName, MiddleName, LastName);

        public bool IsHead => Group.HeadId == Id;

        public static string GetDisplayName(string id, string firstName, string middleName, string lastName)
        {
            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            {
                return $"<{id}>";
            }

            var builder = new StringBuilder();
            builder.Append(firstName);
            if (!string.IsNullOrEmpty(middleName))
            {
                builder.Append(' ').Append(middleName);
            }
            builder.Append(' ').Append(lastName);

            return builder.ToString();
        }
    }

    public enum CheckinStatus
    {
        Tentative,
        CheckedIn,
        CheckedOut,
        CheckedOutExtended
    }
}
