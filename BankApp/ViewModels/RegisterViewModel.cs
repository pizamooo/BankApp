using BankApp.Data;
using BankApp.Services;
using BankApp.Views;
using System.Data.SqlClient;
using System.Windows;
using System.Text.RegularExpressions;
using System.Linq;
using System;
using System.Windows.Input;

namespace BankApp.ViewModels
{
    public class RegisterViewModel : BaseViewModel
    {
        private string _fullName;
        public string FullName
        {
            get => _fullName;
            set
            {
                _fullName = value;
                OnPropertyChanged();
                ValidateAll();
            }
        }

        public string _login;
        public string Login
        {
            get => _login;
            set
            {
                _login = value;
                OnPropertyChanged();
                ValidateAll();
            }
        }
        public string _password;
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PasswordStrength));
                OnPropertyChanged(nameof(PasswordStrengthText));
                OnPropertyChanged(nameof(PasswordStrengthColor));
                ValidateAll();
            }
        }

        public string _repeatPassword;
        public string RepeatPassword
        {
            get => _repeatPassword;
            set
            {
                _repeatPassword = value;
                OnPropertyChanged();
                ValidateAll();
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

        private string _phoneDigits = "";

        public string PhoneDigits
        {
            get => _phoneDigits;
            set
            {
                var digits = new string(value.Where(char.IsDigit).ToArray());

                if (digits.StartsWith("8"))
                    digits = "7" + digits.Substring(1);

                if (!digits.StartsWith("7") && digits.Length > 0)
                    digits = "7" + digits;

                if (digits.Length > 11)
                    digits = digits.Substring(0, 11);

                _phoneDigits = digits;

                OnPropertyChanged(nameof(PhoneDigits));
                OnPropertyChanged(nameof(Phone));
                ValidateAll();
            }
        }

        public string Phone => FormatPhone(_phoneDigits);

        public string PasswordStrengthText
        {
            get
            {
                switch (PasswordStrength)
                {
                    case 0:
                        return "";

                    case 1:
                        return "Слабый";

                    case 2:
                        return "Средний";

                    case 3:
                        return "Хороший";

                    case 4:
                        return "Сильный";

                    default:
                        return "";
                }
            }
        }

        public string PasswordStrengthColor
        {
            get
            {
                switch (PasswordStrength)
                {
                    case 0:
                    case 1:
                        return "#EF4444"; // red

                    case 2:
                        return "#F59E0B"; // yellow

                    case 3:
                        return "#3B82F6"; // blue

                    case 4:
                        return "#22C55E"; // green

                    default:
                        return "#CBD5E1";
                }
            }
        }

        public string FullNameError { get; set; }
        public string PhoneError { get; set; }
        public string LoginError { get; set; }
        public string PasswordError { get; set; }
        public string RepeatPasswordError { get; set; }

        public RelayCommand RegisterCommand { get; set; }
        public RelayCommand BackCommand { get; set; }

        public bool CanRegister =>
            string.IsNullOrEmpty(FullNameError) &&
            string.IsNullOrEmpty(PhoneError) &&
            string.IsNullOrEmpty(LoginError) &&
            string.IsNullOrEmpty(PasswordError) &&
            string.IsNullOrEmpty(RepeatPasswordError) &&
            !string.IsNullOrWhiteSpace(FullName) &&
            ValidatePhoneDigits(_phoneDigits) &&
            !string.IsNullOrWhiteSpace(Login);
        public int PasswordStrength
        {
            get
            {
                int score = 0;

                if (!string.IsNullOrEmpty(Password))
                {
                    if (Password.Length >= 6) score++;
                    if (Regex.IsMatch(Password, "[A-ZА-Я]")) score++;
                    if (Regex.IsMatch(Password, "[0-9]")) score++;
                    if (Regex.IsMatch(Password, @"[!@#$%^&*]")) score++;
                }

                return score; // 0–4
            }
        }

        public RegisterViewModel()
        {
            RegisterCommand = new RelayCommand(Register, () => CanRegister);
            BackCommand = new RelayCommand(Back);
        }

        private bool IsLoginTaken(string login)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Clients WHERE Login=@l",
                    conn);

                cmd.Parameters.AddWithValue("@l", login);

                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        private string FormatPhone(string digits)
        {
            if (string.IsNullOrEmpty(digits))
                return "+7 ";

            string result = "+7 ";

            // 123
            if (digits.Length > 1)
                result += "(" + digits.Substring(1, Math.Min(3, digits.Length - 1)) + ") ";

            // 456
            if (digits.Length > 4)
                result += digits.Substring(4, Math.Min(3, digits.Length - 4)) + "-";

            // 78
            if (digits.Length > 7)
                result += digits.Substring(7, Math.Min(2, digits.Length - 7)) + "-";

            // 9+
            if (digits.Length > 9)
                result += digits.Substring(9);

            return result.TrimEnd('-', ' ');
        }
        private void ValidateAll()
        {
            FullNameError = ValidateFullName(FullName) ? "" : "Введите минимум 2 слова";

            PhoneError = ValidatePhoneDigits(_phoneDigits) ? "" : "Телефон: 79XXXXXXXXX";

            if (!ValidateLogin(Login))
            {
                LoginError = "Без пробелов, только буквы, цифры и точка";
            }
            else if (IsLoginTaken(Login))
            {
                LoginError = "Логин уже занят";
            }
            else
            {
                LoginError = "";
            }

            PasswordError = ValidatePassword(Password)
                ? ""
                : "Мин 6 символов, буква+цифра+символ";

            RepeatPasswordError =
                Password == RepeatPassword ? "" : "Пароли не совпадают";

            OnPropertyChanged(nameof(FullNameError));
            OnPropertyChanged(nameof(PhoneError));
            OnPropertyChanged(nameof(LoginError));
            OnPropertyChanged(nameof(PasswordError));
            OnPropertyChanged(nameof(RepeatPasswordError));

            OnPropertyChanged(nameof(CanRegister));
            RegisterCommand?.RaiseCanExecuteChanged();
        }

        private void Register()
        {
            if (!ValidateLogin(Login))
            {
                MessageBox.Show("Логин: только буквы, цифры и точка (без ..)");
                return;
            }

            if (string.IsNullOrWhiteSpace(RepeatPassword))
            {
                MessageBox.Show("Повторите пароль");
                return;
            }

            if (Password != RepeatPassword)
            {
                MessageBox.Show("Пароли не совпадают");
                return;
            }

            if (!ValidateFullName(FullName))
            {
                MessageBox.Show("Введите ФИО (минимум 2 слова, только буквы)");
                return;
            }

            if (!ValidatePhoneDigits(_phoneDigits))
            {
                MessageBox.Show("Телефон должен быть в формате 89XXXXXXXXX");
                return;
            }

            if (string.IsNullOrWhiteSpace(Login))
            {
                MessageBox.Show("Введите логин");
                return;
            }

            if (!ValidatePassword(Password))
            {
                MessageBox.Show("Пароль: минимум 6 символов, буквы + цифры + спецсимвол");
                return;
            }

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var check = new SqlCommand(
                    "SELECT COUNT(*) FROM Clients WHERE Login=@l",
                    conn);

                check.Parameters.AddWithValue("@l", Login);

                int exists = (int)check.ExecuteScalar();

                if (exists > 0)
                {
                    MessageBox.Show("Логин занят");
                    return;
                }

                var cmd = new SqlCommand(@"
INSERT INTO Clients
(FullName, Phone, Login, PasswordHash, Role)
VALUES
(@f, @p, @l, @pass, 'Client')", conn);

                cmd.Parameters.AddWithValue("@f", FullName.Trim());
                cmd.Parameters.AddWithValue("@p", PhoneDigits);
                cmd.Parameters.AddWithValue("@l", Login);
                cmd.Parameters.AddWithValue("@pass",
                    PasswordHelper.HashPassword(Password));

                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("Регистрация завершена");

            new LoginWindow().Show();
            Application.Current.Windows[0]?.Close();
        }

        private void Back()
        {
            LoginWindow window = new LoginWindow();
            window.Show();

            Application.Current.Windows[0]?.Close();
        }

        private bool ValidateFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return false;

            // должно быть минимум 2 слова (Фамилия Имя)
            var parts = fullName.Trim().Split(' ', (char)StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
                return false;

            // только буквы
            return parts.All(p => Regex.IsMatch(p, @"^[А-Яа-яA-Za-z\-]+$"));
        }

        private bool ValidateLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                return false;

            // только буквы, цифры и точка
            if (!Regex.IsMatch(login, @"^[a-zA-Z0-9\.]+$"))
                return false;

            // нельзя начинать или заканчивать точкой
            if (login.StartsWith(".") || login.EndsWith("."))
                return false;

            // нельзя две точки подряд
            if (login.Contains(".."))
                return false;

            return true;
        }

        private bool ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            if (password.Length < 6)
                return false;

            bool hasLetter = Regex.IsMatch(password, "[A-Za-zА-Яа-я]");
            bool hasDigit = Regex.IsMatch(password, "[0-9]");
            bool hasSpecial = Regex.IsMatch(password, @"[!@#$%^&*(),.?\:{ }|<>]");

    return hasLetter && hasDigit && hasSpecial;
        }

        private bool ValidatePhoneDigits(string digits)
        {
            return !string.IsNullOrEmpty(digits) &&
                   digits.Length == 11 &&
                   digits.StartsWith("7") &&
                   Regex.IsMatch(digits.Substring(1), @"^\d{10}$");
        }
    }
}