using System.ComponentModel.DataAnnotations;

namespace SmartHire.Models
{
    public class CandidateProfile
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Location { get; set; } = string.Empty;

        [StringLength(200)]
        public string? HighestQualification { get; set; }

        [StringLength(200)]
        public string? Specialization { get; set; }

        public int? GraduationYear { get; set; }

        [StringLength(1000)]
        public string? Skills { get; set; }

        [Range(0, 50)]
        public int ExperienceYears { get; set; }

        [StringLength(200)]
        public string? CurrentJobTitle { get; set; }

        [StringLength(500)]
        public string? LinkedInUrl { get; set; }

        [StringLength(500)]
        public string? GitHubUrl { get; set; }

        // Resume path will be added when we implement file upload
        [StringLength(500)]
        public string? ResumePath { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        public ApplicationUser ApplicationUser { get; set; } = null!;

        public ICollection<JobApplication> JobApplications { get; set; }
        = new List<JobApplication>();
    }
}