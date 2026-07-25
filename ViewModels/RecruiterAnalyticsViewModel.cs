namespace SmartHire.ViewModels
{
    public class RecruiterAnalyticsViewModel
    {
        // Jobs
        public int TotalJobs { get; set; }
        public int ActiveJobs { get; set; }
        public int ClosedJobs { get; set; }

        // Applications
        public int TotalApplications { get; set; }
        public int AppliedApplications { get; set; }
        public int ShortlistedApplications { get; set; }
        public int InterviewScheduledApplications { get; set; }
        public int SelectedApplications { get; set; }
        public int RejectedApplications { get; set; }

        // Conversion percentages
        public double ShortlistRate { get; set; }
        public double SelectionRate { get; set; }

        // Applications received for each job
        public List<JobApplicationStatViewModel> JobStatistics { get; set; }
            = new();
    }


    public class JobApplicationStatViewModel
    {
        public int JobId { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public int TotalApplications { get; set; }

        public int Shortlisted { get; set; }

        public int Interviews { get; set; }

        public int Selected { get; set; }

        public int Rejected { get; set; }
    }
}