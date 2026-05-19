
namespace BankApp.Models
{
    public class Account
    {
        public int Id { get; set; }
        public int ClientId { get; set; }

        public string AccountNumber { get; set; }
        public decimal Balance { get; set; }
        public bool IsClosed { get; set; }

        // ДОБАВИМ (для UI)
        public string ClientName { get; set; }

        public override string ToString()
        {
            return AccountNumber;
        }
    }
}