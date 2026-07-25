using System.ComponentModel.DataAnnotations;

namespace SmartHire.ViewModels
{
    public class ResumeUploadViewModel
    {
        [Required(ErrorMessage = "Please select a resume.")]
        [Display(Name = "Resume")]
        public IFormFile Resume { get; set; } = null!;
    }
}