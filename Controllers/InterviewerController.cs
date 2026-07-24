using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartHire.Controllers
{
    [Authorize(Roles = "Interviewer")]
    public class InterviewerController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}