using SmartHire.Models;
using SmartHire.ViewModels;

namespace SmartHire.Services
{
    public interface IJobMatchingService
    {
        JobMatchViewModel CalculateMatch(
            Job job,
            CandidateProfile candidate);
    }
}