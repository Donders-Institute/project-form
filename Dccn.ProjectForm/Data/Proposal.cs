﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace Dccn.ProjectForm.Data
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class Proposal
    {
        public int Id { get; private set; }

        public byte[] Timestamp { get; private set; }
        public DateTime CreatedOn { get; private set; }
        public DateTime LastEditedOn { get; set; }
        public string LastEditedBy { get; set; }
//        public DateTime ExportedOn { get; set; }
//        public string ExportedBy { get; set; }

        public string OwnerId { get; set; }

        public ICollection<Approval> Approvals { get; set; }

        #region General
        public string ProjectId { get; set; }
        public string Title { get; set; }
        public string SupervisorId { get; set; }
        #endregion

        #region Funding
        public string FundingContactName { get; set; }
        public string FundingContactEmail { get; set; }
        public string FinancialCode { get; set; }
        #endregion

        #region Ethical approval
        public bool EcApproved { get; set; }
        public string EcCode { get; set; }
        public string EcReference { get; set; }
        #endregion

        #region Experiment
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool CustomQuota { get; set; }
        public int? CustomQuotaAmount { get; set; }
        public string CustomQuotaMotivation { get; set; }

        public ICollection<Lab> Labs { get; set; }
        public ICollection<Experimenter> Experimenters { get; set; }
        #endregion

        #region Data management
        public bool ExternalPreservation { get; set; }
        public string ExternalPreservationLocation { get; set; }
        public string ExternalPreservationSupervisor { get; set; }
        public string ExternalPreservationReference { get; set; }

        public ICollection<StorageAccessRule> StorageAccessRules { get; set; }
        #endregion

        #region Privacy
        public ICollection<string> PrivacyDataTypes { get; set; } = new Collection<string>();
        public ICollection<string> PrivacyMotivations { get; set; } = new Collection<string>();
        public ICollection<string> PrivacyStorageLocations { get; set; } = new Collection<string>();
        public ICollection<string> PrivacyDataAccessors { get; set; } = new Collection<string>();
        public string PrivacySecurityMeasures { get; set; }
        public string PrivacyDataDisposalTerm { get; set; }
        #endregion

        #region Payment
        public int? PaymentSubjectCount { get; set; }
        public decimal? PaymentAverageSubjectCost { get; set; }
        public decimal? PaymentMaxTotalCost { get; set; }
        #endregion

        public ICollection<Comment> Comments { get; set; }
    }
}
