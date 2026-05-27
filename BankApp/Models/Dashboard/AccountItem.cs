using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankApp.Models.Dashboard
{
    public class AccountItem
    {
        public int Id { get; set; }
        public string Iban { get; set; }
        public decimal Balance { get; set; }
        public string Name { get; set; }
        public string BalanceText =>
            Balance.ToString("N2") + " ₽";

        public string Display =>
            $"{Iban} • {Balance:N2} ₽";
    }
}
