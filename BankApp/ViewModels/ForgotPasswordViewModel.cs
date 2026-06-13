using BankApp.Data;
using BankApp.Views;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BankApp.ViewModels
{
    public class ForgotPasswordViewModel : BaseViewModel
    {
        private string _identifier;
        public string Identifier
        {
            get => _identifier;
            set { _identifier = value; OnPropertyChanged(); }
        }

        public ICommand SendCodeCommand { get; }
        public ICommand BackCommand { get; }

        public ForgotPasswordViewModel()
        {
            SendCodeCommand = new RelayCommand(SendResetCode);
            BackCommand = new RelayCommand(GoBack);
        }

        private void SendResetCode()
        {
            if (string.IsNullOrWhiteSpace(Identifier))
            {
                MessageBox.Show("Введите логин или номер телефона", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string code = GenerateResetCode();

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
                        UPDATE Clients 
                        SET ResetToken = @token, 
                            ResetTokenExpiry = DATEADD(MINUTE, 15, GETDATE())
                        WHERE Login = @identifier OR Phone = @identifier", conn);

                    cmd.Parameters.AddWithValue("@token", code);
                    cmd.Parameters.AddWithValue("@identifier", Identifier);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show($"Код сброса пароля: {code}\n\n" +
                                       "Код действителен 15 минут.",
                                       "Код отправлен",
                                       MessageBoxButton.OK, MessageBoxImage.Information);

                        // Открываем окно сброса
                        var resetWindow = new ResetPasswordWindow(Identifier);
                        resetWindow.Show();

                        // Закрываем текущее окно
                        Application.Current.Windows.OfType<ForgotPasswordWindow>().FirstOrDefault()?.Close();
                    }
                    else
                    {
                        MessageBox.Show("Пользователь с таким логином/телефоном не найден.",
                                       "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке кода: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateResetCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private void GoBack()
        {
            new LoginWindow().Show();
            Application.Current.Windows.OfType<ForgotPasswordWindow>().FirstOrDefault()?.Close();
        }
    }
}
