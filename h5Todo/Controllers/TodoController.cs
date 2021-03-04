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

namespace h5Todo.Controllers
{
    public class TodoController : Controller
    {
        private readonly TodoContext _dbContext;
        private readonly IDataProtector _provider;
        private readonly IConfiguration _config;

        public TodoController(TodoContext context, IDataProtectionProvider provider, IConfiguration config)
        {
            _dbContext = context;
            _config = config;
            _provider = provider.CreateProtector(_config["secretKey"]);
        }

        public async Task<IActionResult> Todo()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Redirect("/login/login");
            }

            await GetAllTodos(userId);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Todo(string username, string password, Todo todo, string isDone)
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(x => x.Username == username);
            var userId = HttpContext.Session.GetInt32("UserId");

            if (todo.Title == null)
            {
                ViewBag.Message = "Please provide a title";
                await GetAllTodos(userId);
                return View();
            }

            todo.IsDone = isDone == "true";

            if (todo.Description == null)
            {
                todo.Description = "";
            }

            todo.DateAdded = DateTime.UtcNow;
            if (userId != null) todo.UserId = (int) userId;

            todo.Title = _provider.Protect(todo.Title);
            todo.Description = _provider.Protect(todo.Description);

            _dbContext.Todos.Add(todo);
            _dbContext.SaveChanges();

            return View();
        }

        private async Task GetAllTodos(int? userId)
        {
            ViewBag.todos = await _dbContext.Todos.Where(x => x.UserId == userId.Value).ToListAsync();
            foreach (Todo todo in ViewBag.todos)
            {
                try
                {
                    todo.Title = _provider.Unprotect(todo.Title);
                    todo.Description = _provider.Unprotect(todo.Description);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditTodo([FromQuery]int todoId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Redirect("/login/login");
            }

            Todo todo = await _dbContext.Todos.FirstOrDefaultAsync(x => x.TodoId == todoId);

            todo.Title = _provider.Unprotect(todo.Title);
            todo.Description = _provider.Unprotect(todo.Description);

            ViewBag.Todo = todo;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int todoId, string title, string description, string isDone)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Redirect("/login/login");
            }
            
            Todo todo = await _dbContext.Todos.FirstOrDefaultAsync(x => x.TodoId == todoId);

            if (todo != null && todo.UserId == userId)
            {

                todo.IsDone = isDone == "true";

                if (!string.IsNullOrWhiteSpace(todo.Title))
                {
                    todo.Title = _provider.Protect(title);
                }
                if (!string.IsNullOrWhiteSpace(todo.Description))
                {
                    todo.Description = _provider.Protect(description);
                }

                _dbContext.Update(todo);
                _dbContext.SaveChanges();
            }

            return Redirect("/Todo/todo");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteTodo(int todoId)
        {

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Redirect("/login/login");
            }

            Todo todo = await _dbContext.Todos.FirstOrDefaultAsync(x => x.TodoId == todoId);
            if (todo != null && todo.UserId == userId)
            {
                _dbContext.Remove(todo);
                _dbContext.SaveChanges();
            }

            return Redirect("/");
        }

    }
}
