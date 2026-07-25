using SmartHire.Models;
using SmartHire.ViewModels;

namespace SmartHire.Services
{
    public class JobMatchingService : IJobMatchingService
    {
        public JobMatchViewModel CalculateMatch(
            Job job,
            CandidateProfile candidate)
        {
            // Convert required skills into clean list
            var requiredSkills = SplitSkills(job.RequiredSkills);

            // Convert candidate skills into clean list
            var candidateSkills = SplitSkills(candidate.Skills);


            // Find matched skills
            var matchedSkills = requiredSkills
                .Where(required =>
                    candidateSkills.Any(candidateSkill =>
                        string.Equals(
                            candidateSkill,
                            required,
                            StringComparison.OrdinalIgnoreCase)))
                .ToList();


            // Find missing skills
            var missingSkills = requiredSkills
                .Where(required =>
                    !candidateSkills.Any(candidateSkill =>
                        string.Equals(
                            candidateSkill,
                            required,
                            StringComparison.OrdinalIgnoreCase)))
                .ToList();


            // =========================
            // SKILL SCORE
            // =========================

            int skillMatchPercentage;

            if (requiredSkills.Count == 0)
            {
                skillMatchPercentage = 100;
            }
            else
            {
                skillMatchPercentage =
                    (int)Math.Round(
                        (double)matchedSkills.Count /
                        requiredSkills.Count * 100);
            }


            // =========================
            // EXPERIENCE SCORE
            // =========================

            var candidateExperience =
                candidate.ExperienceYears;

            var requiredExperience =
                job.ExperienceRequired;


            int experienceMatchPercentage;


            if (requiredExperience <= 0)
            {
                experienceMatchPercentage = 100;
            }
            else if (candidateExperience >= requiredExperience)
            {
                experienceMatchPercentage = 100;
            }
            else
            {
                experienceMatchPercentage =
                    (int)Math.Round(
                        (double)candidateExperience /
                        requiredExperience * 100);
            }


            // =========================
            // FINAL SCORE
            // =========================
            //
            // Skills     = 70%
            // Experience = 30%

            var finalScore =
                (int)Math.Round(
                    (skillMatchPercentage * 0.70) +
                    (experienceMatchPercentage * 0.30));


            // =========================
            // MATCH LEVEL
            // =========================

            string matchLevel;

            if (finalScore >= 80)
            {
                matchLevel = "High Match";
            }
            else if (finalScore >= 50)
            {
                matchLevel = "Medium Match";
            }
            else
            {
                matchLevel = "Low Match";
            }


            return new JobMatchViewModel
            {
                JobId = job.Id,

                JobTitle = job.Title,

                MatchPercentage = finalScore,

                SkillMatchPercentage =
                    skillMatchPercentage,

                ExperienceMatchPercentage =
                    experienceMatchPercentage,

                MatchedSkills = matchedSkills,

                MissingSkills = missingSkills,

                MatchLevel = matchLevel
            };
        }


        // =========================
        // HELPER METHOD
        // =========================

        private static List<string> SplitSkills(
            string? skills)
        {
            if (string.IsNullOrWhiteSpace(skills))
            {
                return new List<string>();
            }

            return skills
                .Split(
                    new[] { ',', ';' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(skill => skill.Trim())
                .Where(skill =>
                    !string.IsNullOrWhiteSpace(skill))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}