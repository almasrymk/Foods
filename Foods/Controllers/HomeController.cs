using FoodEntity.Models;
using Foods.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Repository.Logic;
using Repository.Security;
using System.Diagnostics;

namespace Foods.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string returnUrl)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public IActionResult Login(User user/*, string returnUrl*/)
        {
            var encryptedUserPass = EncryptString.Encrypt(user.Password ?? "");
            if (!ModelState.IsValid)
            { return View("" + user); }
            User loginInfo = new UserManager().LoginByUsernamePasswordMethod(user.FullName, encryptedUserPass);
            if (loginInfo != null)
            {
                HttpContext.Session.SetString("Name", user.FullName);
                loginInfo.SignIn(HttpContext, false);
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("" + string.Empty, "" + "Invalid UserName or password.");

                return View(user);
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}