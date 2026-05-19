using BankApp;
using BankApp.Data;
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
        public ObservableCollection<AccountItem> Accounts { get; set; }

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
            set { _amountText = value; OnPropertyChanged(); }
        }

        private string _info;
        public string Info
        {
            get => _info;
            set { _info = value; OnPropertyChanged(); }
        }

        public RelayCommand TopUpCommand { get; set; }
        public RelayCommand GoBackCommand { get; set; }

        public TopUpViewModel()
        {
            Accounts = new ObservableCollection<AccountItem>();

            TopUpCommand = new RelayCommand(TopUp);
            GoBackCommand = new RelayCommand(GoBack);

            LoadAccounts();
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
            }

            Info = $"✅ Пополнение на {amount:N2} ₽ выполнено";
            AmountText = "";
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