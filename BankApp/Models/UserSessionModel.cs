using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankApp.Models
{
    public class UserSessionModel
    {
        public string DeviceName { get; set; }
        public string Location { get; set; }
        public DateTime LoginTime { get; set; }
        public bool IsCurrent { get; set; }
    }
}
