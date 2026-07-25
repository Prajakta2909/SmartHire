using SmartHire.Models;

namespace SmartHire.ViewModels
{
    public class JobDetailsViewModel
    {
        public Job Job { get; set; } = null!;

        public JobMatchViewModel? Match { get; set; }

        public bool AlreadyApplied { get; set; }
    }
}