using System;

namespace BankApp.Models
{
    public class Transfer
    {
        public int Id { get; set; }
        public int FromAccountId { get; set; }
        public int ToAccountId { get; set; }

        public string FromAccountNumber { get; set; }
        public string ToAccountNumber { get; set; }
        public decimal Amount { get; set; }
        public decimal Commission { get; set; }

        public DateTime Date { get; set; }
    }
}