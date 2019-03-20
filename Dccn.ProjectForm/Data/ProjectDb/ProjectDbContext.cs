using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dccn.ProjectForm.Data.ProjectDb
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
    public class ProjectDbContext : DbContext
    {
        public ProjectDbContext(DbContextOptions<ProjectDbContext> options) : base(options)
        {
        }

        public DbSet<ProjectDbUser> Users { get; private set; }
        public DbSet<ProjectDbGroup> Groups { get; private set; }
        public DbSet<ProjectDbProject> Projects { get; private set; }
        public DbSet<ProjectDbImagingMethod> ImagingMethods { get; private set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            var yesNoConverter = new BoolToStringConverter("no", "yes");

            // Read-only tables
            builder.Entity<ProjectDbUser>(b =>
            {
                b.Property(e => e.Id).HasColumnName("id");
                b.Property(e => e.GroupId).HasColumnName("group_id");
                b.Property(e => e.FirstName).HasColumnName("firstName");
                b.Property(e => e.MiddleName).HasColumnName("middleName");
                b.Property(e => e.LastName).HasColumnName("lastName");
                b.Property(e => e.Initials).HasColumnName("initials");
                b.Property(e => e.Function).HasColumnName("function");
                b.Property(e => e.Email).HasColumnName("email");
                b.Property(e => e.InstituteId).HasColumnName("institute_id");
                b.Property(e => e.Status).HasColumnName("status").HasConversion(new CheckinStatusConverter());

                // Not used
                b.Ignore(e => e.Initials);
                b.Ignore(e => e.Function);
                b.Ignore(e => e.InstituteId);

                b.ToTable("users");
            });

            builder.Entity<ProjectDbGroup>(b =>
            {
                b.Property(e => e.Id).HasColumnName("id");
                b.Property(e => e.HeadId).HasColumnName("head_id");
                b.Property(e => e.HeadIsPi).HasColumnName("head_is_pi").HasConversion(yesNoConverter);
                b.Property(e => e.Description).HasColumnName("description");
                b.Property(e => e.Hidden).HasColumnName("hidden");
                b.Property(e => e.InstituteId).HasColumnName("institute_id");

                // Not used
                b.Ignore(e => e.HeadIsPi);
                b.Ignore(e => e.InstituteId);

                b.HasOne(e => e.Head).WithOne();
                b.HasMany(e => e.Members).WithOne(e => e.Group).HasForeignKey(e => e.GroupId);

                b.ToTable("groups");
            });

            builder.Entity<ProjectDbImagingMethod>(b =>
            {
                b.Property(e => e.Id).HasColumnName("id");
                b.Property(e => e.BillingCategory).HasColumnName("billing_category");
                b.Property(e => e.Description).HasColumnName("description");
                b.Property(e => e.ImagingGroup).HasColumnName("imaging_group");
                b.Property(e => e.SessionQuota).HasColumnName("sessionQuota");
                b.Property(e => e.Url).HasColumnName("url");

                b.ToTable("imaging_methods");
            });



            // RW tables
            builder.Entity<ProjectDbProject>(b =>
            {
                b.Property(e => e.Id).HasColumnName("id").HasMaxLength(20);
                b.Property(e => e.AuthorshipRemarks).IsRequired().HasColumnName("authorshipRemarks");
                b.Property(e => e.AuthorshipSequence).IsRequired().HasColumnName("authorshipSequence");
                b.Property(e => e.CalculatedProjectSpace).HasColumnName("calculatedProjectSpace").HasDefaultValue();
                b.Property(e => e.Created).HasColumnName("created").HasDefaultValue();
                b.Property(e => e.EcRequestedBy).IsRequired().HasColumnName("ecRequestedBy").HasMaxLength(255).HasDefaultValue();
                b.Property(e => e.EndingDate).HasColumnName("endingDate").HasDefaultValue();
                b.Property(e => e.IncreaseDate).HasColumnName("increaseDate");
                b.Property(e => e.OldId).IsRequired().HasColumnName("oldid").HasMaxLength(20).HasDefaultValue();
                b.Property(e => e.OwnerId).IsRequired().HasColumnName("owner_id").HasMaxLength(10).HasDefaultValue();
                b.Property(e => e.PpmApprovedQuota).HasColumnName("ppmApprovedQuota").HasDefaultValue();
                b.Property(e => e.PpmDate).HasColumnName("ppmDate");
                b.Property(e => e.ProjectEndDate).HasColumnName("projectEndDate").HasDefaultValue();
                b.Property(e => e.ProjectName).IsRequired().HasColumnName("projectName").HasMaxLength(255).HasDefaultValue();
                b.Property(e => e.SourceId).IsRequired().HasColumnName("source_id").HasMaxLength(20).HasDefaultValue();
                b.Property(e => e.StartingDate).HasColumnName("startingDate").HasDefaultValue();
                b.Property(e => e.TotalProjectSpace).HasColumnName("totalProjectSpace").HasDefaultValue();
                b.Property(e => e.Updated).HasColumnName("updated").HasDefaultValue();
                b.Property(e => e.UsedProjectSpace).HasColumnName("usedProjectSpace").HasDefaultValue();
                b.Property(e => e.EcApproved).HasColumnName("ecApproved").HasConversion(yesNoConverter).HasDefaultValue();
                b.Property(e => e.ApprovedAdministration).HasColumnName("approvedAdministration").HasConversion(yesNoConverter).HasDefaultValue();
                b.Property(e => e.Sticky).HasColumnName("sticky").HasConversion(yesNoConverter).HasDefaultValue();
                b.Property(e => e.IncreaseQuota).HasColumnName("increaseQuota").HasConversion(yesNoConverter).HasDefaultValue();
                b.Property(e => e.ProjectBased).HasColumnName("projectBased").HasConversion(yesNoConverter).HasDefaultValue();
                b.Property(e => e.Active).HasColumnName("status").HasConversion(new BoolToStringConverter("inactive", "active")).HasDefaultValue();

                b.HasMany(e => e.ProjectMembers).WithOne().HasForeignKey(e => e.ProjectId);
                b.HasMany(e => e.Experiments).WithOne().HasForeignKey(e => e.ProjectId);

                b.ToTable("projects");
            });

            builder.Entity<ProjectDbProjectMember>(b =>
            {
                b.HasKey(e => new {e.UserId, e.ProjectId});

                b.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(10).IsRequired();
                b.Property(e => e.ProjectId).HasColumnName("project_id").HasMaxLength(20).IsRequired();
                b.Property(e => e.Created).HasColumnName("created").IsRequired();
                b.Property(e => e.Updated).HasColumnName("updated").IsRequired();
                b.Property(e => e.Role).HasColumnName("role").IsRequired();
                b.Property(e => e.Action).HasColumnName("action");
                b.Property(e => e.Activated).HasColumnName("activated").HasConversion(yesNoConverter).HasDefaultValue().IsRequired();

                b.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);

                b.ToTable("projectmembers");
            });

            builder.Entity<ProjectDbExperiment>(b =>
            {
                b.Property(e => e.Id).HasColumnName("id");
                b.Property(e => e.CalculatedQuota).HasColumnName("calculatedQuota").HasDefaultValue();
                b.Property(e => e.Created).HasColumnName("created").HasDefaultValue();
                b.Property(e => e.EndingDate).HasColumnName("endingDate").HasDefaultValue();
                b.Property(e => e.ExperimentEndDate).HasColumnName("experimentEndDate").HasDefaultValue();
                b.Property(e => e.ExperimenterId).IsRequired().HasMaxLength(10).HasColumnName("experimenter_id").HasDefaultValue();
                b.Property(e => e.ImagingMethodId).IsRequired().HasMaxLength(15).HasColumnName("imaging_method_id").HasDefaultValue();
                b.Property(e => e.IncreaseQuotaDate).HasColumnName("increaseQuotaDate");
                b.Property(e => e.IncreasedQuota).HasColumnName("increasedQuota").HasDefaultValue();
                b.Property(e => e.LabTime).HasColumnName("labtime");
                b.Property(e => e.NoSessions).HasColumnName("noSessions").HasDefaultValue();
                b.Property(e => e.NoSubjects).HasColumnName("noSubjects").HasDefaultValue();
                b.Property(e => e.Offset).HasColumnName("offset");
                b.Property(e => e.PpmApprovedQuota).HasColumnName("ppmApprovedQuota");
                b.Property(e => e.ProjectId).IsRequired().HasColumnName("project_id").HasMaxLength(20).HasDefaultValue();
                b.Property(e => e.ProjectOldId).IsRequired().HasColumnName("project_oldid").HasMaxLength(20).HasDefaultValue();
                b.Property(e => e.QuotaUserId).IsRequired().HasMaxLength(10).HasColumnName("quota_user_id").HasDefaultValue();
                b.Property(e => e.StartingDate).HasColumnName("startingDate").HasDefaultValue();
                b.Property(e => e.Suffix).IsRequired().HasColumnName("suffix").HasMaxLength(4).HasDefaultValue();
                b.Property(e => e.Updated).HasColumnName("updated").HasDefaultValue();

                b.HasOne(e => e.Experimenter).WithMany().HasForeignKey(e => e.ExperimenterId);
                b.HasOne(e => e.QuotaUser).WithMany().HasForeignKey(e => e.QuotaUserId);
                b.HasOne(e => e.ImagingMethod).WithMany().HasForeignKey(e => e.ImagingMethodId);

                b.ToTable("experiments");
            });
        }

        private class CheckinStatusConverter : ValueConverter<CheckinStatus, string>
        {
            public CheckinStatusConverter(ConverterMappingHints mappingHints = null) : base(ToString(), ToEnum(), mappingHints)
            {
            }

            private new static Expression<Func<CheckinStatus, string>> ToString() => value => ConvertToString(value);

            private static string ConvertToString(CheckinStatus value)
            {
                switch (value)
                {
                    case CheckinStatus.Tentative:
                        return "tentative";
                    case CheckinStatus.CheckedIn:
                        return "checked in";
                    case CheckinStatus.CheckedOut:
                        return "checked out";
                    case CheckinStatus.CheckedOutExtended:
                        return "checked out extended";
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }
            }

            private static Expression<Func<string, CheckinStatus>> ToEnum() => value => ConvertToEnum(value);

            private static CheckinStatus ConvertToEnum(string value)
            {
                switch (value)
                {
                    case "tentative":
                        return CheckinStatus.Tentative;
                    case "checked in":
                        return CheckinStatus.CheckedIn;
                    case "checked out":
                        return CheckinStatus.CheckedOut;
                    case "checked out extended":
                        return CheckinStatus.CheckedOutExtended;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }
            }
        }
    }
}
