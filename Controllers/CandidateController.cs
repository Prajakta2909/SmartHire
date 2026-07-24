using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartHire.Controllers
{
    [Authorize(Roles = "Candidate")]
    public class CandidateController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}