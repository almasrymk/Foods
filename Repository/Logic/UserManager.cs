using FoodEntity.Models;
using Microsoft.EntityFrameworkCore;
using Repository.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using X.PagedList;

namespace Repository.Logic
{
    public class UserManager
    {
        private readonly FoodsContext db;

        public UserManager()
        {
            db = new FoodsContext(new DbContextOptions<FoodsContext>());
        }

        public User LoginByUsernamePasswordMethod(string usernameVal, string passwordVal)
        {
            User? lst;

            try
            {
                lst = db.Users.Where(e => e.FullName == usernameVal && e.Password == passwordVal).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw ;
            }
            return lst;
        }

        public User GeT(string usernameVal)
        {
            User lst;

            try
            {
                lst = db.Users.Where(e => e.FullName == usernameVal).FirstOrDefault() ?? new User();
            }
            catch (Exception ex)
            {
                throw;
            }
            return lst;
        }

        public IPagedList<User> GetAllUser(int page = 1) => db.Users.ToPagedList(page, 10);

        public IPagedList<User>? GetPagedNames(int? page)
        {
            // return a 404 if user browses to before the first page
            if (page.HasValue && page < 1)
                return null;

            // retrieve list from database/whereverand
            var listUnpaged = GetStuffFromDatabase();

            // page the list
            const int pageSize = 10;
            var listPaged = listUnpaged.ToPagedList(page ?? 1, pageSize);

            // return a 404 if user browses to pages beyond last page. special case first page if no items exist
            if (listPaged.PageNumber != 1 && page.HasValue && page > listPaged.PageCount)
                return null;

            return listPaged;
        }

        protected IEnumerable<User> GetStuffFromDatabase()
        {
            _ = new List<User>();
            List<User> users = db.Users.ToList();
            return users;
        }

        public User GetUserbyID(int Id = 0)
        {
            var users = db.Users.Find(Id);
            return users ?? new User();
        }

        public User Save(User model)
        {
            User users = new User();
            model.Password = EncryptString.Encrypt(model.Password ?? "");

            if (model.Id == 0)
            {
                db.Users.Add(model);
            }
            else
            {
                users.FullName = model.FullName;
                users.Password = EncryptString.Encrypt(model.Password ?? "");
                users.Email = model.Email;
            }

            db.SaveChanges();

            return model;
        }

        public void DleteUserbyID(int Id = 0)
        {
            var users = db.Users.Find(Id);
            db.Users.Remove(users);
            db.SaveChanges();
        }
    }
}
