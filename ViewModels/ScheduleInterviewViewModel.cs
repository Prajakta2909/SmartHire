using System.ComponentModel.DataAnnotations;

namespace SmartHire.ViewModels
{
    public class ScheduleInterviewViewModel
    {
        public int JobApplicationId { get; set; }

        [Required]
        [Display(Name = "Interview Date & Time")]
        public DateTime ScheduledDateTime { get; set; }

        [Required]
        [Display(Name = "Interview Mode")]
        public string InterviewMode { get; set; } = string.Empty;

        [Display(Name = "Meeting Link")]
        [StringLength(500)]
        public string? MeetingLink { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [StringLength(1000)]
        public string? Instructions { get; set; }
    }
}