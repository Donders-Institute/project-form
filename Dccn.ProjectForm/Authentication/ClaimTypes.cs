using System;
using System.Security.Claims;

namespace Dccn.ProjectForm.Authentication
{
    public static class ClaimTypes
    {
        public const string UserId = nameof(UserId);
        public const string UserName = nameof(UserName);
        public const string IsSupervisor = nameof(IsSupervisor);
        public const string Role = nameof(Role);
        public const string EmailAddress = nameof(EmailAddress);
    }
}