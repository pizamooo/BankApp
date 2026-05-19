using BankApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankApp.Services
{
    public static class Session
    {
        public static Client CurrentUser { get; set; }

        public static bool IsAdmin =>
            CurrentUser?.Role == "Admin";

        public static bool IsOperator =>
            CurrentUser?.Role == "Operator";

        public static bool IsClient =>
            CurrentUser?.Role == "Client";
    }
}
