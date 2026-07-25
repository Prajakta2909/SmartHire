using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHire.Data;
using SmartHire.Models;
using SmartHire.ViewModels;

namespace SmartHire.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        //-------------------------------------------------------------------------------------------------------------------------


        public async Task<IActionResult> Dashboard()
        {
            var candidateUsers =
                await _userManager.GetUsersInRoleAsync("Candidate");

            var recruiterUsers =
                await _userManager.GetUsersInRoleAsync("Recruiter");

            var model = new AdminDashboardViewModel
            {
                TotalUsers = await _userManager.Users.CountAsync(),

                TotalCandidates = candidateUsers.Count,

                TotalRecruiters = recruiterUsers.Count,

                TotalJobs = await _context.Jobs.CountAsync(),

                ActiveJobs = await _context.Jobs
                    .CountAsync(j => j.IsActive),

                TotalApplications = await _context.JobApplications
                    .CountAsync()
            };

            return View(model);
        }


        //-------------------------------------------------------------------------------------------------------------



        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.FirstName)
                .ToListAsync();

            var model = new List<AdminUserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                model.Add(new AdminUserViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? "",
                    Role = roles.FirstOrDefault() ?? "No Role"
                });
            }

            return View(model);
        }


        //-----------------------------------------------------------------------------------------------------------------------------



        [HttpGet]
        public async Task<IActionResult> Jobs()
        {
            var jobs = await _context.Jobs
                .Include(j => j.RecruiterProfile)
                .OrderByDescending(j => j.PostedDate)
                .ToListAsync();

            return View(jobs);
        }


        //---------------------------------------------------------------------------------------------------------------------------------



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleJobStatus(int id)
        {
            var job = await _context.Jobs
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound();
            }

            job.IsActive = !job.IsActive;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = job.IsActive
                ? "Job reopened successfully."
                : "Job closed successfully.";

            return RedirectToAction(nameof(Jobs));
        }


        //-------------------------------------------------------------------------------------------------------------------------------
    }
}