using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankApp.Models
{
    public class TopUpHistoryItem
    {
        public int Id { get; set; }
        public string Card { get; set; }
        public string Date { get; set; }
        public string Amount { get; set; }
                                              
    }
}