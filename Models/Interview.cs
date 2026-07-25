using System.ComponentModel.DataAnnotations;

namespace SmartHire.Models
{
    public class Interview
    {
        public int Id { get; set; }

        [Required]
        public DateTime ScheduledDateTime { get; set; }

        [Required]
        [StringLength(50)]
        public string InterviewMode { get; set; } = string.Empty;

        [StringLength(500)]
        public string? MeetingLink { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [StringLength(1000)]
        public string? Instructions { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Foreign Key
        public int JobApplicationId { get; set; }

        // Navigation Property
        public JobApplication JobApplication { get; set; } = null!;
    }
}