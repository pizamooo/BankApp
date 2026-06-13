using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BankApp.Data;
using BankApp.Services;
using BankApp.Views;

namespace BankApp.ViewModels
{
    public class ResetPasswordViewModel : BaseViewModel
    {
        private readonly string _identifier;
        private readonly ResetPasswordWindow _window;

        private string _code;
        public string Code
        {
            get => _code;
            set { _code = value; OnPropertyChanged(); }
        }

        public ICommand ResetPasswordCommand { get; }
        public ICommand BackCommand { get; }

        // Новый конструктор
        public ResetPasswordViewModel(string identifier, ResetPasswordWindow window)
        {
            _identifier = identifier;
            _window = window;                    // сохраняем ссылку на окно

            ResetPasswordCommand = new RelayCommand(ResetPassword);
            BackCommand = new RelayCommand(GoBack);
        }

        private void ResetPassword()
        {
            if (_window == null)
            {
                MessageBox.Show("Ошибка окна", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            PasswordBox pbNew = _window.FindName("pbNewPassword") as PasswordBox;
            PasswordBox pbConfirm = _window.FindName("pbConfirmPassword") as PasswordBox;

            string newPassword = pbNew?.Password;
            string confirmPassword = pbConfirm?.Password;

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать минимум 6 символов", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают!", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(Code) || Code.Length != 6)
            {
                MessageBox.Show("Введите 6-значный код", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
                    UPDATE Clients
                    SET PasswordHash = @newHash,
                        ResetToken = NULL,
                        ResetTokenExpiry = NULL
                    WHERE (Login = @identifier OR Phone = @identifier)
                      AND ResetToken = @code
                      AND ResetTokenExpiry > GETDATE()", conn);

                    cmd.Parameters.AddWithValue("@newHash", PasswordHelper.HashPassword(newPassword));
                    cmd.Parameters.AddWithValue("@identifier", _identifier);
                    cmd.Parameters.AddWithValue("@code", Code);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Пароль успешно изменён!", "Успех",
                                       MessageBoxButton.OK, MessageBoxImage.Information);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (Window w in Application.Current.Windows)
                                w.Close();

                            new LoginWindow().Show();
                        });
                    }
                    else
                    {
                        MessageBox.Show("Неверный код или срок действия истёк.", "Ошибка",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при смене пароля: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GoBack()
        {
            new LoginWindow().Show();
            _window?.Close();        // закрываем только это окно
        }
    }
}