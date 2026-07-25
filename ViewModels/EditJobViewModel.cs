using System.ComponentModel.DataAnnotations;

namespace SmartHire.ViewModels
{
    public class EditJobViewModel
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
        [Display(Name = "Employment Type")]
        public string EmploymentType { get; set; } = string.Empty;

        [Display(Name = "Required Skills")]
        [StringLength(500)]
        public string? RequiredSkills { get; set; }

        [Display(Name = "Experience Required (Years)")]
        [Range(0, 50)]
        public int ExperienceRequired { get; set; }

        [Display(Name = "Minimum Salary")]
        [Range(0, double.MaxValue)]
        public decimal? SalaryMin { get; set; }

        [Display(Name = "Maximum Salary")]
        [Range(0, double.MaxValue)]
        public decimal? SalaryMax { get; set; }

        [Display(Name = "Application Deadline")]
        [DataType(DataType.Date)]
        public DateTime? ApplicationDeadline { get; set; }
    }
}