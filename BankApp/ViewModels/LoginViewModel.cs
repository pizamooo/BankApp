using BankApp.Data;
using BankApp.Models;
using BankApp.Services;
using BankApp.Views;
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

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

            if (Properties.Settings.Default.RememberMe)
            {
                Login = Properties.Settings.Default.SavedLogin;
                RememberMe = true;
            }
        }

        private void SaveUserSession()
        {
            string deviceId =
                DeviceService.GetDeviceId();

            string deviceName =
                DeviceService.GetDeviceName();

            string location = GeoLocationService.GetLocation();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                // убираем текущее устройство
                var resetCmd = new SqlCommand(@"
UPDATE UserSessions
SET IsCurrent = 0
WHERE UserId = @userId", conn);

                resetCmd.Parameters.AddWithValue(
                    "@userId",
                    Session.CurrentUser.Id);

                resetCmd.ExecuteNonQuery();

                // ищем устройство
                var checkCmd = new SqlCommand(@"
SELECT Id
FROM UserSessions
WHERE UserId = @userId
AND DeviceId = @deviceId", conn);

                checkCmd.Parameters.AddWithValue(
                    "@userId",
                    Session.CurrentUser.Id);

                checkCmd.Parameters.AddWithValue(
                    "@deviceId",
                    deviceId);

                var existing =
                    checkCmd.ExecuteScalar();

                // если устройство уже есть
                if (existing != null)
                {
                    var updateCmd = new SqlCommand(@"
UPDATE UserSessions
SET
    LoginTime = GETDATE(),
    LastActivity = GETDATE(),
    IsActive = 1,
    IsCurrent = 1
WHERE Id = @id", conn);

                    updateCmd.Parameters.AddWithValue(
                        "@id",
                        (int)existing);

                    updateCmd.ExecuteNonQuery();
                }
                else
                {
                    // первое устройство
                    var insertCmd = new SqlCommand(@"
INSERT INTO UserSessions
(
    UserId,
    DeviceId,
    DeviceName,
    Location,
    LoginTime,
    LastActivity,
    IsActive,
    IsCurrent
)
VALUES
(
    @userId,
    @deviceId,
    @deviceName,
    @location,
    GETDATE(),
    GETDATE(),
    1,
    1
)", conn);

                    insertCmd.Parameters.AddWithValue(
                        "@userId",
                        Session.CurrentUser.Id);

                    insertCmd.Parameters.AddWithValue(
                        "@deviceId",
                        deviceId);

                    insertCmd.Parameters.AddWithValue(
                        "@deviceName",
                        deviceName);

                    insertCmd.Parameters.AddWithValue(
                        "@location",
                        location);

                    insertCmd.ExecuteNonQuery();
                }
            }
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

            if (RememberMe)
            {
                Properties.Settings.Default.SavedLogin = Login;
                Properties.Settings.Default.RememberMe = true;
            }
            else
            {
                Properties.Settings.Default.SavedLogin = "";
                Properties.Settings.Default.RememberMe = false;
            }

            Properties.Settings.Default.Save();

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

                SaveUserSession();
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