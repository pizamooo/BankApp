using BankApp.Data;
using BankApp.Models.Dashboard;
using BankApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Collections.Generic;
using BankApp.Models;
using BankApp.Views;

namespace BankApp.ViewModels
{
    public class ClientDashboardViewModel : BaseViewModel
    {
        private string _selectedPeriod = "За год";

        public string SelectedPeriod
        {
            get => _selectedPeriod;
            set
            {
                _selectedPeriod = value;

                OnPropertyChanged();

                LoadChart();
            }
        }

        private AccountItem _selectedAccount;

        public AccountItem SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                _selectedAccount = value;

                OnPropertyChanged();

                if (value != null)
                {
                    SelectedIban = value.Iban;
                    SelectedBalance = value.BalanceText;
                }
            }
        }

        private string _selectedIban;

        public string SelectedIban
        {
            get => _selectedIban;
            set
            {
                _selectedIban = value;
                OnPropertyChanged();
            }
        }

        private string _selectedBalance;

        public string SelectedBalance
        {
            get => _selectedBalance;
            set
            {
                _selectedBalance = value;
                OnPropertyChanged();
            }
        }

        public List<string> Periods { get; set; } =
            new List<string>
        {
    "За неделю",
    "За месяц",
    "За год"
        };

        private decimal _balance;

        public decimal Balance
        {
            get { return _balance; }
            set
            {
                _balance = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BalanceText));
            }
        }

        public string UserName
        {
            get { return Session.CurrentUser.FullName; }
        }

        public string BalanceText
        {
            get { return Balance.ToString("N0") + " ₽"; }
        }

        public string IBAN
        {
            get { return "RU" + Session.CurrentUser.Id.ToString("0000000000000000"); }
        }

        public ObservableCollection<OperationItem> LastOperations { get; set; }
        public ObservableCollection<TemplateItem> Templates { get; set; }
        public ObservableCollection<ChartBarItem> ChartBars { get; set; }
        public static ClientDashboardViewModel Instance { get; private set; }
        public ObservableCollection<AccountItem> Accounts { get; set; }


        private decimal _totalIncome;
        public decimal TotalIncome
        {
            get => _totalIncome;
            set
            {
                _totalIncome = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalIncomeText));
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
                OnPropertyChanged(nameof(TotalExpenseText));
            }
        }

        public string TotalIncomeText =>
            "+" + TotalIncome.ToString("N0") + " ₽";

        public string TotalExpenseText =>
            "-" + TotalExpense.ToString("N0") + " ₽";

        public string ChartTitle
        {
            get { return "Финансы за " + DateTime.Now.Year; }
        }

        public ClientDashboardViewModel()
        {
            Instance = this;
            Accounts = new ObservableCollection<AccountItem>();
            LastOperations = new ObservableCollection<OperationItem>();
            Templates = new ObservableCollection<TemplateItem>();
            ChartBars = new ObservableCollection<ChartBarItem>();

            LoadData();
        }

        public void UpdateBalance()
        {
            OnPropertyChanged(nameof(SelectedBalance));
            OnPropertyChanged(nameof(SelectedIban));
        }

        public void Refresh()
        {
            LoadData();

            OnPropertyChanged(nameof(Balance));
            OnPropertyChanged(nameof(BalanceText));
            OnPropertyChanged(nameof(LastOperations));
            OnPropertyChanged(nameof(ChartBars));
        }

        private void LoadData()
        {
            LoadBalance();
            LoadAccounts();
            LoadLastOperations();
            LoadTemplates();
            LoadChart();
        }

        private void LoadAccounts()
        {
            Accounts.Clear();

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
SELECT
    Id,
    Iban,
    Balance
FROM Accounts
WHERE ClientId = @id
AND IsClosed = 0", conn);

                cmd.Parameters.AddWithValue(
                    "@id",
                    Session.CurrentUser.Id);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Accounts.Add(new AccountItem
                    {
                        Id = (int)reader["Id"],
                        Iban = reader["Iban"].ToString(),
                        Balance = Convert.ToDecimal(reader["Balance"])
                    });
                }
            }

            if (Accounts.Count > 0)
            {
                SelectedAccount = Accounts[0];
            }
        }

        // =========================
        // CHART
        // =========================
        private void LoadChart()
        {
            string dateFilter = "";

            if (SelectedPeriod == "За неделю")
            {
                dateFilter = "AND Date >= DATEADD(day, -7, GETDATE())";
            }
            else if (SelectedPeriod == "За месяц")
            {
                dateFilter = "AND Date >= DATEADD(month, -1, GETDATE())";
            }
            else
            {
                dateFilter = "AND YEAR(Date) = YEAR(GETDATE())";
            }

            ChartBars.Clear();

            // СБРОС
            TotalIncome = 0;
            TotalExpense = 0;

            SqlConnection conn = DatabaseHelper.GetConnection();

            try
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand($@"
SELECT 
    MONTH(Date) AS MonthNumber,
    DATENAME(MONTH, Date) AS Month,

    SUM(CASE WHEN Category IN ('Deposit','TransferIn') 
             THEN Amount ELSE 0 END) AS Income,

    SUM(CASE WHEN Category IN ('TransferOut','Withdraw','Fee') 
             THEN Amount ELSE 0 END) AS Expense

FROM Transactions t
INNER JOIN Accounts a ON t.AccountId = a.Id

WHERE a.ClientId = @id
{dateFilter}

GROUP BY MONTH(Date), DATENAME(MONTH, Date)
ORDER BY MonthNumber", conn);

                cmd.Parameters.AddWithValue("@id", Session.CurrentUser.Id);

                SqlDataReader reader = cmd.ExecuteReader();

                List<Tuple<string, double, double>> raw =
                    new List<Tuple<string, double, double>>();

                double max = 0;

                while (reader.Read())
                {
                    double income = reader["Income"] != DBNull.Value
                        ? Convert.ToDouble(reader["Income"])
                        : 0;

                    double expense = reader["Expense"] != DBNull.Value
                        ? Convert.ToDouble(reader["Expense"])
                        : 0;

                    // ТЕПЕРЬ БУДЕТ ПРАВИЛЬНО
                    TotalIncome += Convert.ToDecimal(income);
                    TotalExpense += Convert.ToDecimal(expense);

                    string month = reader["Month"].ToString();

                    raw.Add(new Tuple<string, double, double>(
                        month,
                        income,
                        expense));

                    if (income > max) max = income;
                    if (expense > max) max = expense;
                }

                reader.Close();

                foreach (var r in raw)
                {
                    ChartBars.Add(new ChartBarItem
                    {
                        Month = r.Item1.Substring(0, 3),

                        IncomeHeight =
                            max == 0 ? 0 : (r.Item2 / max) * 120,

                        ExpenseHeight =
                            max == 0 ? 0 : (r.Item3 / max) * 120
                    });
                }
            }
            finally
            {
                conn.Close();
            }

            OnPropertyChanged(nameof(ChartBars));
            OnPropertyChanged(nameof(TotalIncomeText));
            OnPropertyChanged(nameof(TotalExpenseText));
        }

        // =========================
        // BALANCE
        // =========================
        private void LoadBalance()
        {
            SqlConnection conn = DatabaseHelper.GetConnection();

            try
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
SELECT ISNULL(SUM(Balance),0)
FROM Accounts
WHERE ClientId=@id
AND IsClosed = 0", conn);

                cmd.Parameters.AddWithValue("@id", Session.CurrentUser.Id);

                object result = cmd.ExecuteScalar();

                Balance = (result == null || result == DBNull.Value)
                    ? 0
                    : Convert.ToDecimal(result);
            }
            finally
            {
                conn.Close();
            }
        }

        // =========================
        // LAST OPERATIONS
        // =========================
        private void LoadLastOperations()
        {
            LastOperations.Clear();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand(@"
SELECT TOP 10
    t.Description,
    t.Amount,
    t.Date,
    t.Type,
    t.Category
FROM Transactions t
WHERE t.AccountId IN (
    SELECT Id FROM Accounts WHERE ClientId = @id
)
ORDER BY t.Date DESC", conn);

                cmd.Parameters.AddWithValue("@id", Session.CurrentUser.Id);

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    decimal amount = reader["Amount"] != DBNull.Value
                        ? Convert.ToDecimal(reader["Amount"])
                        : 0;

                    string category = reader["Category"]?.ToString();
                    string desc = reader["Description"]?.ToString();

                    DateTime date = Convert.ToDateTime(reader["Date"]);

                    string title;

                    if (category == "TransferIn")
                    {
                        title = "📥 От: " + ExtractName(desc);
                    }
                    else if (category == "TransferOut")
                    {
                        title = "📤 Кому: " + ExtractName(desc);
                    }
                    else
                    {
                        title = desc ?? "Операция";
                    }

                    bool isIncome = category == "Deposit" || category == "TransferIn";

                    LastOperations.Add(new OperationItem
                    {
                        Title = title,
                        DateText = date.ToString("dd.MM.yyyy HH:mm"),
                        AmountText = (isIncome ? "+" : "-") + amount.ToString("N2") + " ₽",
                        IsIncome = isIncome
                    });
                }
            }
        }

        private string ExtractName(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "неизвестно";

            return text.Split('(')[0].Trim();
        }

        // =========================
        // TEMPLATES
        // =========================
        private void LoadTemplates()
        {
            Templates.Clear();

            Templates.Add(new TemplateItem
            {
                Name = "Мобильная связь",
                Icon = "📱",
                Category = "Mobile",
                Command = new RelayCommand(() =>
                {
                    OpenTemplatePayment(new TemplateItem
                    {
                        Name = "Мобильная связь",
                        Icon = "📱",
                        Category = "Mobile"
                    });
                })
            });

            Templates.Add(new TemplateItem
            {
                Name = "Интернет",
                Icon = "🌐",
                Category = "Internet",
                Command = new RelayCommand(() =>
                {
                    OpenTemplatePayment(new TemplateItem
                    {
                        Name = "Интернет",
                        Icon = "🌐",
                        Category = "Internet",
                    });
                })
            });

            Templates.Add(new TemplateItem
            {
                Name = "Коммунальные услуги",
                Icon = "🏠",
                Category = "Housing",
                Command = new RelayCommand(() =>
                {
                    OpenTemplatePayment(new TemplateItem
                    {
                        Name = "Коммунальные услуги",
                        Icon = "🏠",
                        Category = "Housing",
                    });
                })
            });
        }

        private void OpenTemplatePayment(TemplateItem template)
        {
            var window = new TemplatePaymentWindow(template);

            window.ShowDialog();

            Refresh();
        }
    }
}