using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dccn.ProjectForm.Data.Projects
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
    public class ProjectsDbContext : DbContext
    {
        public ProjectsDbContext(DbContextOptions<ProjectsDbContext> options) : base(options)
        {
        }

        public DbSet<ProjectsUser> Users { get; private set; }
        public DbSet<ProjectsGroup> Groups { get; private set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ProjectsUser>(b =>
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

            builder.Entity<ProjectsGroup>(b =>
            {
                b.Property(e => e.Id).HasColumnName("id");
                b.Property(e => e.HeadId).HasColumnName("head_id");
                b.Property(e => e.HeadIsPi).HasColumnName("head_is_pi").HasConversion(new BoolToStringConverter("no", "yes"));
                b.Property(e => e.Description).HasColumnName("description");
                b.Property(e => e.Hidden).HasColumnName("hidden");
                b.Property(e => e.InstituteId).HasColumnName("institute_id");

                // Not used
                b.Ignore(e => e.HeadIsPi);
                // b.Ignore(e => e.Hidden);
                b.Ignore(e => e.InstituteId);

                b.HasOne(e => e.Head).WithOne();
                b.HasMany(e => e.Members).WithOne(e => e.Group).HasForeignKey(e => e.GroupId);

                b.ToTable("groups");
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
