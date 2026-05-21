using System;

namespace BankApp.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public bool IsCanceled {  get; set; }
        public string AccountNumber { get; set; }
    }

    public class ChartPoint
    {
        public string Label { get; set; }   // дата (например 03.05)
        public decimal Value { get; set; }  // сумма
    }
}