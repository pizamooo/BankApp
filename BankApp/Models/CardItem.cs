using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankApp.Models
{
    public class CardItem
    {
        public int Id { get; set; }

        public int AccountId { get; set; }

        public string CardNumber { get; set; }

        public string ExpiryDate { get; set; }

        public string CVV { get; set; }

        public bool IsActive { get; set; }

        public string MaskedCard
        {
            get
            {
                if (string.IsNullOrEmpty(CardNumber) || CardNumber.Length < 4)
                    return "**** **** **** ****";

                return $"**** **** **** {CardNumber.Substring(CardNumber.Length - 4)}";
            }
        }
    }
}
