using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartHire.Models;

namespace SmartHire.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }


        public DbSet<RecruiterProfile> RecruiterProfiles { get; set; }
        public DbSet<Job> Jobs { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ApplicationUser ↔ RecruiterProfile

            builder.Entity<RecruiterProfile>()
                .HasOne(r => r.ApplicationUser)
                .WithOne()
                .HasForeignKey<RecruiterProfile>(r => r.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);


            // RecruiterProfile ↔ Jobs

            builder.Entity<Job>()
                .HasOne(j => j.RecruiterProfile)
                .WithMany(r => r.Jobs)
                .HasForeignKey(j => j.RecruiterProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // Salary precision

            builder.Entity<Job>()
                .Property(j => j.SalaryMin)
                .HasPrecision(18, 2);

            builder.Entity<Job>()
                .Property(j => j.SalaryMax)
                .HasPrecision(18, 2);





        }





    }
}