using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFDemo.Models.Interfaces
{
    public interface ISeleniumConnectable
    {
        SeleniumDriverManager Sdm { get; set; }

        void EnsureLogin();
        void Login();
        bool IsLogged();
    }
}
