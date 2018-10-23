using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dccn.ProjectForm.Data.Projects
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
    public class ProjectsDbContext : DbContext{

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
                b.Property(e => e.Hidden).HasColumnName("hidden");

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

                b.HasOne(e => e.Head).WithOne();
                b.HasMany(e => e.Members).WithOne(e => e.Group).HasForeignKey(e => e.GroupId);

                b.ToTable("groups");
            });
        }
    }
}
