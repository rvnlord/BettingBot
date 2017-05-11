using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace WPFDemo.Models
{
    [Serializable]
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        
        public virtual ICollection<Website> Websites { get; set; }

        public User()
        {
            Websites = new HashSet<Website>();
        }

        public User(int id, string login, string password)
        {
            Id = id;
            Name = login;
            Password = password;

            Websites = new HashSet<Website>();
        }

        public bool IsEmpty()
        {
            return string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Password);
        }
    }
}
