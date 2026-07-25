using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHire.Data;
using SmartHire.Models;

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
    }
}