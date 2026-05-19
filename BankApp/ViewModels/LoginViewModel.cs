using BankApp.Data;
using BankApp.Models;
using BankApp.Services;
using BankApp.Views;
using System.Data.SqlClient;
using System.Windows;

namespace BankApp.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private string _login;

        public string Login
        {
            get => _login;
            set
            {
                _login = value;

                Validate();

                OnPropertyChanged();
                OnPropertyChanged(nameof(CanLogin));
            }
        }

        private string _password;

        public string Password
        {
            get => _password;
            set
            {
                _password = value;

                Validate();

                OnPropertyChanged();
                OnPropertyChanged(nameof(CanLogin));
            }
        }

        private bool _isPasswordHidden = true;

        public bool IsPasswordHidden
        {
            get => _isPasswordHidden;
            set
            {
                _isPasswordHidden = value;
                OnPropertyChanged();
            }
        }

        private bool _rememberMe;

        public bool RememberMe
        {
            get => _rememberMe;
            set
            {
                _rememberMe = value;
                OnPropertyChanged();
            }
        }

        private bool _isBlocked;

        public bool IsBlocked
        {
            get => _isBlocked;
            set
            {
                _isBlocked = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanLogin));
            }
        }

        private bool _isCapsLockOn;

        public bool IsCapsLockOn
        {
            get => _isCapsLockOn;
            set
            {
                _isCapsLockOn = value;
                OnPropertyChanged();
            }
        }

        public string LoginError { get; set; }
        public string PasswordError { get; set; }

        private int _attempts = 0;

        public bool CanLogin =>
            !string.IsNullOrWhiteSpace(Login) &&
            !string.IsNullOrWhiteSpace(Password) &&
            !IsBlocked;

        public RelayCommand LoginCommand { get; set; }
        public RelayCommand OpenRegisterCommand { get; set; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(LoginUser);
            OpenRegisterCommand = new RelayCommand(OpenRegister);
        }

        private void Validate()
        {
            LoginError =
                string.IsNullOrWhiteSpace(Login)
                ? "Введите логин"
                : "";

            PasswordError =
                string.IsNullOrWhiteSpace(Password)
                ? "Введите пароль"
                : "";

            OnPropertyChanged(nameof(LoginError));
            OnPropertyChanged(nameof(PasswordError));
        }

        private void LoginUser()
        {
            if (IsBlocked)
            {
                return;
            }

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand(@"
SELECT *
FROM Clients
WHERE Login COLLATE Latin1_General_CS_AS = @login", conn);

                cmd.Parameters.AddWithValue("@login", Login);

                var reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    LoginError = "Пользователь не найден";

                    OnPropertyChanged(nameof(LoginError));

                    return;
                }

                string hash = reader["PasswordHash"].ToString();

                bool valid = PasswordHelper.Verify(Password, hash);

                if (!valid)
                {
                    _attempts++;

                    PasswordError =
                        $"Неверный пароль ({_attempts}/3)";

                    OnPropertyChanged(nameof(PasswordError));

                    if (_attempts >= 3)
                    {
                        IsBlocked = true;

                        PasswordError =
                            "Слишком много попыток";

                        OnPropertyChanged(nameof(PasswordError));
                    }

                    return;
                }

                _attempts = 0;

                Session.CurrentUser = new Client
                {
                    Id = (int)reader["Id"],
                    FullName = reader["FullName"].ToString(),
                    Phone = reader["Phone"].ToString(),
                    Login = reader["Login"].ToString(),
                    Role = reader["Role"].ToString()
                };
            }

            MainWindow window = new MainWindow();
            window.Show();

            Application.Current.Windows[0]?.Close();
        }

        private void OpenRegister()
        {
            RegisterWindow window = new RegisterWindow();
            window.Show();

            Application.Current.Windows[0]?.Close();
        }
    }
}