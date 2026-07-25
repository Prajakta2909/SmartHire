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

        public RecruiterController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Dashboard()
        {
            return View();
        }

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



    }
}