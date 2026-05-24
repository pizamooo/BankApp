using BankApp.Data;
using BankApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Threading;

namespace BankApp.ViewModels
{
    public class AdminDashboardViewModel : BaseViewModel
    {
        private List<SystemLog> _cacheLogs = new List<SystemLog>();
        public ObservableCollection<SystemLog> Logs { get; set; }
        public ObservableCollection<string> LogActions { get; set; }

        private string _selectedLogAction;

        public string SelectedLogAction
        {
            get => _selectedLogAction;
            set
            {
                _selectedLogAction = value;
                OnPropertyChanged();

                FilterLogs();
            }
        }
        public int UsersCount
        {
            get => _usersCount;
            set
            {
                _usersCount = value;
                OnPropertyChanged();
            }
        }

        private string _logSearch;

        public string LogSearch
        {
            get => _logSearch;
            set
            {
                _logSearch = value;
                OnPropertyChanged();

                FilterLogs();
            }
        }

        public int OperatorsCount
        {
            get => _operatorsCount;
            set
            {
                _operatorsCount = value;
                OnPropertyChanged();
            }
        }

        public int ActiveAccountsCount
        {
            get => _activeAccountsCount;
            set
            {
                _activeAccountsCount = value;
                OnPropertyChanged();
            }
        }

        private int _activeAccountsCount;

        public int ClosedAccountsCount
        {
            get => _closedAccountsCount;
            set
            {
                _closedAccountsCount = value;
                OnPropertyChanged();
            }
        }

        private int _closedAccountsCount;

        private int _operatorsCount;

        private int _usersCount;

        public int AccountsCount
        {
            get => _accountsCount;
            set
            {
                _accountsCount = value;
                OnPropertyChanged();
            }
        }

        private int _accountsCount;

        public decimal TotalBankBalance
        {
            get => _totalBankBalance;
            set
            {
                _totalBankBalance = value;
                OnPropertyChanged();
            }
        }

        private decimal _totalBankBalance;

        public ObservableCollection<Client> LastUsers { get; set; }

        public AdminDashboardViewModel()
        {
            Logs = new ObservableCollection<SystemLog>();
            _cacheLogs = new List<SystemLog>();
            LogActions = new ObservableCollection<string>
{
    "Все действия",
    "Создание операции",
    "Отмена операции",
    "Открытие счета",
    "Закрытие счета",
    "Закрытие всех счетов",
    "Экспорт в PDF",
    "Экспорт в Excel",
    "Блокировка пользователя",
    "Разблокировка пользователя",
    "Смена роли"
};

            SelectedLogAction = "Все действия";
            LoadLogs();
            LastUsers = new ObservableCollection<Client>();

            LoadStatistics();
            LoadLastUsers();
            StartAutoRefresh();
        }

        private void FilterLogs()
        {
            if (_cacheLogs == null)
                return;

            var query = _cacheLogs.AsEnumerable();

            if (SelectedLogAction != "Все действия")
            {
                query = query.Where(x =>
                    x.Action == SelectedLogAction);
            }

            if (!string.IsNullOrWhiteSpace(LogSearch))
            {
                string search = LogSearch.Trim().ToLower();

                query = query.Where(x =>
                    (x.FullName?.ToLower().Contains(search) ?? false) ||
                    (x.Action?.ToLower().Contains(search) ?? false) ||
                    (x.Description?.ToLower().Contains(search) ?? false));
            }

            Logs.Clear();

            foreach (var log in query)
                Logs.Add(log);


            OnPropertyChanged(nameof(Logs));
        }

        private void StartAutoRefresh()
        {
            DispatcherTimer timer = new DispatcherTimer();

            timer.Interval = TimeSpan.FromSeconds(5);

            timer.Tick += (s, e) =>
            {
                LoadLogs();
                LoadStatistics();
            };

            timer.Start();
        }

        private void LoadLogs()
        {
            Logs.Clear();
            _cacheLogs.Clear();

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
SELECT TOP 20
    sl.Id,
    sl.UserId,
    c.FullName,
    sl.Action,
    sl.Description,
    sl.CreatedAt
FROM SystemLogs sl
LEFT JOIN Clients c
    ON sl.UserId = c.Id
ORDER BY sl.Id DESC", conn);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var log = new SystemLog
                    {
                        Id = (int)reader["Id"],
                        UserId = reader["UserId"] == DBNull.Value
                            ? null
                            : (int?)reader["UserId"],
                        FullName = reader["FullName"] == DBNull.Value
                            ? "Система"
                            : reader["FullName"].ToString(),
                        Action = reader["Action"].ToString(),
                        Description = reader["Description"].ToString(),
                        CreatedAt = (DateTime)reader["CreatedAt"]
                    };

                    _cacheLogs.Add(log);
                }
            }

            FilterLogs();
        }

        private void LoadStatistics()
        {

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                // USERS
                SqlCommand usersCmd =
                    new SqlCommand(
                        "SELECT COUNT(*) FROM Clients",
                        conn);

                UsersCount =
                    (int)usersCmd.ExecuteScalar();

                // ACCOUNTS
                SqlCommand accountsCmd =
                    new SqlCommand(
                        "SELECT COUNT(*) FROM Accounts",
                        conn);

                AccountsCount =
                    (int)accountsCmd.ExecuteScalar();

                // MONEY
                SqlCommand moneyCmd =
                    new SqlCommand(
                        "SELECT ISNULL(SUM(Balance),0) FROM Accounts WHERE IsClosed = 0",
                        conn);

                TotalBankBalance =
                    (decimal)moneyCmd.ExecuteScalar();

                SqlCommand operatorsCmd = new SqlCommand(
        "SELECT COUNT(*) FROM Clients WHERE Role = 'Operator'", conn);

                OperatorsCount = (int)operatorsCmd.ExecuteScalar();

                SqlCommand activeCmd = new SqlCommand(
        "SELECT COUNT(*) FROM Accounts WHERE IsClosed = 0", conn);

                ActiveAccountsCount = (int)activeCmd.ExecuteScalar();

                SqlCommand closedCmd = new SqlCommand(
                        "SELECT COUNT(*) FROM Accounts WHERE IsClosed = 1", conn);

                ClosedAccountsCount = (int)closedCmd.ExecuteScalar();
            }
        }

        private void LoadLastUsers()
        {
            LastUsers.Clear();

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
SELECT TOP 5
Id,
FullName,
Phone,
Role
FROM Clients
ORDER BY Id DESC", conn);

                SqlDataReader reader =
                    cmd.ExecuteReader();

                while (reader.Read())
                {
                    LastUsers.Add(new Client
                    {
                        Id = (int)reader["Id"],
                        FullName = reader["FullName"].ToString(),
                        Phone = reader["Phone"].ToString(),
                        Role = reader["Role"].ToString()
                    });
                }
            }
        }
    }
}