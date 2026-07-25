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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<RecruiterProfile>()
                .HasOne(r => r.ApplicationUser)
                .WithOne()
                .HasForeignKey<RecruiterProfile>(r => r.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        }


    }
}