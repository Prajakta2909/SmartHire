using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHire.Data;
using SmartHire.Models;
using SmartHire.ViewModels;
using SmartHire.Services;

namespace SmartHire.Controllers
{
    [Authorize(Roles = "Candidate")]
    public class CandidateController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailService _emailService;
        private readonly IJobMatchingService _jobMatchingService;

        public CandidateController(
             ApplicationDbContext context,
             UserManager<ApplicationUser> userManager,
             IWebHostEnvironment environment,
             IEmailService emailService,
             IJobMatchingService jobMatchingService)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _emailService = emailService;
            _jobMatchingService = jobMatchingService;
        }


        //----------------------------------------------------------------------------------------------------------------------
        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var candidateProfile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

            var model = new CandidateDashboardViewModel();

            if (candidateProfile != null)
            {
                model.TotalApplications = await _context.JobApplications
                    .CountAsync(a =>
                        a.CandidateProfileId == candidateProfile.Id);

                model.ShortlistedApplications = await _context.JobApplications
                    .CountAsync(a =>
                        a.CandidateProfileId == candidateProfile.Id &&
                        a.Status == "Shortlisted");

                model.InterviewsScheduled = await _context.JobApplications
                    .CountAsync(a =>
                        a.CandidateProfileId == candidateProfile.Id &&
                        a.Status == "Interview Scheduled");

                model.SelectedApplications = await _context.JobApplications
                    .CountAsync(a =>
                        a.CandidateProfileId == candidateProfile.Id &&
                        a.Status == "Selected");
            }

            return View(model);
        }


        //------------------------------------------------------------------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

            if (profile == null)
            {
                profile = new CandidateProfile();
            }

            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(CandidateProfile model)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            ModelState.Remove(nameof(CandidateProfile.ApplicationUser));
            ModelState.Remove(nameof(CandidateProfile.ApplicationUserId));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingProfile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

            if (existingProfile == null)
            {
                model.ApplicationUserId = userId;

                _context.CandidateProfiles.Add(model);
            }
            else
            {
                existingProfile.PhoneNumber = model.PhoneNumber;
                existingProfile.Location = model.Location;
                existingProfile.HighestQualification = model.HighestQualification;
                existingProfile.Specialization = model.Specialization;
                existingProfile.GraduationYear = model.GraduationYear;
                existingProfile.Skills = model.Skills;
                existingProfile.ExperienceYears = model.ExperienceYears;
                existingProfile.CurrentJobTitle = model.CurrentJobTitle;
                existingProfile.LinkedInUrl = model.LinkedInUrl;
                existingProfile.GitHubUrl = model.GitHubUrl;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Candidate profile saved successfully.";

            return RedirectToAction(nameof(Profile));
        }

        //-----------------------------------------------------------------------------------------------------------------------------


        [HttpGet]
        public IActionResult UploadResume()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadResume(ResumeUploadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

            if (profile == null)
            {
                TempData["ErrorMessage"] =
                    "Please complete your candidate profile before uploading a resume.";

                return RedirectToAction(nameof(Profile));
            }

            var file = model.Resume;

            // Maximum file size: 5 MB
            const long maxFileSize = 5 * 1024 * 1024;

            if (file.Length > maxFileSize)
            {
                ModelState.AddModelError(
                    nameof(model.Resume),
                    "Resume size cannot exceed 5 MB.");

                return View(model);
            }

            if (file.Length == 0)
            {
                ModelState.AddModelError(
                    nameof(model.Resume),
                    "The selected file is empty.");

                return View(model);
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (extension != ".pdf")
            {
                ModelState.AddModelError(
                    nameof(model.Resume),
                    "Only PDF resumes are allowed.");

                return View(model);
            }

            var uploadsFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "resumes");

            Directory.CreateDirectory(uploadsFolder);

            // Generate our own filename instead of trusting user's filename
            var uniqueFileName = $"{Guid.NewGuid()}.pdf";

            var filePath = Path.Combine(
                uploadsFolder,
                uniqueFileName);

            await using (var stream = new FileStream(
                filePath,
                FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Delete previous resume if candidate already had one
            if (!string.IsNullOrEmpty(profile.ResumePath))
            {
                var oldFileName = Path.GetFileName(profile.ResumePath);

                var oldFilePath = Path.Combine(
                    uploadsFolder,
                    oldFileName);

                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            profile.ResumePath =
                $"/uploads/resumes/{uniqueFileName}";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Resume uploaded successfully.";

            return RedirectToAction(nameof(UploadResume));
        }


        //------------------------------------------------------------------------------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> ViewResume()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

            if (profile == null || string.IsNullOrEmpty(profile.ResumePath))
            {
                return NotFound("Resume not found.");
            }

            var fileName = Path.GetFileName(profile.ResumePath);

            var filePath = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "resumes",
                fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Resume file not found.");
            }

            return PhysicalFile(filePath, "application/pdf");
        }


        //------------------------------------------------------------------------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> BrowseJobs(
            string? searchTerm,
            string? location,
            string? employmentType,
            int pageNumber = 1)
            {
            const int pageSize = 5;

            if (pageNumber < 1)
            {
                pageNumber = 1;
            }


            // Get logged-in candidate
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var candidateProfile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(c =>
                    c.ApplicationUserId == userId);


            var query = _context.Jobs
                .Include(j => j.RecruiterProfile)
                .Where(j => j.IsActive);


            // Search by title or skills
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(j =>
                    j.Title.Contains(searchTerm) ||
                    (j.RequiredSkills != null &&
                     j.RequiredSkills.Contains(searchTerm)));
            }


            // Filter by location
            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(j =>
                    j.Location.Contains(location));
            }


            // Filter by employment type
            if (!string.IsNullOrWhiteSpace(employmentType))
            {
                query = query.Where(j =>
                    j.EmploymentType == employmentType);
            }


            // Count jobs
            var totalJobs = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(
                totalJobs / (double)pageSize);


            if (totalPages > 0 &&
                pageNumber > totalPages)
            {
                pageNumber = totalPages;
            }


            // Get jobs for current page
            var jobs = await query
                .OrderByDescending(j => j.PostedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();


            // =========================
            // CALCULATE JOB MATCHES
            // =========================

            var jobMatches =
                new Dictionary<int, JobMatchViewModel>();


            if (candidateProfile != null)
            {
                foreach (var job in jobs)
                {
                    var match =
                        _jobMatchingService.CalculateMatch(
                            job,
                            candidateProfile);

                    jobMatches[job.Id] = match;
                }
            }


            // Create view model
            var model = new JobSearchViewModel
            {
                SearchTerm = searchTerm,

                Location = location,

                EmploymentType = employmentType,

                PageNumber = pageNumber,

                PageSize = pageSize,

                TotalPages = totalPages,

                Jobs = jobs,

                JobMatches = jobMatches
            };


            return View(model);
        }


        //-------------------------------------------------------------------------------------------------------------------------------------



        [HttpGet]
        public async Task<IActionResult> JobDetails(int id)
        {
            var job = await _context.Jobs
                .Include(j => j.RecruiterProfile)
                .FirstOrDefaultAsync(j =>
                    j.Id == id &&
                    j.IsActive);

            if (job == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var candidateProfile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(c =>
                    c.ApplicationUserId == userId);

            JobMatchViewModel? match = null;
            bool alreadyApplied = false;

            if (candidateProfile != null)
            {
                match = _jobMatchingService.CalculateMatch(
                    job,
                    candidateProfile);

                alreadyApplied = await _context.JobApplications
                    .AnyAsync(a =>
                        a.JobId == job.Id &&
                        a.CandidateProfileId == candidateProfile.Id);
            }

            var model = new JobDetailsViewModel
            {
                Job = job,
                Match = match,
                AlreadyApplied = alreadyApplied
            };

            return View(model);
        }



        //--------------------------------------------------------------------------------------------------------------------------------------


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyJob(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            // Find logged-in candidate profile
            var candidateProfile = await _context.CandidateProfiles
                .Include(c => c.ApplicationUser)
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

            if (candidateProfile == null)
            {
                TempData["ErrorMessage"] =
                    "Please complete your candidate profile before applying.";

                return RedirectToAction(nameof(Profile));
            }

            // Resume is required
            if (string.IsNullOrEmpty(candidateProfile.ResumePath))
            {
                TempData["ErrorMessage"] =
                    "Please upload your resume before applying for a job.";

                return RedirectToAction(nameof(UploadResume));
            }


            // Get active job + recruiter + recruiter user account
            var job = await _context.Jobs
                .Include(j => j.RecruiterProfile)
                    .ThenInclude(r => r.ApplicationUser)
                .FirstOrDefaultAsync(j =>
                    j.Id == id &&
                    j.IsActive);

            if (job == null)
            {
                return NotFound();
            }


            // Check application deadline
            if (job.ApplicationDeadline.HasValue &&
                job.ApplicationDeadline.Value.Date < DateTime.UtcNow.Date)
            {
                TempData["ErrorMessage"] =
                    "The application deadline for this job has passed.";

                return RedirectToAction(
                    nameof(JobDetails),
                    new { id });
            }


            // Prevent duplicate application
            var alreadyApplied = await _context.JobApplications
                .AnyAsync(a =>
                    a.CandidateProfileId == candidateProfile.Id &&
                    a.JobId == id);

            if (alreadyApplied)
            {
                TempData["ErrorMessage"] =
                    "You have already applied for this job.";

                return RedirectToAction(
                    nameof(JobDetails),
                    new { id });
            }


            // Create application
            var application = new JobApplication
            {
                CandidateProfileId = candidateProfile.Id,
                JobId = job.Id,
                AppliedDate = DateTime.UtcNow,
                Status = "Applied"
            };

            _context.JobApplications.Add(application);


            // Candidate name
            var candidateName =
                $"{candidateProfile.ApplicationUser.FirstName} " +
                $"{candidateProfile.ApplicationUser.LastName}";


            // Create in-app notification for recruiter
            var notification = new Notification
            {
                UserId = job.RecruiterProfile.ApplicationUserId,

                Message =
                    $"{candidateName} applied for {job.Title}.",

                IsRead = false,

                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);


            // Save application + in-app notification
            await _context.SaveChangesAsync();


            // Send email notification to recruiter
            var recruiterEmail =
                job.RecruiterProfile.ApplicationUser.Email;

            if (!string.IsNullOrWhiteSpace(recruiterEmail))
            {
                await _emailService.SendEmailAsync(
                    recruiterEmail,

                    $"New Application - {job.Title}",

                    $@"
                <h2>New Job Application</h2>

                <p>
                    <strong>{candidateName}</strong>
                    has applied for
                    <strong>{job.Title}</strong>.
                </p>

                <p>
                    Login to SmartHire to view the candidate's
                    application and resume.
                </p>

                <p>
                    Regards,<br/>
                    SmartHire Recruitment System
                </p>
            "
                );
            }


            TempData["SuccessMessage"] =
                "Application submitted successfully.";

            return RedirectToAction(
                nameof(JobDetails),
                new { id });
        }



        //---------------------------------------------------------------------------------------------------------------------------------


        [HttpGet]
        public async Task<IActionResult> MyApplications()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var candidateProfile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

            if (candidateProfile == null)
            {
                return RedirectToAction(nameof(Profile));
            }

            var applications = await _context.JobApplications
                .Include(a => a.Job)
                    .ThenInclude(j => j.RecruiterProfile)
                .Where(a => a.CandidateProfileId == candidateProfile.Id)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            return View(applications);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------


        [HttpGet]
        public async Task<IActionResult> InterviewDetails(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var candidateProfile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

            if (candidateProfile == null)
            {
                return RedirectToAction(nameof(Profile));
            }

            var application = await _context.JobApplications
                .Include(a => a.Job)
                    .ThenInclude(j => j.RecruiterProfile)
                .Include(a => a.Interview)
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.CandidateProfileId == candidateProfile.Id);

            if (application == null)
            {
                return NotFound();
            }

            if (application.Interview == null)
            {
                return NotFound("Interview has not been scheduled.");
            }

            return View(application);
        }


        //----------------------------------------------------------------------------------------------------------------------------------------------


    }
}