﻿using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Dccn.ProjectForm.Data
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class ProposalsDbContext : DbContext
    {
        public ProposalsDbContext(DbContextOptions<ProposalsDbContext> options) : base(options)
        {
        }

        public DbSet<Proposal> Proposals { get; private set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Proposal>(b =>
            {
                b.Property(e => e.CreatedOn).HasColumnType("DATETIME").HasDefaultValueSql("CURRENT_TIMESTAMP");
                b.Property(e => e.LastEditedOn).HasColumnType("DATETIME").HasDefaultValueSql("CURRENT_TIMESTAMP");
                b.Property(e => e.LastEditedBy).IsRequired();
                b.Property(e => e.TimeStamp).IsRowVersion().IsRequired();
                b.Property(e => e.OwnerId).HasMaxLength(10).IsRequired();
                b.Property(e => e.SupervisorId).HasMaxLength(10).IsRequired();
                b.Property(e => e.ProjectId).HasMaxLength(10);
                b.Property(e => e.Title).IsRequired();
                b.Property(e => e.StartDate).HasColumnType("DATE");
                b.Property(e => e.EndDate).HasColumnType("DATE");
                b.Property(e => e.PaymentAverageSubjectCost).HasColumnType("decimal(5, 2)");
                b.Property(e => e.PaymentMaxTotalCost).HasColumnType("decimal(6, 2)");

                b.HasMany(e => e.Approvals).WithOne().HasForeignKey(e => e.ProposalId);
                b.HasMany(e => e.Labs).WithOne().HasForeignKey(e => e.ProposalId);
                b.HasMany(e => e.Experimenters).WithOne().HasForeignKey(e => e.ProposalId);
                b.HasMany(e => e.DataAccessRules).WithOne().HasForeignKey(e => e.ProposalId);
                b.HasMany(e => e.Comments).WithOne().HasForeignKey(e => e.ProposalId);

                b.HasIndex(e => e.OwnerId);
                b.HasIndex(e => e.SupervisorId);
            });

            builder.Entity<Approval>(b =>
            {
                b.Property(e => e.AuthorityRole).HasConversion<string>();
                b.Property(e => e.Status).HasConversion<string>().HasDefaultValue(ApprovalStatus.NotSubmitted);

                b.HasIndex(e => new {e.ProposalId, e.AuthorityRole}).IsUnique();
            });

            builder.Entity<Lab>(b =>
            {
                b.Property(e => e.Modality);

                b.ToTable("Labs");
            });

            builder.Entity<Experimenter>(b =>
            {
                b.OwnsOne(e => e.User, bb =>
                {
                    bb.Property(e => e.Id).HasColumnName("UserId");
                    bb.Property(e => e.DisplayName).IsRequired().HasColumnName("UserName");
                });

                b.ToTable("Experimenters");
            });

            builder.Entity<StorageAccessRule>(b =>
            {
                b.Property(e => e.Role).HasConversion<string>();

                b.OwnsOne(e => e.User, bb =>
                {
                    bb.Property(e => e.Id).HasColumnName("UserId");
                    bb.Property(e => e.DisplayName).IsRequired().HasColumnName("UserName");
                });

                b.ToTable("StorageAccessRules");
            });

            builder.Entity<Comment>(b =>
            {
                b.Property(e => e.SectionId).IsRequired();
                b.Property(e => e.CreatedOn).HasColumnType("DATETIME").HasDefaultValueSql("CURRENT_TIMESTAMP");

                b.HasIndex(e => new {e.ProposalId, e.SectionId}).IsUnique();

                b.ToTable("Comments");
            });
        }
    }
}