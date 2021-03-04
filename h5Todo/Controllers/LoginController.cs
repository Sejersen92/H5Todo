using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using h5Todo.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using BC = BCrypt.Net.BCrypt;

namespace h5Todo.Controllers
{
    public class LoginController : Controller
    {
        private readonly IDataProtector _provider;
        private readonly IConfiguration _config;
        private readonly TodoContext _dbContext;

        public LoginController(TodoContext context, IDataProtectionProvider provider, IConfiguration config)
        {
            _dbContext = context;
            _config = config;
            _provider = provider.CreateProtector(_config["secretKey"]);
        }

        public async Task<IActionResult> Login(string password, string username)
        {
            User user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (username == null || password == null)
            {
                ViewBag.Message = "Please fill out both fields";
                return View();
            }

            if (user == null)
            {
                ViewBag.Message = "Username or password is incorrect";
                return View();
            }
            else
            {
                try
                {
                    if (BC.Verify(password, user.Password))
                    {
                        HttpContext.Session.SetInt32("UserId", user.UserId);
                        return Redirect("/");
                    }
                    else
                    {
                        ViewBag.Message = "Username or password is incorrect";
                        return View();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    ViewBag.Message = "Please enter a valid user, or create a new one.";
                    return View();
                }

            }
        }
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<ViewResult> Register(string username, string email, string password)
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(x => x.Username == username);

            if (user != null)
            {
                ViewBag.Message = "This user already exists";
                return View();
            }

            var createdUser = new User()
            {
                Email = email,
                Password = BC.HashPassword(password),
                Username = username
            };

            await _dbContext.Users.AddAsync(createdUser);
            await _dbContext.SaveChangesAsync();

            ViewBag.Message = "Your new user has been created";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ChangePasswordPage()
        {
            var model = new User();

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Redirect("/login/login");
            }

            User user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            model.Username = user.Username;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Redirect("/login/login");
            }

            User user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user != null && user.UserId == userId)
            {

                if (!string.IsNullOrWhiteSpace(newPassword) && !string.IsNullOrWhiteSpace(confirmPassword))
                {
                    if (newPassword == confirmPassword)
                    {
                        user.Password = BC.HashPassword(confirmPassword);
                    }
                }

                _dbContext.Update(user);
                _dbContext.SaveChanges();
            }

            return Redirect("/");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Redirect("/");
        }
    }
}
