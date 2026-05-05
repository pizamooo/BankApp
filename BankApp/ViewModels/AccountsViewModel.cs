using BankApp.Data;
using BankApp.Models;
using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace BankApp.ViewModels
{
    public class AccountsViewModel : BaseViewModel
    {
        public ObservableCollection<Account> Accounts { get; set; }
        private ObservableCollection<Account> _allAccounts;

        public ObservableCollection<Client> Clients { get; set; }

        private ObservableCollection<Client> _filteredClients;
        public ObservableCollection<Client> FilteredClients
        {
            get => _filteredClients;
            set
            {
                _filteredClients = value;
                OnPropertyChanged();
            }
        }

        public AccountsViewModel()
        {
            Accounts = new ObservableCollection<Account>();
            _allAccounts = new ObservableCollection<Account>();
            Clients = new ObservableCollection<Client>();

            AddAccountCommand = new RelayCommand(AddAccount, () => CanCreateAccount);
            CloseAccountCommand = new RelayCommand(CloseAccount);
            OpenAccountCommand = new RelayCommand(OpenAccount);
            CloseAllAccountsCommand = new RelayCommand(CloseAllAccounts);

            LoadClients();
            LoadAccounts();
        }

        private void RefreshCommands()
        {
            AddAccountCommand?.RaiseCanExecuteChanged();
        }

        // ================= SEARCH =================
        private string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        // ================= CLIENT =================
        private Client _selectedClient;
        public Client SelectedClient
        {
            get { return _selectedClient; }
            set
            {
                _selectedClient = value;
                OnPropertyChanged();

                SelectedClientPhone = value != null ? value.Phone : null;
                OnPropertyChanged(nameof(CanCreateAccount));
                RefreshCommands();

                ApplyFilter();
            }
        }

        private string _selectedClientPhone;
        public string SelectedClientPhone
        {
            get { return _selectedClientPhone; }
            set
            {
                _selectedClientPhone = value;
                OnPropertyChanged();
            }
        }

        // ================= CREATE =================
        private string _newAccountNumber;
        public string NewAccountNumber
        {
            get { return _newAccountNumber; }
            set
            {
                _newAccountNumber = value;
                OnPropertyChanged();

                ValidateAccount();
                OnPropertyChanged(nameof(CanCreateAccount));
                RefreshCommands();
            }
        }

        private string _newBalance;
        public string NewBalance
        {
            get { return _newBalance; }
            set
            {
                _newBalance = value;
                OnPropertyChanged();
            }
        }

        private bool _isAccountValid = true;
        public bool IsAccountValid
        {
            get { return _isAccountValid; }
            set
            {
                _isAccountValid = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanCreateAccount));
                RefreshCommands();
            }
        }

        public bool CanCreateAccount
        {
            get
            {
                return SelectedClient != null &&
                       IsAccountValid &&
                       !string.IsNullOrWhiteSpace(NewAccountNumber);
            }
        }

        // ================= SELECTED =================
        private Account _selectedAccount;
        public Account SelectedAccount
        {
            get { return _selectedAccount; }
            set
            {
                _selectedAccount = value;
                OnPropertyChanged();
            }
        }

        // ================= COMMANDS =================
        private RelayCommand _addAccountCommand;
        public RelayCommand AddAccountCommand
        {
            get => _addAccountCommand;
            set => _addAccountCommand = value;
        }
        public RelayCommand CloseAccountCommand { get; set; }
        public RelayCommand OpenAccountCommand { get; set; }
        public RelayCommand CloseAllAccountsCommand { get; set; }

        // ================= LOAD =================
        private void LoadClients()
        {
            Clients.Clear();

            SqlConnection conn = DatabaseHelper.GetConnection();
            conn.Open();

            SqlCommand cmd = new SqlCommand("SELECT Id, FullName, Phone FROM Clients", conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Clients.Add(new Client
                {
                    Id = (int)reader["Id"],
                    FullName = reader["FullName"].ToString(),
                    Phone = reader["Phone"].ToString()
                });
            }

            conn.Close();
            FilteredClients = new ObservableCollection<Client>(Clients);
        }

        private void LoadAccounts()
        {
            Accounts.Clear();
            _allAccounts.Clear();

            SqlConnection conn = DatabaseHelper.GetConnection();
            conn.Open();

            SqlCommand cmd = new SqlCommand(@"
                SELECT Id, AccountNumber, Balance, ClientId, IsClosed
                FROM Accounts", conn);

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Account acc = new Account
                {
                    Id = (int)reader["Id"],
                    AccountNumber = reader["AccountNumber"].ToString(),
                    Balance = (decimal)reader["Balance"],
                    ClientId = (int)reader["ClientId"],
                    IsClosed = (bool)reader["IsClosed"]
                };

                _allAccounts.Add(acc);
            }

            conn.Close();

            ApplyFilter();
        }


        // ================= FILTER =================
        private void ApplyFilter()
        {
            var result = _allAccounts.AsEnumerable();

            if (SelectedClient != null)
                result = result.Where(x => x.ClientId == SelectedClient.Id);

            if (!string.IsNullOrWhiteSpace(SearchText))
                result = result.Where(x => x.AccountNumber.Contains(SearchText));

            Accounts.Clear();

            foreach (var item in result)
                Accounts.Add(item);
        }

        // ================= VALIDATION =================
        private void ValidateAccount()
        {
            if (string.IsNullOrWhiteSpace(NewAccountNumber))
            {
                IsAccountValid = false;
                return;
            }

            IsAccountValid = !_allAccounts.Any(x =>
                string.Equals(x.AccountNumber, NewAccountNumber, StringComparison.OrdinalIgnoreCase));
        }

        // ================= ADD =================
        private void AddAccount()
        {
            if (!CanCreateAccount)
            {
                MessageBox.Show("Ошибка ввода");
                return;
            }

            decimal balance;

            if (!decimal.TryParse(NewBalance, out balance))
            {
                MessageBox.Show("Введите корректный баланс!");
                return;
            }

            SqlConnection conn = DatabaseHelper.GetConnection();
            conn.Open();

            SqlCommand cmd = new SqlCommand(@"
                INSERT INTO Accounts (AccountNumber, Balance, ClientId)
                VALUES (@n, @b, @c)", conn);

            cmd.Parameters.AddWithValue("@n", NewAccountNumber);
            cmd.Parameters.AddWithValue("@b", balance);
            cmd.Parameters.AddWithValue("@c", SelectedClient.Id);

            cmd.ExecuteNonQuery();
            conn.Close();

            NewAccountNumber = "";
            NewBalance = "";

            LoadAccounts();
        }

        // ================= CLOSE =================
        private void CloseAccount()
        {
            if (SelectedAccount == null) return;

            SqlConnection conn = DatabaseHelper.GetConnection();
            conn.Open();

            SqlCommand cmd = new SqlCommand(
                "UPDATE Accounts SET IsClosed = 1 WHERE Id = @id", conn);

            cmd.Parameters.AddWithValue("@id", SelectedAccount.Id);
            cmd.ExecuteNonQuery();

            conn.Close();
            LoadAccounts();
        }

        // ================= OPEN =================
        private void OpenAccount()
        {
            if (SelectedAccount == null) return;

            SqlConnection conn = DatabaseHelper.GetConnection();
            conn.Open();

            SqlCommand cmd = new SqlCommand(
                "UPDATE Accounts SET IsClosed = 0 WHERE Id = @id", conn);

            cmd.Parameters.AddWithValue("@id", SelectedAccount.Id);
            cmd.ExecuteNonQuery();

            conn.Close();
            LoadAccounts();
        }

        // ================= CLOSE ALL =================
        private void CloseAllAccounts()
        {
            if (SelectedClient == null) return;

            SqlConnection conn = DatabaseHelper.GetConnection();
            conn.Open();

            SqlCommand cmd = new SqlCommand(
                "UPDATE Accounts SET IsClosed = 1 WHERE ClientId = @id", conn);

            cmd.Parameters.AddWithValue("@id", SelectedClient.Id);
            cmd.ExecuteNonQuery();

            conn.Close();
            LoadAccounts();
        }
    }
}