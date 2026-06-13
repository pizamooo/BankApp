using BankApp;
using BankApp.Data;
using BankApp.Models;
using BankApp.Models.Dashboard;
using BankApp.Services;
using BankApp.ViewModels;
using BankApp.Views;
using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;

namespace BankApp.ViewModels
{
    public class TopUpViewModel : BaseViewModel
    {
        public ObservableCollection<CardItem> Cards { get; set; }
        public ObservableCollection<AccountItem> Accounts { get; set; }
        public ObservableCollection<TopUpHistoryItem> TopUpHistory { get; set; }
        private readonly PdfReceiptService _receiptService = new PdfReceiptService();

        private TopUpHistoryItem _selectedTopUp;
        public TopUpHistoryItem SelectedTopUp
        {
            get => _selectedTopUp;
            set
            {
                _selectedTopUp = value;
                OnPropertyChanged();
            }
        }
        public string UserName => Session.CurrentUser.FullName?.ToUpper();

        public decimal Commission
        {
            get
            {
                if (!decimal.TryParse(AmountText, out decimal amount))
                    return 0;

                return amount * 0.01m;
            }
        }

        public decimal Total
        {
            get
            {
                if (!decimal.TryParse(AmountText, out decimal amount))
                    return 0;

                return amount + Commission;
            }
        }

        private CardItem _selectedCard;
        public CardItem SelectedCard
        {
            get => _selectedCard;
            set
            {
                _selectedCard = value;
                OnPropertyChanged();
            }
        }

        private string _cvv;
        public string CVV
        {
            get => _cvv;
            set
            {
                _cvv = value;
                OnPropertyChanged();
            }
        }
        private AccountItem _selectedAccount;
        public AccountItem SelectedAccount
        {
            get => _selectedAccount;
            set { _selectedAccount = value; OnPropertyChanged(); }
        }

        private string _amountText;
        public string AmountText
        {
            get => _amountText;
            set
            {
                if (value != null)
                {
                    value = value.Replace(".", ",");
                }

                _amountText = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(Commission));
                OnPropertyChanged(nameof(Total));
            }
        }

        private string _info;
        public string Info
        {
            get => _info;
            set { _info = value; OnPropertyChanged(); }
        }

        public RelayCommand TopUpCommand { get; set; }
        public RelayCommand GoBackCommand { get; set; }
        public RelayCommand Add1000Command { get; set; }
        public RelayCommand Add5000Command { get; set; }
        public RelayCommand Add10000Command { get; set; }
        public RelayCommand ExportTopUpReceiptCommand { get; }

        public TopUpViewModel()
        {
            Accounts = new ObservableCollection<AccountItem>();

            TopUpCommand = new RelayCommand(TopUp);
            GoBackCommand = new RelayCommand(GoBack);
            Cards = new ObservableCollection<CardItem>();
            TopUpHistory = new ObservableCollection<TopUpHistoryItem>();

            Add1000Command = new RelayCommand(() => AddAmount(1000));
            Add5000Command = new RelayCommand(() => AddAmount(5000));
            Add10000Command = new RelayCommand(() => AddAmount(10000));
            ExportTopUpReceiptCommand = new RelayCommand(ExportTopUpReceipt);

            LoadCards();

            LoadAccounts();
            LoadTopUpHistory();
        }

        private void ExportTopUpReceipt()
        {
            if (SelectedTopUp == null)
            {
                MessageBox.Show("Сначала выберите пополнение из списка",
                               "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string clientName = Session.CurrentUser?.FullName ?? "Клиент";

                var bytes = _receiptService.GenerateTopUpReceipt(SelectedTopUp, clientName);

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF файлы (*.pdf)|*.pdf",
                    FileName = $"Чек_пополнение_{SelectedTopUp.Id}_{DateTime.Now:yyyy-MM-dd_HH-mm}.pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllBytes(dialog.FileName, bytes);
                    MessageBox.Show("✅ Чек успешно сохранён!", "Успех",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении чека:\n{ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddAmount(decimal amount)
        {
            decimal current = 0;

            decimal.TryParse(AmountText, out current);

            AmountText = (current + amount).ToString();

            OnPropertyChanged(nameof(AmountText));
            OnPropertyChanged(nameof(Commission));
        }

        private void LoadCards()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand(@"
SELECT c.*
FROM Cards c
JOIN Accounts a ON c.AccountId = a.Id
WHERE a.ClientId = @id
AND c.IsActive = 1", conn);

                cmd.Parameters.AddWithValue("@id", Session.CurrentUser.Id);

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Cards.Add(new CardItem
                    {
                        Id = (int)reader["Id"],
                        AccountId = (int)reader["AccountId"],
                        CardNumber = reader["CardNumber"].ToString(),
                        ExpiryDate = Convert.ToDateTime(reader["ExpiryDate"]).ToString("MM'/'yy"),
                        CVV = reader["CVV"].ToString(),
                        IsActive = (bool)reader["IsActive"]
                    });
                }
            }
        }

        private void LoadAccounts()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand(@"
SELECT Id, Iban, Balance
FROM Accounts
WHERE ClientId = @id AND IsClosed = 0", conn);

                cmd.Parameters.AddWithValue("@id", Session.CurrentUser.Id);

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Accounts.Add(new AccountItem
                    {
                        Id = (int)reader["Id"],
                        Iban = reader["Iban"].ToString(),
                        Balance = (decimal)reader["Balance"]
                    });
                }
            }
        }

        private void TopUp()
        {
            if (SelectedAccount == null)
            {
                Info = "Выберите счёт";
                return;
            }

            if (SelectedCard == null)
            {
                Info = "Выберите карту";
                return;
            }

            if (!SelectedCard.IsActive)
            {
                Info = "Карта заблокирована";
                return;
            }

            DateTime expiry =
                DateTime.ParseExact(
                    SelectedCard.ExpiryDate,
                    "MM/yy",
                    null);

            expiry = new DateTime(
                expiry.Year,
                expiry.Month,
                DateTime.DaysInMonth(expiry.Year, expiry.Month));

            if (expiry < DateTime.Now.Date)
            {
                Info = "Срок действия карты истёк";
                return;
            }

            if (CVV != SelectedCard.CVV)
            {
                Info = "Неверный CVV";
                return;
            }

            if (!decimal.TryParse(AmountText, out decimal amount) || amount <= 0)
            {
                Info = "Введите корректную сумму";
                return;
            }

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand(@"
UPDATE Accounts
SET Balance = Balance + @amount
WHERE Id = @id", conn);

                cmd.Parameters.AddWithValue("@amount", amount);
                cmd.Parameters.AddWithValue("@id", SelectedAccount.Id);

                cmd.ExecuteNonQuery();

                var transactionCmd = new SqlCommand(@"
INSERT INTO Transactions
(
    AccountId,
    Amount,
    Type,
    Category,
    Description,
    Date
)
VALUES
(
    @accountId,
    @amount,
    'Income',
    'Deposit',
    @desc,
    GETDATE()
)", conn);

                transactionCmd.Parameters.AddWithValue("@accountId", SelectedAccount.Id);
                transactionCmd.Parameters.AddWithValue("@amount", amount);
                transactionCmd.Parameters.AddWithValue("@desc",
                    "Пополнение с карты " + SelectedCard.MaskedCard);

                transactionCmd.ExecuteNonQuery();

                TopUpHistory.Insert(0, new TopUpHistoryItem
                {
                    Card = SelectedCard.MaskedCard,
                    Amount = "+" + amount.ToString("N2") + " ₽",
                    Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
                });
            }
            SelectedAccount.Balance += amount;

            Info = $"✅ Пополнение на {amount:N2} ₽ выполнено";
            ClientDashboardViewModel.Instance?.Refresh();
            AmountText = "";
        }

        private void LoadTopUpHistory()
        {
            TopUpHistory.Clear();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand(@"
SELECT TOP 10
    t.Amount,
    t.Date,
    t.Description
FROM Transactions t
JOIN Accounts a ON t.AccountId = a.Id
WHERE a.ClientId = @id
AND t.Category = 'Deposit'
ORDER BY t.Date DESC", conn);

                cmd.Parameters.AddWithValue("@id", Session.CurrentUser.Id);

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string desc = reader["Description"].ToString();

                    string card = "****";

                    if (desc.Contains("карты"))
                    {
                        int index = desc.LastIndexOf(" ");

                        if (index >= 0)
                        {
                            card = "**** **** **** " + desc.Substring(index + 1);
                        }
                    }

                    TopUpHistory.Add(new TopUpHistoryItem
                    {
                        Card = card,
                        Amount = "+" +
                            Convert.ToDecimal(reader["Amount"])
                            .ToString("N2") + " ₽",

                        Date = Convert.ToDateTime(reader["Date"])
                            .ToString("dd.MM.yyyy HH:mm")
                    });
                }
            }
        }

        private void GoBack()
        {
            try
            {
                NavService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка навигации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}