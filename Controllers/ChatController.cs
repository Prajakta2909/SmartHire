using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHire.Data;
using SmartHire.Models;

namespace SmartHire.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        //-----------------------------------------------------------------------------------------------------------------------



        // =========================
        // DISPLAY CONVERSATION
        // =========================

        [HttpGet]
        public async Task<IActionResult> Conversation(int applicationId)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var application = await _context.JobApplications
                .Include(a => a.Job)
                    .ThenInclude(j => j.RecruiterProfile)
                .Include(a => a.CandidateProfile)
                    .ThenInclude(c => c.ApplicationUser)
                .FirstOrDefaultAsync(a =>
                    a.Id == applicationId);

            if (application == null)
            {
                return NotFound();
            }


            var candidateUserId =
                application.CandidateProfile.ApplicationUserId;

            var recruiterUserId =
                application.Job.RecruiterProfile.ApplicationUserId;


            // Only candidate and recruiter belonging
            // to this application can access chat
            if (userId != candidateUserId &&
                userId != recruiterUserId)
            {
                return Forbid();
            }


            // Mark received messages as read
            var unreadMessages = await _context.Messages
                .Where(m =>
                    m.JobApplicationId == applicationId &&
                    m.ReceiverId == userId &&
                    !m.IsRead)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            if (unreadMessages.Any())
            {
                await _context.SaveChangesAsync();
            }


            // Load conversation
            var messages = await _context.Messages
                .Where(m =>
                    m.JobApplicationId == applicationId)
                .Include(m => m.Sender)
                .OrderBy(m => m.SentAt)
                .ToListAsync();


            ViewBag.ApplicationId = application.Id;

            ViewBag.JobTitle =
                application.Job.Title;

            ViewBag.CurrentUserId =
                userId;


            return View(messages);
        }


        // =========================
        // SEND MESSAGE
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(
            int applicationId,
            string content)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }


            // Empty message
            if (string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction(
                    nameof(Conversation),
                    new { applicationId });
            }


            content = content.Trim();


            // Maximum message length
            if (content.Length > 1000)
            {
                TempData["ErrorMessage"] =
                    "Message cannot exceed 1000 characters.";

                return RedirectToAction(
                    nameof(Conversation),
                    new { applicationId });
            }


            var application = await _context.JobApplications
                .Include(a => a.Job)
                    .ThenInclude(j => j.RecruiterProfile)
                .Include(a => a.CandidateProfile)
                .FirstOrDefaultAsync(a =>
                    a.Id == applicationId);

            if (application == null)
            {
                return NotFound();
            }


            var candidateUserId =
                application.CandidateProfile.ApplicationUserId;

            var recruiterUserId =
                application.Job.RecruiterProfile.ApplicationUserId;


            // Security check
            if (userId != candidateUserId &&
                userId != recruiterUserId)
            {
                return Forbid();
            }


            // Determine receiver
            string receiverId;

            if (userId == candidateUserId)
            {
                receiverId = recruiterUserId;
            }
            else
            {
                receiverId = candidateUserId;
            }


            // Create message
            var message = new Message
            {
                SenderId = userId,

                ReceiverId = receiverId,

                JobApplicationId = application.Id,

                Content = content,

                SentAt = DateTime.UtcNow,

                IsRead = false
            };


            _context.Messages.Add(message);

            await _context.SaveChangesAsync();


            return RedirectToAction(
                nameof(Conversation),
                new { applicationId });
        }





        [HttpGet]
        public async Task<IActionResult> Inbox()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            // Get applications where logged-in user
            // is either candidate or recruiter
            var applications = await _context.JobApplications
                .Include(a => a.Job)
                    .ThenInclude(j => j.RecruiterProfile)
                        .ThenInclude(r => r.ApplicationUser)
                .Include(a => a.CandidateProfile)
                    .ThenInclude(c => c.ApplicationUser)
                .Where(a =>
                    a.CandidateProfile.ApplicationUserId == userId ||
                    a.Job.RecruiterProfile.ApplicationUserId == userId)
                .ToListAsync();


            // Only show applications that have chat messages
            var applicationIds = await _context.Messages
                .Where(m =>
                    m.SenderId == userId ||
                    m.ReceiverId == userId)
                .Select(m => m.JobApplicationId)
                .Distinct()
                .ToListAsync();


            applications = applications
                .Where(a => applicationIds.Contains(a.Id))
                .ToList();


            // Get latest message time for sorting
            var latestMessageTimes = await _context.Messages
                .Where(m =>
                    m.SenderId == userId ||
                    m.ReceiverId == userId)
                .GroupBy(m => m.JobApplicationId)
                .Select(g => new
                {
                    ApplicationId = g.Key,
                    LastMessageTime = g.Max(m => m.SentAt)
                })
                .ToDictionaryAsync(
                    x => x.ApplicationId,
                    x => x.LastMessageTime);


            applications = applications
                .OrderByDescending(a =>
                    latestMessageTimes.ContainsKey(a.Id)
                        ? latestMessageTimes[a.Id]
                        : DateTime.MinValue)
                .ToList();


            // Unread count for each conversation
            var unreadCounts = await _context.Messages
                .Where(m =>
                    m.ReceiverId == userId &&
                    !m.IsRead)
                .GroupBy(m => m.JobApplicationId)
                .Select(g => new
                {
                    ApplicationId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(
                    x => x.ApplicationId,
                    x => x.Count);


            ViewBag.CurrentUserId = userId;
            ViewBag.UnreadCounts = unreadCounts;

            return View(applications);
        }





        // =========================
        // UNREAD MESSAGE COUNT
        // =========================

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Json(0);
            }


            var count = await _context.Messages
                .CountAsync(m =>
                    m.ReceiverId == userId &&
                    !m.IsRead);


            return Json(count);
        }
    }
}