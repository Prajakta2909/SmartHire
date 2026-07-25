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
        public DbSet<CandidateProfile> CandidateProfiles { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<Interview> Interviews { get; set; }
        public DbSet<Notification> Notifications { get; set; }


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


            builder.Entity<CandidateProfile>()
                .HasOne(c => c.ApplicationUser)
                .WithOne()
                .HasForeignKey<CandidateProfile>(c => c.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);




            builder.Entity<JobApplication>()
                .HasOne(a => a.CandidateProfile)
                .WithMany(c => c.JobApplications)
                .HasForeignKey(a => a.CandidateProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JobApplication>()
                .HasOne(a => a.Job)
                .WithMany(j => j.JobApplications)
                .HasForeignKey(a => a.JobId)
                .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<JobApplication>()
                .HasIndex(a => new
                {
                    a.CandidateProfileId,
                    a.JobId
                })
                .IsUnique();



            builder.Entity<Interview>()
                .HasOne(i => i.JobApplication)
                .WithOne(a => a.Interview)
                .HasForeignKey<Interview>(i => i.JobApplicationId)
                .OnDelete(DeleteBehavior.Cascade);




        }





    }
}