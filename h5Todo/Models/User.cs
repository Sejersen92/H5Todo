using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace h5Todo.Models
{
    public class User
    {
        [Key]public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public List<Todo> Todos { get; set; }

    }
}
