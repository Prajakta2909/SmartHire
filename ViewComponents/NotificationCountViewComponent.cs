using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using SmartHire.Data;
using SmartHire.Models;

namespace SmartHire.ViewComponents
{
    public class NotificationCountViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationCountViewComponent(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = _userManager.GetUserId(UserClaimsPrincipal);

            if (userId == null)
            {
                return Content("");
            }

            var unreadCount = await _context.Notifications
                .CountAsync(n =>
                    n.UserId == userId &&
                    !n.IsRead);

            return View(unreadCount);
        }
    }
}