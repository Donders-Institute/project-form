using System;
using System.Collections.Generic;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global

namespace Dccn.ProjectForm.Data.Repository
{
    public class RepositoryUser
    {
        public string ApiAuthToken { get; set; }
        public DateTime? AttributeLastUpdatedDateTime { get; set; }
        public int ContributorEligibility { get; set; }
        public string CustomDisplayName { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string EmailFrequencyCollectionChanges { get; set; }
        public string GivenName { get; set; }
        public string IdentityProvider { get; set; }
        public string IrodsUserName { get; set; }
        public ISet<string> IsAdminOf { get; set; }
        public string OpenResearcherAndContributorId { get; set; }
        public ISet<string> OrganisationalUnit { get; set; }
        public string PersonalWebsiteUrl { get; set; }
        public string ResearcherId { get; set; }
        public bool SocialAccount { get; set; }
        public string SurName { get; set; }
        public string Uid { get; set; }
        public int ViewerEligibility { get; set; }
    }
}
