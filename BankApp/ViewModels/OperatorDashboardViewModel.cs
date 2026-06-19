using BankApp.Data;
using BankApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankApp.ViewModels
{
    public class OperatorDashboardViewModel : BaseViewModel
    {
        public ObservableCollection<Transaction> LastTransactions { get; set; } = new ObservableCollection<Transaction>();

        public ObservableCollection<Account> Accounts { get; set; }
    = new ObservableCollection<Account>();

        private Account _selectedAccount;
        public Account SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                _selectedAccount = value;
                OnPropertyChanged();

                LoadDashboardData();
            }
        }

        public ObservableCollection<ChartPoint> IncomeChart { get; set; }
            = new ObservableCollection<ChartPoint>();

        public ObservableCollection<ChartPoint> ExpenseChart { get; set; }
            = new ObservableCollection<ChartPoint>();

        private decimal _totalBankBalance;
        public decimal TotalBankBalance
        {
            get => _totalBankBalance;
            set
            {
                _totalBankBalance = value;
                OnPropertyChanged();
            }
        }

        private decimal _totalIncome;
        public decimal TotalIncome
        {
            get => _totalIncome;
            set
            {
                _totalIncome = value;
                OnPropertyChanged();
            }
        }

        private decimal _totalExpense;
        public decimal TotalExpense
        {
            get => _totalExpense;
            set
            {
                _totalExpense = value;
                OnPropertyChanged();
            }
        }

        public OperatorDashboardViewModel()
        {
            LoadAccounts();
            LoadTotalBalance();
            if (Accounts.Any())
                SelectedAccount = Accounts.First();
        }

        private void LoadDashboardData()
        {
            if (SelectedAccount == null)
                return;

            TotalBankBalance = SelectedAccount.Balance;

            LoadTotals();
            LoadLastTransactions();
            LoadCharts();
        }

        private void LoadAccounts()
        {
            Accounts.Clear();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query = @"
        SELECT Id, AccountNumber, Balance, IsClosed
        FROM Accounts
        WHERE IsClosed = 0";

                SqlCommand cmd = new SqlCommand(query, conn);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Accounts.Add(new Account
                    {
                        Id = (int)reader["Id"],
                        AccountNumber = reader["AccountNumber"].ToString(),
                        Balance = (decimal)reader["Balance"],
                        IsClosed = (bool)reader["IsClosed"]
                    });
                }
            }
        }

        private void LoadTotals()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string incomeQuery = @"
        SELECT ISNULL(SUM(Amount),0)
        FROM Transactions
        WHERE AccountId = @accountId
        AND Category IN ('Deposit','TransferIn')";

                string expenseQuery = @"
        SELECT ISNULL(SUM(Amount),0)
        FROM Transactions
        WHERE AccountId = @accountId
        AND Category IN ('Withdraw','TransferOut')";

                SqlCommand incomeCmd = new SqlCommand(incomeQuery, conn);
                SqlCommand expenseCmd = new SqlCommand(expenseQuery, conn);

                incomeCmd.Parameters.AddWithValue("@accountId", SelectedAccount.Id);
                expenseCmd.Parameters.AddWithValue("@accountId", SelectedAccount.Id);

                TotalIncome = Convert.ToDecimal(incomeCmd.ExecuteScalar());
                TotalExpense = Convert.ToDecimal(expenseCmd.ExecuteScalar());
            }
        }

        private void LoadTotalBalance()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query = @"
            SELECT ISNULL(SUM(Balance),0)
            FROM Accounts
            WHERE IsClosed = 0";

                SqlCommand cmd = new SqlCommand(query, conn);

                TotalBankBalance = (decimal)cmd.ExecuteScalar();
            }
        }

        private void LoadLastTransactions()
        {
            LastTransactions.Clear();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query = @"
        SELECT TOP 10
            t.Id,
            t.Amount,
            t.Type,
            t.Date,
            t.Description,
            a.AccountNumber
        FROM Transactions t
        JOIN Accounts a ON a.Id = t.AccountId
        WHERE t.AccountId = @accountId
        ORDER BY t.Date DESC";

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@accountId", SelectedAccount.Id);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    LastTransactions.Add(new Transaction
                    {
                        Id = (int)reader["Id"],
                        Amount = (decimal)reader["Amount"],

                        Type = reader["Type"].ToString() == "Income"
                            ? "Пополнение"
                            : "Списание",

                        Date = (DateTime)reader["Date"],

                        AccountNumber = reader["AccountNumber"].ToString(),

                        Description = reader["Description"].ToString()
                    });
                }
            }
        }

        private void LoadCharts()
        {
            IncomeChart.Clear();
            ExpenseChart.Clear();

            var grouped = LastTransactions
                .GroupBy(x => x.Date.ToString("dd.MM"))
                .OrderBy(x => x.Key);

            foreach (var g in grouped)
            {
                IncomeChart.Add(new ChartPoint
                {
                    Label = g.Key,
                    Value = g.Where(x => x.Type == "Пополнение")
                             .Sum(x => x.Amount)
                });

                ExpenseChart.Add(new ChartPoint
                {
                    Label = g.Key,
                    Value = g.Where(x => x.Type == "Списание")
                             .Sum(x => x.Amount)
                });
            }
        }
    }
}
