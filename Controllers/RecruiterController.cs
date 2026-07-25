using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHire.Data;
using SmartHire.Models;
using SmartHire.Services;
using SmartHire.ViewModels;
using SmartHire.Services;

namespace SmartHire.Controllers
{
    [Authorize(Roles = "Recruiter")]
    public class RecruiterController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailService _emailService;
        private readonly IJobMatchingService _jobMatchingService;

        public RecruiterController(
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
            // Check ViewModel validation
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // Validate salary range
            if (model.SalaryMin.HasValue &&
                model.SalaryMax.HasValue &&
                model.SalaryMin.Value > model.SalaryMax.Value)
            {
                ModelState.AddModelError(
                    nameof(model.SalaryMax),
                    "Maximum salary must be greater than or equal to minimum salary.");

                return View(model);
            }


            // Validate application deadline
            if (model.ApplicationDeadline.HasValue &&
                model.ApplicationDeadline.Value.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(
                    nameof(model.ApplicationDeadline),
                    "Application deadline cannot be in the past.");

                return View(model);
            }


            // Get logged-in recruiter
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }


            // Get recruiter profile
            var recruiterProfile = await _context.RecruiterProfiles
                .FirstOrDefaultAsync(r =>
                    r.ApplicationUserId == userId);

            if (recruiterProfile == null)
            {
                TempData["ErrorMessage"] =
                    "Please complete your recruiter profile before posting a job.";

                return RedirectToAction(nameof(Profile));
            }


            // Create new job
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


            // Save job
            _context.Jobs.Add(job);

            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Job posted successfully.";


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
                .FirstOrDefaultAsync(r =>
                    r.ApplicationUserId == userId);

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


            // Calculate candidate-job match
            var match = _jobMatchingService.CalculateMatch(
                application.Job,
                application.CandidateProfile);


            // Send match information to the view
            ViewBag.JobMatch = match;


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

            // Load Job + Candidate + Candidate User
            var application = await _context.JobApplications
                .Include(a => a.Job)
                .Include(a => a.CandidateProfile)
                    .ThenInclude(c => c.ApplicationUser)
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.Job.RecruiterProfileId == recruiterProfile.Id);

            if (application == null)
            {
                return NotFound();
            }

            // Candidate can be selected only after interview
            if (status == "Selected" &&
                application.Status != "Interview Scheduled")
            {
                TempData["ErrorMessage"] =
                    "Candidate can be selected only after an interview is scheduled.";

                return RedirectToAction(
                    nameof(ApplicationDetails),
                    new { id = application.Id });
            }

            // Update status
            application.Status = status;


            // Prepare notification message
            string notificationMessage;

            if (status == "Shortlisted")
            {
                notificationMessage =
                    $"You have been shortlisted for {application.Job.Title}.";
            }
            else if (status == "Selected")
            {
                notificationMessage =
                    $"Congratulations! You have been selected for {application.Job.Title}.";
            }
            else
            {
                notificationMessage =
                    $"Your application for {application.Job.Title} has been rejected.";
            }


            // Create in-app notification
            var notification = new Notification
            {
                UserId = application.CandidateProfile.ApplicationUserId,
                Message = notificationMessage,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);


            // Save status + notification
            await _context.SaveChangesAsync();


            // Get candidate email
            var candidateEmail =
                application.CandidateProfile.ApplicationUser.Email;

            if (!string.IsNullOrWhiteSpace(candidateEmail))
            {
                string emailSubject;

                if (status == "Shortlisted")
                {
                    emailSubject =
                        $"Application Shortlisted - {application.Job.Title}";
                }
                else if (status == "Selected")
                {
                    emailSubject =
                        $"Congratulations! Selected - {application.Job.Title}";
                }
                else
                {
                    emailSubject =
                        $"Application Update - {application.Job.Title}";
                }


                // Send email
                await _emailService.SendEmailAsync(
                    candidateEmail,
                    emailSubject,
                    $@"
                <h2>SmartHire Application Update</h2>

                <p>{notificationMessage}</p>

                <p>
                    Login to SmartHire to view your
                    application details.
                </p>

                <p>
                    Regards,<br/>
                    SmartHire Recruitment System
                </p>
            "
                );
            }


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


            // Get application + job + interview + candidate + candidate user
            var application = await _context.JobApplications
                .Include(a => a.Job)
                .Include(a => a.Interview)
                .Include(a => a.CandidateProfile)
                    .ThenInclude(c => c.ApplicationUser)
                .FirstOrDefaultAsync(a =>
                    a.Id == model.JobApplicationId &&
                    a.Job.RecruiterProfileId == recruiterProfile.Id);

            if (application == null)
            {
                return NotFound();
            }


            // Only shortlisted candidates can have interview
            if (application.Status != "Shortlisted")
            {
                TempData["ErrorMessage"] =
                    "Only shortlisted candidates can be scheduled for an interview.";

                return RedirectToAction(
                    nameof(ApplicationDetails),
                    new { id = application.Id });
            }


            // Prevent duplicate interview
            if (application.Interview != null)
            {
                TempData["ErrorMessage"] =
                    "An interview has already been scheduled for this candidate.";

                return RedirectToAction(
                    nameof(ApplicationDetails),
                    new { id = application.Id });
            }


            // Create interview
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


            // Update application status
            application.Status = "Interview Scheduled";


            // Create in-app notification for candidate
            var notification = new Notification
            {
                UserId = application.CandidateProfile.ApplicationUserId,

                Message =
                    $"Your interview for {application.Job.Title} " +
                    $"has been scheduled for " +
                    $"{model.ScheduledDateTime:dd MMM yyyy hh:mm tt}.",

                IsRead = false,

                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);


            // Save interview + status + notification
            await _context.SaveChangesAsync();


            // Send email to candidate
            var candidateEmail =
                application.CandidateProfile.ApplicationUser.Email;

            if (!string.IsNullOrWhiteSpace(candidateEmail))
            {
                await _emailService.SendEmailAsync(
                    candidateEmail,

                    $"Interview Scheduled - {application.Job.Title}",

                    $@"
                <h2>Interview Scheduled</h2>

                <p>
                    Your interview for
                    <strong>{application.Job.Title}</strong>
                    has been scheduled.
                </p>

                <p>
                    <strong>Date & Time:</strong>
                    {model.ScheduledDateTime:dd MMM yyyy hh:mm tt}
                </p>

                <p>
                    <strong>Mode:</strong>
                    {model.InterviewMode}
                </p>

                {(model.InterviewMode == "Online"
                            ? $"<p><strong>Meeting Link:</strong> {model.MeetingLink}</p>"
                            : $"<p><strong>Location:</strong> {model.Location}</p>")}

                <p>
                    Login to SmartHire for complete interview details.
                </p>

                <p>
                    Regards,<br/>
                    SmartHire Recruitment System
                </p>
            "
                );
            }


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
            // Interview must be in the future
            if (model.ScheduledDateTime <= DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(model.ScheduledDateTime),
                    "Interview date and time must be in the future.");
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
            }

            // Online interview requires meeting link
            if (model.InterviewMode == "Online" &&
                string.IsNullOrWhiteSpace(model.MeetingLink))
            {
                ModelState.AddModelError(
                    nameof(model.MeetingLink),
                    "Meeting link is required for an online interview.");
            }

            // In-person interview requires location
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

            // Get logged-in recruiter
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

            // Get interview + job + candidate + candidate user
            var interview = await _context.Interviews
                .Include(i => i.JobApplication)
                    .ThenInclude(a => a.Job)
                .Include(i => i.JobApplication)
                    .ThenInclude(a => a.CandidateProfile)
                        .ThenInclude(c => c.ApplicationUser)
                .FirstOrDefaultAsync(i =>
                    i.Id == interviewId &&
                    i.JobApplication.Job.RecruiterProfileId
                        == recruiterProfile.Id);

            if (interview == null)
            {
                return NotFound();
            }

            // Update interview
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

            // Create in-app notification for candidate
            var notification = new Notification
            {
                UserId =
                    interview.JobApplication
                        .CandidateProfile
                        .ApplicationUserId,

                Message =
                    $"Your interview for " +
                    $"{interview.JobApplication.Job.Title} " +
                    $"has been rescheduled to " +
                    $"{model.ScheduledDateTime:dd MMM yyyy hh:mm tt}.",

                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);

            // Save interview changes + notification
            await _context.SaveChangesAsync();

            // Send email to candidate
            var candidateEmail =
                interview.JobApplication
                    .CandidateProfile
                    .ApplicationUser.Email;

            if (!string.IsNullOrWhiteSpace(candidateEmail))
            {
                await _emailService.SendEmailAsync(
                    candidateEmail,

                    $"Interview Rescheduled - {interview.JobApplication.Job.Title}",

                    $@"
                <h2>Interview Rescheduled</h2>

                <p>
                    Your interview for
                    <strong>{interview.JobApplication.Job.Title}</strong>
                    has been rescheduled.
                </p>

                <p>
                    <strong>New Date & Time:</strong>
                    {model.ScheduledDateTime:dd MMM yyyy hh:mm tt}
                </p>

                <p>
                    <strong>Mode:</strong>
                    {model.InterviewMode}
                </p>

                {(model.InterviewMode == "Online"
                            ? $"<p><strong>Meeting Link:</strong> {model.MeetingLink}</p>"
                            : $"<p><strong>Location:</strong> {model.Location}</p>")}

                <p>
                    Login to SmartHire for complete interview details.
                </p>

                <p>
                    Regards,<br/>
                    SmartHire Recruitment System
                </p>
            "
                );
            }

            TempData["SuccessMessage"] =
                "Interview rescheduled successfully.";

            return RedirectToAction(nameof(Interviews));
        }






        //----------------------------------------------------------------------------------------------------------------------------



    }
}