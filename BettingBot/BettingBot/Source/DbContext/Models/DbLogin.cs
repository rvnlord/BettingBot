using System;
using System.Collections.Generic;
using BettingBot.Source.Converters;
using BettingBot.Source.ViewModels;

namespace BettingBot.Source.DbContext.Models
{
    [Serializable]
    public class DbLogin
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        
        public virtual ICollection<DbWebsite> Websites { get; set; } = new HashSet<DbWebsite>();

        public DbLogin()
        {
        }

        public DbLogin(int id, string login, string password)
        {
            Id = id;
            Name = login;
            Password = password;
        }

        public bool IsEmpty()
        {
            return string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Password);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DbLogin)) return false;
            var l = (DbLogin)obj;

            return Name == l.Name
                && Password == l.Password;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ 7
                   * Password.GetHashCode() ^ 11;
        }

        public LoginGvVM ToLoginGvVM()
        {
            return LoginConverter.ToLoginGvVM(this);
        }
    }
}
