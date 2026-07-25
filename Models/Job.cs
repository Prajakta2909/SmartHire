using System.ComponentModel.DataAnnotations;

namespace SmartHire.Models
{
    public class Job
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string EmploymentType { get; set; } = string.Empty;

        [StringLength(500)]
        public string? RequiredSkills { get; set; }

        [Range(0, 50)]
        public int ExperienceRequired { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? SalaryMin { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? SalaryMax { get; set; }

        public DateTime PostedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ApplicationDeadline { get; set; }

        public bool IsActive { get; set; } = true;

        // Foreign Key
        public int RecruiterProfileId { get; set; }

        // Navigation Property
        public RecruiterProfile RecruiterProfile { get; set; } = null!;
    }
}