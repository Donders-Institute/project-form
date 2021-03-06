﻿using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;

namespace Dccn.ProjectForm.Data
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class ProposalDbContext : DbContext
    {
        private const int UserIdMaxLength = 10;

        public ProposalDbContext(DbContextOptions<ProposalDbContext> options) : base(options)
        {
        }

        public DbSet<Proposal> Proposals { get; private set; }
        public DbSet<Approval> Approvals { get; private set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Proposal>(b =>
            {
                b.Property(e => e.CreatedOn).HasColumnType("DATETIME").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
                b.Property(e => e.LastEditedOn).HasColumnType("DATETIME").HasDefaultValueSql("CURRENT_TIMESTAMP");
                b.Property(e => e.LastEditedBy).IsRequired();
                b.Property(e => e.Timestamp).HasColumnType("ROWVERSION").IsRequired().ValueGeneratedOnAddOrUpdate();
                b.Property(e => e.OwnerId).HasMaxLength(UserIdMaxLength).IsRequired();
                b.Property(e => e.SupervisorId).HasMaxLength(UserIdMaxLength).IsRequired();
                b.Property(e => e.ProjectId).HasMaxLength(10);
                b.Property(e => e.Title).IsRequired();
                b.Property(e => e.StartDate).HasColumnType("DATE");
                b.Property(e => e.EndDate).HasColumnType("DATE");
                b.Property(e => e.PaymentAverageSubjectCost).HasColumnType("DECIMAL(5, 2)");
                b.Property(e => e.PaymentMaxTotalCost).HasColumnType("DECIMAL(9, 2)");

                var jsonConverter = new ValueConverter<ICollection<string>, string>(
                                        m => JsonConvert.SerializeObject(m),
                                        p => JsonConvert.DeserializeObject<ICollection<string>>(p));

                b.Property(e => e.PrivacyDataTypes).IsRequired().HasConversion(jsonConverter);
                b.Property(e => e.PrivacyMotivations).IsRequired().HasConversion(jsonConverter);
                b.Property(e => e.PrivacyStorageLocations).IsRequired().HasConversion(jsonConverter);
                b.Property(e => e.PrivacyDataAccessors).IsRequired().HasConversion(jsonConverter);
                b.Property(e => e.PrivacySecurityMeasures);

                b.HasMany(e => e.Approvals).WithOne(e => e.Proposal).HasForeignKey(e => e.ProposalId);
                b.HasMany(e => e.Labs).WithOne().HasForeignKey(e => e.ProposalId);
                b.HasMany(e => e.Experimenters).WithOne().HasForeignKey(e => e.ProposalId);
                b.HasMany(e => e.StorageAccessRules).WithOne().HasForeignKey(e => e.ProposalId);
                b.HasMany(e => e.Comments).WithOne().HasForeignKey(e => e.ProposalId);

                b.HasIndex(e => e.OwnerId);
                b.HasIndex(e => e.SupervisorId);
                b.HasIndex(e => e.ProjectId).IsUnique();
            });

            builder.Entity<Approval>(b =>
            {
                b.HasKey(e => new {e.ProposalId, e.AuthorityRole});

                b.Property(e => e.AuthorityRole).HasConversion<string>();
                b.Property(e => e.Status).HasConversion<string>().HasDefaultValue(ApprovalStatus.NotSubmitted);

                b.HasIndex(e => e.AuthorityRole);

                b.ToTable("Approvals");
            });

            builder.Entity<Lab>(b =>
            {
                b.Property(e => e.Modality);

                b.ToTable("Labs");
            });

            builder.Entity<Experimenter>(b =>
            {
                b.HasKey(e => new {e.ProposalId, e.UserId});

                b.Property(e => e.UserId).HasMaxLength(UserIdMaxLength);

                b.ToTable("Experimenters");
            });

            builder.Entity<StorageAccessRule>(b =>
            {
                b.HasKey(e => new {e.ProposalId, e.UserId});

                b.Property(e => e.UserId).HasMaxLength(UserIdMaxLength);
                b.Property(e => e.Role).HasConversion<string>();

                b.ToTable("StorageAccessRules");
            });

            builder.Entity<Comment>(b =>
            {
                b.HasKey(e => new {e.ProposalId, e.SectionId});

                b.Property(e => e.Content).IsRequired();

                b.ToTable("Comments");
            });
        }
    }
}
