using System.ComponentModel.DataAnnotations;

namespace SmartHire.Models
{
    public class Message
    {
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [Required]
        public string ReceiverId { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        // Chat belongs to a particular job application
        public int JobApplicationId { get; set; }

        public JobApplication JobApplication { get; set; } = null!;

        public ApplicationUser Sender { get; set; } = null!;

        public ApplicationUser Receiver { get; set; } = null!;
    }
}