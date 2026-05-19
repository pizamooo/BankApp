using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankApp.Models.Dashboard
{
    public class OperationItem
    {
        public string Title { get; set; }

        public string DateText { get; set; }

        public string AmountText { get; set; }

        public bool IsIncome { get; set; }
    }
}
