using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BankApp.Models.Dashboard
{
    public class TemplateItem
    {
        public string Name { get; set; }

        public string Icon { get; set; }

        public string Category { get; set; }

        public ICommand Command { get; set; }
    }
}
