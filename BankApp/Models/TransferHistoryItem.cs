using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankApp.Models
{
    public class TransferHistoryItem
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string AmountText { get; set; }

        public string DateText { get; set; }

        public bool IsIncoming { get; set; }

        public decimal Amount { get; set; }

        public string Iban { get; set; }
    }
}
