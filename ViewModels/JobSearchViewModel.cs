using SmartHire.Models;

namespace SmartHire.ViewModels
{
    public class JobSearchViewModel
    {
        public string? SearchTerm { get; set; }

        public string? Location { get; set; }

        public string? EmploymentType { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 5;

        public int TotalPages { get; set; }

        public List<Job> Jobs { get; set; } = new();

        public Dictionary<int, JobMatchViewModel> JobMatches { get; set; }
        = new Dictionary<int, JobMatchViewModel>();
    }
}