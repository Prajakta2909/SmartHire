namespace SmartHire.ViewModels
{
    public class JobMatchViewModel
    {
        public int JobId { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public int MatchPercentage { get; set; }

        public int SkillMatchPercentage { get; set; }

        public int ExperienceMatchPercentage { get; set; }

        public List<string> MatchedSkills { get; set; } = new();

        public List<string> MissingSkills { get; set; } = new();

        public string MatchLevel { get; set; } = string.Empty;
    }
}