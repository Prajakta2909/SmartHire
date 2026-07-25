using System.ComponentModel.DataAnnotations;

namespace SmartHire.Models
{
    public class JobApplication
    {
        public int Id { get; set; }

        public DateTime AppliedDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Applied";

        // Candidate
        public int CandidateProfileId { get; set; }

        public CandidateProfile CandidateProfile { get; set; } = null!;

        // Job
        public int JobId { get; set; }

        public Job Job { get; set; } = null!;

        public Interview? Interview { get; set; }
    }
}