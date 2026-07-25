using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHire.Data;
using SmartHire.Models;
using SmartHire.ViewModels;

namespace SmartHire.Controllers
{
    [Authorize(Roles = "Recruiter")]
    public class RecruiterController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public RecruiterController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }


        //----------------------------------------------------------------------------------------------------------------------


        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var recruiterProfile = await _context.RecruiterProfiles
                .FirstOrDefaultAsync(r =>
                    r.ApplicationUserId == userId);

            var model = new RecruiterDashboardViewModel();

            if (recruiterProfile != null)
            {
                model.TotalJobs = await _context.Jobs
                    .CountAsync(j =>
                        j.RecruiterProfileId == recruiterProfile.Id);

                model.ActiveJobs = await _context.Jobs
                    .CountAsync(j =>
                        j.RecruiterProfileId == recruiterProfile.Id &&
                        j.IsActive);

                model.TotalApplications = await _context.JobApplications
                    .CountAsync(a =>
                        a.Job.RecruiterProfileId == recruiterProfile.Id);

                model.SelectedCandidates = await _context.JobApplications
                    .CountAsync(a =>
                        a.Job.RecruiterProfileId == recruiterProfile.Id &&
                        a.Status == "Selected");
            }

            return View(model);
        }



        //--------------------------------------------------------------------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = _userManager.GetUserId(User);

            var profile = await _context.RecruiterProfiles
                .FirstOrDefaultAsync(r => r.ApplicationUserId == userId);

            if (profile == null)
            {
                profile = new RecruiterProfile();
            }

            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(RecruiterProfile model)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            ModelState.Remove(nameof(RecruiterProfile.ApplicationUser));
            ModelState.Remove(nameof(RecruiterProfile.ApplicationUserId));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingProfile = await _context.RecruiterProfiles
                .FirstOrDefaultAsync(r => r.ApplicationUserId == userId);

            if (existingProfile == null)
            {
                model.ApplicationUserId = userId;

                _context.RecruiterProfiles.Add(model);
            }
            else
            {
                existingProfile.CompanyName = model.CompanyName;
                existingProfile.CompanyWebsite = model.CompanyWebsite;
                existingProfile.Designation = model.Designation;
                existingProfile.Location = model.Location;
                existingProfile.CompanyDescription = model.CompanyDescription;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile saved successfully.";

            return RedirectToAction(nameof(Profile));
        }

        //---------------------------------------------------------------------------------------------------------------------------------------


        [HttpGet]
        public IActionResult CreateJob()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateJob(CreateJobViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validate salary range
            if (model.SalaryMin.HasValue &&
                model.SalaryMax.HasValue &&
                model.SalaryMin > model.SalaryMax)
            {
                ModelState.AddModelError(
                    nameof(model.SalaryMax),
                    "Maximum salary must be greater than or equal to minimum salary.");

                return View(model);
            }

            // Validate deadline
            if (model.ApplicationDeadline.HasValue &&
                model.ApplicationDeadline.Value.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(
                    nameof(model.ApplicationDeadline),
                    "Application deadline cannot be in the past.");

                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var recruiterProfile = await _context.RecruiterProfiles
                .FirstOrDefaultAsync(r => r.ApplicationUserId == userId);

            if (recruiterProfile == null)
            {
                TempData["ErrorMessage"] =
                    "Please complete your recruiter profile before posting a job.";

                return RedirectToAction(nameof(Profile));
            }

            var job = new Job
            {
                Title = model.Title,
                Description = model.Description,
                Location = model.Location,
                EmploymentType = model.EmploymentType,
                RequiredSkills = model.RequiredSkills,
                ExperienceRequired = model.ExperienceRequired,
                SalaryMin = model.SalaryMin,
                SalaryMax = model.SalaryMax,
                ApplicationDeadline = model.ApplicationDeadline,

                RecruiterProfileId = recruiterProfile.Id,
                PostedDate = DateTime.UtcNow,
                IsActive = true
            };

            _context.Jobs.Add(job);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Job posted successfully.";

            return RedirectToAction(nameof(Dashboard));
        }


        //----------------------------------------------------------------------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> MyJobs()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var recruiterProfile = await _context.RecruiterProfiles
                .FirstOrDefaultAsync(r => r.ApplicationUserId == userId);

            if (recruiterProfile == null)
            {
                return RedirectToAction(nameof(Profile));
            }

            var jobs = await _context.Jobs
                .Where(j => j.RecruiterProfileId == recruiterProfile.Id)
                .OrderByDescending(j => j.PostedDate)
                .ToListAsync();

            return View(jobs);
        }


        //----------------------------------------------------------------------------------------------------------------------------------------



        [HttpGet]
        public async Task<IActionResult> JobDetails(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var recruiterProfile = await _context.RecruiterProfiles
                .FirstOrDefaultAsync(r => r.ApplicationUserId == userId);

            if (recruiterProfile == null)
            {
                return RedirectToAction(nameof(Profile));
            }

            var job = await _context.Jobs
                .FirstOrDefaultAsync(j =>
                    j.Id == id &&
                    j.RecruiterProfileId == recruiterProfile.Id);

            if (job == null)
            {
                return NotFound();
            }

            return View(job);
        }


        //----------------------------------------------------------------------------------------------------------------------------------------



        [HttpGet]
        public async Task<IActionResult> EditJob(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var recruiterProfile = await _context.RecruiterProfiles
                .FirstOrDefaultAsync(r => r.ApplicationUserId == userId);

            if (recruiterProfile == null)
            {
                return RedirectToAction(nameof(Profile));
            }

            var job = await _context.Jobs
                .FirstOrDefaultAsync(j =>
                    j.Id == id &&
                    j.RecruiterProfileId == recruiterProfile.Id);

            if (job == null)
            {
                return NotFound();
            }

            var model = new EditJobViewModel
            {
                Id = job.Id,
                Title = job.Title,
                Description = job.Description,
                Location = job.Location,
                EmploymentType = job.EmploymentType,
                RequiredSkills = job.RequiredSkills,
                ExperienceRequired = job.ExperienceRequired,
                SalaryMin = job.SalaryMin,
                SalaryMax = job.SalaryMax,
                ApplicationDeadline = job.ApplicationDeadline
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditJob(EditJobViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.SalaryMin.HasValue &&
                model.SalaryMax.HasValue &&
                model.SalaryMin > model.SalaryMax)
            {
                ModelState.AddModelError(
                    nameof(model.SalaryMax),
                    "Maximum salary must be greater than or equal to minimum salary.");

                return View(model);
            }

            if (model.ApplicationDeadline.HasValue &&
                model.ApplicationDeadline.Value.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(
                    nameof(model.ApplicationDeadline),
                    "Application deadline cannot be in the past.");

                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var recruiterProfile = await _context.RecruiterProfiles
                .FirstOrDefaultAsync(r => r.ApplicationUserId == userId);

            if (recruiterProfile == null)
            {
                return RedirectToAction(nameof(Profile));
            }

            var job = await _context.Jobs
                .FirstOrDefaultAsync(j =>
                    j.Id == model.Id &&
                    j.RecruiterProfileId == recruiterProfile.Id);

            if (job == null)
            {
                return NotFound();
            }

            job.Title = model.Title;
            job.Description = model.Description;
            job.Location = model.Location;
            job.EmploymentType = model.EmploymentType;
            job.RequiredSkills = model.RequiredSkills;
            job.ExperienceRequired = model.ExperienceRequired;
            job.SalaryMin = model.SalaryMin;
            job.SalaryMax = model.SalaryMax;
            job.ApplicationDeadline = model.ApplicationDeadline;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Job updated successfully.";

            return RedirectToAction(nameof(MyJobs));
        }



        //----------------------------------------------------------------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleJobStatus(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var recruiterProfile = await _context.RecruiterProfiles
                .FirstOrDefaultAsync(r => r.ApplicationUserId == userId);

            if (recruiterProfile == null)
            {
                return RedirectToAction(nameof(Profile));
            }

            var job = await _context.Jobs
                .FirstOrDefaultAsync(j =>
                    j.Id == id &&
                    j.RecruiterProfileId == recruiterProfile.Id);

            if (job == null)
            {
                return NotFound();
            }

            job.IsActive = !job.IsActive;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = job.IsActive
                ? "Job reopened successfully."
                : "Job closed successfully.";

            return RedirectToAction(nameof(MyJobs));
        }

        //--------------------------------------------------------------------------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> JobApplications(
    int id,
    string? status)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var recruiterProfile = await _context.RecruiterProfiles
                .FirstOrDefaultAsync(r =>
                    r.ApplicationUserId == userId);

            if (recruiterProfile == null)
            {
                return RedirectToAction(nameof(Profile));
            }

            // Make sure the job belongs to logged-in recruiter
            var job = await _context.Jobs
                .FirstOrDefaultAsync(j =>
                    j.Id == id &&
                    j.RecruiterProfileId == recruiterProfile.Id);

            if (job == null)
            {
                return NotFound();
            }

            var query = _context.JobApplications
                .Include(a => a.CandidateProfile)
                    .ThenInclude(c => c.ApplicationUser)
                .Where(a => a.JobId == id);

            // Filter by application status
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(a => a.Status == status);
            }

            var applications = await query
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            ViewBag.JobTitle = job.Title;
            ViewBag.JobId = job.Id;
            ViewBag.SelectedStatus = status;

            return View(applications);
        }




        //-----------------------------------------------------------------------------------------------------------------------------------------------


        [HttpGet]
        public async Task<IActionResult> ApplicationDetails(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var recruiterProfile = await _context.RecruiterProfiles
                .FirstOrDefaultAsync(r => r.ApplicationUserId == userId);

            if (recruiterProfile == null)
            {
                return RedirectToAction(nameof(Profile));
            }

            var application = await _context.JobApplications
                .Include(a => a.CandidateProfile)
                    .ThenInclude(c => c.ApplicationUser)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.Job.RecruiterProfileId == recruiterProfile.Id);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }


        //------------------------------------------------------------------------------------------------------------------------------------------------------


        [HttpGet]
        public async Task<IActionResult> ViewCandidateResume(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var recruiterProfile = await _context.RecruiterProfiles
                .FirstOrDefaultAsync(r => r.ApplicationUserId == userId);

            if (recruiterProfile == null)
            {
                return NotFound();
            }

            var application = await _context.JobApplications
                .Include(a => a.CandidateProfile)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.Job.RecruiterProfileId == recruiterProfile.Id);

            if (application == null ||
                string.IsNullOrEmpty(application.CandidateProfile.ResumePath))
            {
                return NotFound();
            }

            var fileName =
                Path.GetFileName(application.CandidateProfile.ResumePath);

            var filePath = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "resumes",
                fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            return PhysicalFile(filePath, "application/pdf");
        }


        //----------------------------------------------------------------------------------------------------------------------------------------



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateApplicationStatus(
    int id,
    string status)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var recruiterProfile = await _context.RecruiterProfiles
                .FirstOrDefaultAsync(r => r.ApplicationUserId == userId);

            if (recruiterProfile == null)
            {
                return RedirectToAction(nameof(Profile));
            }

            var allowedStatuses = new[]
            {
        "Shortlisted",
        "Rejected",
        "Selected"
    };

            if (!allowedStatuses.Contains(status))
            {
                return BadRequest("Invalid application status.");
            }

            var application = await _context.JobApplications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.Job.RecruiterProfileId == recruiterProfile.Id);

            if (application == null)
            {
                return NotFound();
            }

            // ADD THE NEW CHECK HERE 
            if (status == "Selected" &&
                application.Status != "Interview Scheduled")
            {
                TempData["ErrorMessage"] =
                    "Candidate can be selected only after an interview is scheduled.";

                return RedirectToAction(
                    nameof(ApplicationDetails),
                    new { id = application.Id });
            }

            // Then status is updated
            application.Status = status;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Candidate marked as {status}.";

            return RedirectToAction(
                nameof(ApplicationDetails),
                new { id = application.Id });
        }




        //---------------------------------------------------------------------------------------------------------------------------------------------


        [HttpGet]
        public async Task<IActionResult> ScheduleInterview(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var recruiterProfile = await _context.RecruiterProfiles
                .FirstOrDefaultAsync(r => r.ApplicationUserId == userId);

            if (recruiterProfile == null)
            {
                return RedirectToAction(nameof(Profile));
            }

            var application = await _context.JobApplications
                .Include(a => a.Job)
                .Include(a => a.Interview)
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.Job.RecruiterProfileId == recruiterProfile.Id);

            if (application == null)
            {
                return NotFound();
            }

            // Only shortlisted candidates can be scheduled
            if (application.Status != "Shortlisted")
            {
                TempData["ErrorMessage"] =
                    "Only shortlisted candidates can be scheduled for an interview.";

                return RedirectToAction(
                    nameof(ApplicationDetails),
                    new { id });
            }

            // Prevent creating a second interview
            if (application.Interview != null)
            {
                TempData["ErrorMessage"] =
                    "An interview has already been scheduled for this candidate.";

                return RedirectToAction(
                    nameof(ApplicationDetails),
                    new { id });
            }

            var model = new ScheduleInterviewViewModel
            {
                JobApplicationId = application.Id
            };

            return View(model);
        }


        //-------------------------------------------------------------------------------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleInterview(
    ScheduleInterviewViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Interview must be scheduled in the future
            if (model.ScheduledDateTime <= DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(model.ScheduledDateTime),
                    "Interview date and time must be in the future.");

                return View(model);
            }

            // Validate interview mode
            var allowedModes = new[]
            {
        "Online",
        "In Person"
    };

            if (!allowedModes.Contains(model.InterviewMode))
            {
                ModelState.AddModelError(
                    nameof(model.InterviewMode),
                    "Please select a valid interview mode.");

                return View(model);
            }

            // Online interview requires meeting link
            if (model.InterviewMode == "Online" &&
                string.IsNullOrWhiteSpace(model.MeetingLink))
            {
                ModelState.AddModelError(
                    nameof(model.MeetingLink),
                    "Meeting link is required for an online interview.");

                return View(model);
            }

            // In-person interview requires location
            if (model.InterviewMode == "In Person" &&
                string.IsNullOrWhiteSpace(model.Location))
            {
                ModelState.AddModelError(
                    nameof(model.Location),
                    "Location is required for an in-person interview.");

                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var recruiterProfile = await _context.RecruiterProfiles
                .FirstOrDefaultAsync(r =>
                    r.ApplicationUserId == userId);

            if (recruiterProfile == null)
            {
                return RedirectToAction(nameof(Profile));
            }

            var application = await _context.JobApplications
                .Include(a => a.Job)
                .Include(a => a.Interview)
                .FirstOrDefaultAsync(a =>
                    a.Id == model.JobApplicationId &&
                    a.Job.RecruiterProfileId == recruiterProfile.Id);

            if (application == null)
            {
                return NotFound();
            }

            if (application.Status != "Shortlisted")
            {
                TempData["ErrorMessage"] =
                    "Only shortlisted candidates can be scheduled for an interview.";

                return RedirectToAction(
                    nameof(ApplicationDetails),
                    new { id = application.Id });
            }

            if (application.Interview != null)
            {
                TempData["ErrorMessage"] =
                    "An interview has already been scheduled for this candidate.";

                return RedirectToAction(
                    nameof(ApplicationDetails),
                    new { id = application.Id });
            }

            var interview = new Interview
            {
                JobApplicationId = application.Id,
                ScheduledDateTime = model.ScheduledDateTime,
                InterviewMode = model.InterviewMode,

                MeetingLink = model.InterviewMode == "Online"
                    ? model.MeetingLink
                    : null,

                Location = model.InterviewMode == "In Person"
                    ? model.Location
                    : null,

                Instructions = model.Instructions,
                CreatedDate = DateTime.UtcNow
            };

            _context.Interviews.Add(interview);

            // Candidate will now see this status
            application.Status = "Interview Scheduled";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Interview scheduled successfully.";

            return RedirectToAction(
                nameof(ApplicationDetails),
                new { id = application.Id });
        }


        //------------------------------------------------------------------------------------------------------------------------------




        [HttpGet]
        public async Task<IActionResult> Interviews()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var recruiterProfile = await _context.RecruiterProfiles
                .FirstOrDefaultAsync(r =>
                    r.ApplicationUserId == userId);

            if (recruiterProfile == null)
            {
                return RedirectToAction(nameof(Profile));
            }

            var interviews = await _context.Interviews
                .Include(i => i.JobApplication)
                    .ThenInclude(a => a.Job)
                .Include(i => i.JobApplication)
                    .ThenInclude(a => a.CandidateProfile)
                        .ThenInclude(c => c.ApplicationUser)
                .Where(i =>
                    i.JobApplication.Job.RecruiterProfileId
                        == recruiterProfile.Id)
                .OrderBy(i => i.ScheduledDateTime)
                .ToListAsync();

            return View(interviews);
        }

        //-------------------------------------------------------------------------------------------------------------------------------


        [HttpGet]
        public async Task<IActionResult> RescheduleInterview(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var recruiterProfile = await _context.RecruiterProfiles
                .FirstOrDefaultAsync(r => r.ApplicationUserId == userId);

            if (recruiterProfile == null)
            {
                return RedirectToAction(nameof(Profile));
            }

            var interview = await _context.Interviews
                .Include(i => i.JobApplication)
                    .ThenInclude(a => a.Job)
                .FirstOrDefaultAsync(i =>
                    i.Id == id &&
                    i.JobApplication.Job.RecruiterProfileId == recruiterProfile.Id);

            if (interview == null)
            {
                return NotFound();
            }

            var model = new ScheduleInterviewViewModel
            {
                JobApplicationId = interview.JobApplicationId,
                ScheduledDateTime = interview.ScheduledDateTime,
                InterviewMode = interview.InterviewMode,
                MeetingLink = interview.MeetingLink,
                Location = interview.Location,
                Instructions = interview.Instructions
            };

            ViewBag.InterviewId = interview.Id;

            return View(model);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RescheduleInterview(
    int interviewId,
    ScheduleInterviewViewModel model)
        {
            if (model.ScheduledDateTime <= DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(model.ScheduledDateTime),
                    "Interview date and time must be in the future.");
            }

            var allowedModes = new[]
            {
        "Online",
        "In Person"
    };

            if (!allowedModes.Contains(model.InterviewMode))
            {
                ModelState.AddModelError(
                    nameof(model.InterviewMode),
                    "Please select a valid interview mode.");
            }

            if (model.InterviewMode == "Online" &&
                string.IsNullOrWhiteSpace(model.MeetingLink))
            {
                ModelState.AddModelError(
                    nameof(model.MeetingLink),
                    "Meeting link is required for an online interview.");
            }

            if (model.InterviewMode == "In Person" &&
                string.IsNullOrWhiteSpace(model.Location))
            {
                ModelState.AddModelError(
                    nameof(model.Location),
                    "Location is required for an in-person interview.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.InterviewId = interviewId;
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var recruiterProfile = await _context.RecruiterProfiles
                .FirstOrDefaultAsync(r =>
                    r.ApplicationUserId == userId);

            if (recruiterProfile == null)
            {
                return RedirectToAction(nameof(Profile));
            }

            var interview = await _context.Interviews
                .Include(i => i.JobApplication)
                    .ThenInclude(a => a.Job)
                .FirstOrDefaultAsync(i =>
                    i.Id == interviewId &&
                    i.JobApplication.Job.RecruiterProfileId
                        == recruiterProfile.Id);

            if (interview == null)
            {
                return NotFound();
            }

            interview.ScheduledDateTime = model.ScheduledDateTime;
            interview.InterviewMode = model.InterviewMode;

            interview.MeetingLink =
                model.InterviewMode == "Online"
                    ? model.MeetingLink
                    : null;

            interview.Location =
                model.InterviewMode == "In Person"
                    ? model.Location
                    : null;

            interview.Instructions = model.Instructions;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Interview rescheduled successfully.";

            return RedirectToAction(nameof(Interviews));
        }

        //----------------------------------------------------------------------------------------------------------------------------



    }
}