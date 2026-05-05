using BankApp.Data;
using BankApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BankApp.ViewModels
{
    public class TransfersViewModel : BaseViewModel
    {
        public ObservableCollection<Account> Accounts { get; set; }
        public ObservableCollection<Transfer> Transfers { get; set; }
        private ObservableCollection<Transfer> _allTransfers;

        public enum TransferFilterType
        {
            All,
            Income,
            Expense
        }

        public enum ClientType
        {
            Standard,
            VIP
        }

        private TransferFilterType _filterType = TransferFilterType.All;
        public TransferFilterType FilterType
        {
            get => _filterType;
            set
            {
                _filterType = value;
                OnPropertyChanged();
                ApplyTransferFilter();
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyTransferFilter();
            }
        }

        private decimal _monthlyLimit = 200000;
        private decimal _usedThisMonth = 0;
        public ObservableCollection<Account> ToAccounts => new ObservableCollection<Account>(
            Accounts?.Where(a => FromAccount == null || a.Id != FromAccount.Id)
            ?? new List<Account>());

        private Account _fromAccount;
        public Account FromAccount
        {
            get => _fromAccount;
            set
            {
                _fromAccount = value;
                OnPropertyChanged();
                ApplyTransferFilter();
                TransferCommand.RaiseCanExecuteChanged();
            }
        }

        private Account _toAccount;
        public Account ToAccount
        {
            get => _toAccount;
            set
            {
                _toAccount = value;
                OnPropertyChanged();
                TransferCommand.RaiseCanExecuteChanged();
            }
        }

        private string _transferAmount;
        public string TransferAmount
        {
            get => _transferAmount;
            set
            {
                _transferAmount = value;
                OnPropertyChanged();
                TransferCommand.RaiseCanExecuteChanged();
            }
        }

        private string _commission;
        public string Commission
        {
            get => _commission;
            set
            {
                _commission = value;
                OnPropertyChanged();
                TransferCommand.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand TransferCommand { get; set; }

        public TransfersViewModel()
        {
            TransferCommand = new RelayCommand(MakeTransfer, CanTransfer);
            LoadAccounts();
            LoadTransfers();
            Commission = "1%"; // 1% комиссия
        }

        private bool CanTransfer()
        {
            if (!decimal.TryParse(TransferAmount, out decimal amount))
                return false;

            if (!CheckMonthlyLimit(amount))
                return false;

            return FromAccount != null &&
                   ToAccount != null &&
                   FromAccount.Id != ToAccount.Id &&
                   !FromAccount.IsClosed &&
                   !ToAccount.IsClosed &&
                   amount > 0;
        }

        private void MakeTransfer()
        {
            if (FromAccount == null || ToAccount == null)
            {
                MessageBox.Show("Выберите счета!");
                return;
            }

            if (FromAccount.Id == ToAccount.Id)
            {
                MessageBox.Show("Нельзя переводить на тот же счет");
                return;
            }

            if (FromAccount.IsClosed || ToAccount.IsClosed)
            {
                MessageBox.Show("Один из счетов закрыт!");
                return;
            }

            if (!decimal.TryParse(TransferAmount, out decimal amount))
            {
                MessageBox.Show("Некорректная сумма");
                return;
            }

            decimal commissionAmount;

            if (FromAccount.ClientType == ClientType.VIP)
            {
                commissionAmount = 0; // VIP бесплатно
            }
            else
            {
                commissionAmount = amount * 0.01m;

                if (amount > 150000)
                    commissionAmount = amount * 0.005m;

                if (commissionAmount < 50)
                    commissionAmount = 50;

                if (commissionAmount > 1500)
                    commissionAmount = 1500;
            }
            _usedThisMonth += amount;

            decimal total = amount + commissionAmount;

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var transaction = conn.BeginTransaction();

                try
                {
                    // Проверка баланса
                    var checkCmd = new SqlCommand(
                        "SELECT Balance FROM Accounts WHERE Id=@id",
                        conn, transaction);

                    checkCmd.Parameters.AddWithValue("@id", FromAccount.Id);
                    decimal balance = (decimal)checkCmd.ExecuteScalar();

                    if (balance < total)
                    {
                        MessageBox.Show("Недостаточно средств");
                        transaction.Rollback();
                        return;
                    }

                    // Списание
                    var withdraw = new SqlCommand(
                        "UPDATE Accounts SET Balance = Balance - @sum WHERE Id=@id",
                        conn, transaction);

                    withdraw.Parameters.AddWithValue("@sum", total);
                    withdraw.Parameters.AddWithValue("@id", FromAccount.Id);
                    withdraw.ExecuteNonQuery();

                    // Пополнение
                    var deposit = new SqlCommand(
                        "UPDATE Accounts SET Balance = Balance + @sum WHERE Id=@id",
                        conn, transaction);

                    deposit.Parameters.AddWithValue("@sum", amount);
                    deposit.Parameters.AddWithValue("@id", ToAccount.Id);
                    deposit.ExecuteNonQuery();

                    // История перевода
                    var insert = new SqlCommand(@"
                INSERT INTO Transfers (FromAccountId, ToAccountId, Amount, Commission)
                VALUES (@f, @t, @a, @c)",
                        conn, transaction);

                    insert.Parameters.AddWithValue("@f", FromAccount.Id);
                    insert.Parameters.AddWithValue("@t", ToAccount.Id);
                    insert.Parameters.AddWithValue("@a", amount);
                    insert.Parameters.AddWithValue("@c", commissionAmount);

                    insert.ExecuteNonQuery();

                    var opOut = new SqlCommand(@"
INSERT INTO Transactions (AccountId, Amount, Type, Date, Description)
VALUES (@id, @amount, @type, @date, @desc)", conn, transaction);

                    opOut.Parameters.AddWithValue("@id", FromAccount.Id);
                    opOut.Parameters.AddWithValue("@amount", total);
                    opOut.Parameters.AddWithValue("@type", "Expense");
                    opOut.Parameters.AddWithValue("@date", DateTime.Now);
                    opOut.Parameters.AddWithValue("@desc", $"Перевод + комиссия {commissionAmount:N2} на {ToAccount.AccountNumber}");

                    opOut.ExecuteNonQuery();

                    var opIn = new SqlCommand(@"
INSERT INTO Transactions (AccountId, Amount, Type, Date, Description)
VALUES (@id, @amount, @type, @date, @desc)", conn, transaction);

                    opIn.Parameters.AddWithValue("@id", ToAccount.Id);
                    opIn.Parameters.AddWithValue("@amount", amount);
                    opIn.Parameters.AddWithValue("@type", "Income");
                    opIn.Parameters.AddWithValue("@date", DateTime.Now);
                    opIn.Parameters.AddWithValue("@desc", $"Перевод + комиссия {commissionAmount:N2} от {FromAccount.AccountNumber}");

                    opIn.ExecuteNonQuery();

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    MessageBox.Show("Ошибка перевода");
                }
            }

            LoadAccounts();
            LoadTransfers();

            TransferAmount = "";
            Commission = "";

            FromAccount = null;
            ToAccount = null;
        }

        private void LoadAccounts()
        {
            Accounts = new ObservableCollection<Account>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand(
                    "SELECT Id, AccountNumber, Balance, IsClosed FROM Accounts",
                    conn);

                var reader = cmd.ExecuteReader();

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

            OnPropertyChanged(nameof(Accounts));
        }
        private void LoadTransfers()
        {
            _allTransfers = new ObservableCollection<Transfer>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand(@"
            SELECT t.Id,
                   t.Amount,
                   t.Commission,
                   t.Date,
                   t.FromAccountId,
                   t.ToAccountId,
                   a1.AccountNumber AS FromAcc,
                   a2.AccountNumber AS ToAcc
            FROM Transfers t
            JOIN Accounts a1 ON t.FromAccountId = a1.Id
            JOIN Accounts a2 ON t.ToAccountId = a2.Id
            ORDER BY t.Id DESC", conn);

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    _allTransfers.Add(new Transfer
                    {
                        Id = (int)reader["Id"],
                        Amount = (decimal)reader["Amount"],
                        Commission = (decimal)reader["Commission"],
                        Date = (DateTime)reader["Date"],
                        FromAccountId = (int)reader["FromAccountId"],
                        ToAccountId = (int)reader["ToAccountId"],
                        FromAccountNumber = reader["FromAcc"].ToString(),
                        ToAccountNumber = reader["ToAcc"].ToString()
                    });
                }
            }

            ApplyTransferFilter();
        }

        private void ApplyTransferFilter()
        {
            if (_allTransfers == null)
                return;

            IEnumerable<Transfer> query = _allTransfers;

            // 1. фильтр по счету
            if (FromAccount != null)
            {
                query = query.Where(t =>
                    t.FromAccountId == FromAccount.Id ||
                    t.ToAccountId == FromAccount.Id);
            }

            // 2. Income / Expense
            switch (FilterType)
            {
                case TransferFilterType.Income:
                    query = query.Where(t => t.ToAccountId == FromAccount?.Id);
                    break;

                case TransferFilterType.Expense:
                    query = query.Where(t => t.FromAccountId == FromAccount?.Id);
                    break;

                default:
                    break;
            }

            // 3. search (счет / сумма / дата)
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string s = SearchText.ToLower();

                query = query.Where(t =>
                    t.FromAccountNumber?.ToLower().Contains(s) == true ||
                    t.ToAccountNumber?.ToLower().Contains(s) == true ||
                    t.Amount.ToString().Contains(s) ||
                    t.Date.ToString("dd.MM.yyyy HH:mm").Contains(s));
            }

            Transfers = new ObservableCollection<Transfer>(query);
            OnPropertyChanged(nameof(Transfers));
        }

        private bool CheckMonthlyLimit(decimal amount)
        {
            if (FromAccount?.ClientType == ClientType.VIP)
                return true; // VIP без ограничений

            if (_usedThisMonth + amount > _monthlyLimit)
                return false;

            return true;
        }
    }
}
