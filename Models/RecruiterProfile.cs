using System.ComponentModel.DataAnnotations;

namespace SmartHire.Models
{
    public class RecruiterProfile
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? CompanyWebsite { get; set; }

        [StringLength(100)]
        public string? Designation { get; set; }

        [StringLength(100)]
        public string? Location { get; set; }

        [StringLength(500)]
        public string? CompanyDescription { get; set; }

        // Foreign Key
        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        // Navigation Property
        public ApplicationUser ApplicationUser { get; set; } = null!;
    }
}