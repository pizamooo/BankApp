using BankApp.Data;
using BankApp.Models;
using BankApp.Services;
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
    public class SecurityViewModel : BaseViewModel
    {
        public ObservableCollection<UserSessionModel> Sessions { get; set; }

        private string _currentPassword;
        public string CurrentPassword
        {
            get => _currentPassword;
            set
            {
                _currentPassword = value;
                OnPropertyChanged();
            }
        }

        private string _newPassword;
        public string NewPassword
        {
            get => _newPassword;
            set
            {
                _newPassword = value;
                OnPropertyChanged();
            }
        }

        private string _repeatPassword;
        public string RepeatPassword
        {
            get => _repeatPassword;
            set
            {
                _repeatPassword = value;
                OnPropertyChanged();
            }
        }

        private string _info;
        public string Info
        {
            get => _info;
            set
            {
                _info = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand ChangePasswordCommand { get; set; }
        public RelayCommand GoBackCommand { get; set; }

        public RelayCommand LogoutAllDevice {  get; set; }
        public SecurityViewModel() 
        {
            ChangePasswordCommand = new RelayCommand(ChangePassword);
            GoBackCommand = new RelayCommand(GoBack);
            LogoutAllDevice = new RelayCommand(LogoutAllDevices);
            LoadSessions();
        }

        private void LoadSessions()
        {
            Sessions = new ObservableCollection<UserSessionModel>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand(@"
SELECT DeviceName, Location, LoginTime, IsCurrent
FROM UserSessions
WHERE UserId = @id
ORDER BY LoginTime DESC", conn);

                cmd.Parameters.AddWithValue("@id", Session.CurrentUser.Id);

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Sessions.Add(new UserSessionModel
                    {
                        DeviceName = reader["DeviceName"].ToString(),
                        Location = reader["Location"].ToString(),
                        LoginTime = (DateTime)reader["LoginTime"],
                        IsCurrent = (bool)reader["IsCurrent"]
                    });
                }
            }
        }

        private void LogoutAllDevices()
        {
            string currentDevice =
                DeviceService.GetDeviceId();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand(@"
UPDATE UserSessions
SET IsActive = 0,
    IsCurrent = 0
WHERE UserId = @id
AND DeviceId != @deviceId", conn);

                cmd.Parameters.AddWithValue(
                    "@id",
                    Session.CurrentUser.Id);

                cmd.Parameters.AddWithValue(
                    "@deviceId",
                    currentDevice);

                cmd.ExecuteNonQuery();
            }

            LoadSessions();

            Info = "✅ Выполнен выход со всех устройств";
        }

        private void ChangePassword()
        {
            if (string.IsNullOrWhiteSpace(CurrentPassword))
            {
                Info = "Введите текущий пароль";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                Info = "Введите новый пароль";
                return;
            }

            if (NewPassword.Length < 6)
            {
                Info = "Пароль должен быть не менее 6 символов";
                return;
            }

            if (NewPassword != RepeatPassword)
            {
                Info = "Пароли не совпадают";
                return;
            }

            if (CurrentPassword == NewPassword)
            {
                Info = "Новый пароль должен отличаться";
                return;
            }

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                // текущий hash
                var cmd = new SqlCommand(@"
SELECT PasswordHash
FROM Clients
WHERE Id = @id", conn);

                cmd.Parameters.AddWithValue(
                    "@id",
                    Session.CurrentUser.Id);

                string currentHash =
                    cmd.ExecuteScalar()?.ToString();

                bool valid =
                    PasswordHelper.Verify(
                        CurrentPassword,
                        currentHash);

                if (!valid)
                {
                    Info = "Текущий пароль неверный";
                    return;
                }

                string newHash =
                    PasswordHelper.HashPassword(NewPassword);

                var updateCmd = new SqlCommand(@"
UPDATE Clients
SET PasswordHash = @hash
WHERE Id = @id", conn);

                updateCmd.Parameters.AddWithValue(
                    "@hash",
                    newHash);

                updateCmd.Parameters.AddWithValue(
                    "@id",
                    Session.CurrentUser.Id);

                updateCmd.ExecuteNonQuery();
            }

            CurrentPassword = "";
            NewPassword = "";
            RepeatPassword = "";

            OnPropertyChanged(nameof(CurrentPassword));
            OnPropertyChanged(nameof(NewPassword));
            OnPropertyChanged(nameof(RepeatPassword));

            Info = "✅ Пароль успешно изменён";
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
