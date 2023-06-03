using FoodEntity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.Logic;

namespace Foods.Controllers
{

    [Authorize]
    public class UsersController : Controller
    {
        private readonly UserManager logic;

        public UsersController()
        {
            logic = new UserManager();
        }
        public ActionResult Index(int page = 1)
        {
            var list = logic.GetPagedNames(page);
            ViewBag.Names = list;
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ? (ActionResult)PartialView("List", list) : View("Index", list);
        }
        [HttpGet]
        public ActionResult Save(int id = 0)
        {
            var user = logic.GetUserbyID(id);
            return View(user);
        }
        [HttpPost]
        public ActionResult Save(User users)
        {
            if (ModelState.IsValid)
            {
                users = logic.Save(users);

                return View(users);
            }
            return PartialView(users);
        }
        [HttpGet]
        public ActionResult Delete(int? id = 0)
        {
            User user = logic.GetUserbyID(id.Value);
            return View("Delete", user);
        }
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var user = logic.GetUserbyID(id);
            logic.DleteUserbyID(user.Id);
            return RedirectToAction("Index");


        }

    }
}

