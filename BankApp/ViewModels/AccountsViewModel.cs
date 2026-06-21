using BankApp.Data;
using BankApp.Models;
using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using BankApp.Services;
using System.Text.RegularExpressions;
using System.Globalization;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Font;
using iText.Kernel.Font;
using iText.Kernel.Colors;
using Microsoft.Extensions.Logging;


namespace BankApp.ViewModels
{
    public class AccountsViewModel : BaseViewModel
    {
        public ObservableCollection<Account> Accounts { get; set; }
        private ObservableCollection<Account> _allAccounts;
        private List<Client> _allClients = new List<Client>();
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

        private string _generatedIban;
        public string GeneratedIban
        {
            get => _generatedIban;
            set
            {
                _generatedIban = value;
                OnPropertyChanged();
            }
        }

        public bool IsEmptyStateVisible => !IsLoading && Accounts.Count == 0;

        private string _phoneSearch;

        public string PhoneSearch
        {
            get => _phoneSearch;
            set
            {
                if (value != null)
                {
                    // оставляем только цифры
                    value = Regex.Replace(value, @"[^0-9]", "");

                    // максимум 11 цифр
                    if (value.Length > 11)
                        value = value.Substring(0, 11);
                }

                _phoneSearch = value;

                OnPropertyChanged();

                FilterClients();
            }
        }

        private bool _showOnlyActive;
        public bool ShowOnlyActive
        {
            get => _showOnlyActive;
            set
            {
                _showOnlyActive = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        private bool _showOnlyClosed;
        public bool ShowOnlyClosed
        {
            get => _showOnlyClosed;
            set
            {
                _showOnlyClosed = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        private bool _showPositiveBalance;
        public bool ShowPositiveBalance
        {
            get => _showPositiveBalance;
            set
            {
                _showPositiveBalance = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        private int _sortIndex;
        public int SortIndex
        {
            get => _sortIndex;
            set
            {
                _sortIndex = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        private int _activeAccountsCount;
        public int ActiveAccountsCount
        {
            get => _activeAccountsCount;
            set
            {
                _activeAccountsCount = value;
                OnPropertyChanged();
            }
        }

        private int _closedAccountsCount;
        public int ClosedAccountsCount
        {
            get => _closedAccountsCount;
            set
            {
                _closedAccountsCount = value;
                OnPropertyChanged();
            }
        }

        private decimal _totalBalance;
        public decimal TotalBalance
        {
            get => _totalBalance;
            set
            {
                _totalBalance = value;
                OnPropertyChanged();
            }
        }

        // ================= LOADING =================

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand CloseAllAccountsCommand { get; set; }
        public RelayCommand ExportAccountsCommand { get; set; }

        public AccountsViewModel()
        {
            Accounts = new ObservableCollection<Account>();
            _allAccounts = new ObservableCollection<Account>();
            Clients = new ObservableCollection<Client>();

            ExportAccountsCommand = new RelayCommand(ExportAccounts);
            AddAccountCommand = new RelayCommand(AddAccount, () => CanCreateAccount);
            CloseAccountCommand = new RelayCommand(CloseAccount);
            OpenAccountCommand = new RelayCommand(OpenAccount);
            CloseAllAccountsCommand = new RelayCommand(CloseAllAccounts);

            LoadClients();
            LoadAccounts();
        }

        private string GenerateIban()
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
SELECT TOP 1 Iban
FROM Accounts
ORDER BY Id DESC", conn);

                object result = cmd.ExecuteScalar();

                if (result == null)
                    return "RU00000000000000000";

                string lastIban = result.ToString();

                string numericPart =
                    lastIban.Replace("RU", "");

                long number = long.Parse(numericPart);

                number++;

                return "RU" + number.ToString("D17");
            }
        }

        private void ExportAccounts()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF file (*.pdf)|*.pdf",
                FileName = "выписка.pdf"
            };

            if (dialog.ShowDialog() != true)
                return;

            string path = dialog.FileName;

            var font = PdfFontFactory.CreateFont(
                "c:/windows/fonts/arial.ttf",
                PdfEncodings.IDENTITY_H);

            var bold = PdfFontFactory.CreateFont(
                "c:/windows/fonts/arialbd.ttf",
                PdfEncodings.IDENTITY_H);

            using (var writer = new PdfWriter(path))
            using (var pdf = new PdfDocument(writer))
            using (var doc = new Document(pdf))
            {
                // =========================
                // HEADER
                // =========================

                doc.Add(new Paragraph("СПИСОК БАНКОВСКИХ СЧЕТОВ")
                    .SetFont(bold)
                    .SetFontSize(20)
                    .SetTextAlignment(
                        iText.Layout.Properties.TextAlignment.CENTER));

                doc.Add(new Paragraph(
                    $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}")
                    .SetFont(font)
                    .SetTextAlignment(
                        iText.Layout.Properties.TextAlignment.CENTER));

                doc.Add(new Paragraph("\n"));

                // =========================
                // SUMMARY
                // =========================

                int total = Accounts.Count;
                int active = Accounts.Count(x => !x.IsClosed);
                int closed = Accounts.Count(x => x.IsClosed);
                decimal balance = Accounts
                    .Where(x => !x.IsClosed)
                    .Sum(x => x.Balance);

                Table summary = new Table(4)
                    .UseAllAvailableWidth();

                void SummaryHeader(string text)
                {
                    summary.AddHeaderCell(
                        new Cell()
                            .Add(new Paragraph(text).SetFont(bold))
                            .SetBackgroundColor(
                                iText.Kernel.Colors.ColorConstants.LIGHT_GRAY));
                }

                SummaryHeader("Всего");
                SummaryHeader("Активные");
                SummaryHeader("Закрытые");
                SummaryHeader("Общий баланс");

                summary.AddCell(
                    new Cell().Add(
                        new Paragraph(total.ToString()).SetFont(font)));

                summary.AddCell(
                    new Cell().Add(
                        new Paragraph(active.ToString()).SetFont(font)));

                summary.AddCell(
                    new Cell().Add(
                        new Paragraph(closed.ToString()).SetFont(font)));

                summary.AddCell(
                    new Cell().Add(
                        new Paragraph($"{balance:N2} ₽").SetFont(font)));

                doc.Add(summary);

                doc.Add(new Paragraph("\n"));

                // =========================
                // TABLE
                // =========================

                Table table = new Table(5)
                    .UseAllAvailableWidth();

                void Header(string text)
                {
                    table.AddHeaderCell(
                        new Cell()
                            .Add(new Paragraph(text).SetFont(bold))
                            .SetBackgroundColor(
                                iText.Kernel.Colors.ColorConstants.LIGHT_GRAY));
                }

                Header("ID");
                Header("Номер счета");
                Header("Баланс");
                Header("Клиент");
                Header("Статус");

                foreach (var acc in Accounts)
                {
                    table.AddCell(
                        new Cell().Add(
                            new Paragraph(acc.Id.ToString())
                                .SetFont(font)));

                    table.AddCell(
                        new Cell().Add(
                            new Paragraph(acc.AccountNumber)
                                .SetFont(font)));

                    var balanceCell = new Cell()
                        .Add(new Paragraph(
                            $"{acc.Balance:N2} ₽")
                        .SetFont(font));

                    if (acc.Balance > 0)
                    {
                        balanceCell.SetFontColor(
                            iText.Kernel.Colors.ColorConstants.BLUE);
                    }

                    table.AddCell(balanceCell);

                    table.AddCell(
                        new Cell().Add(
                            new Paragraph(acc.ClientId.ToString())
                                .SetFont(font)));

                    string status =
                        acc.IsClosed
                        ? "Закрыт"
                        : "Активен";

                    var statusCell = new Cell()
                        .Add(new Paragraph(status)
                        .SetFont(font));

                    if (acc.IsClosed)
                    {
                        statusCell.SetFontColor(
                            iText.Kernel.Colors.ColorConstants.RED);
                    }
                    else
                    {
                        statusCell.SetFontColor(
                            iText.Kernel.Colors.ColorConstants.GREEN);
                    }

                    table.AddCell(statusCell);
                }

                doc.Add(table);

                doc.Add(new Paragraph("\n"));

                // =========================
                // FOOTER
                // =========================

                doc.Add(
                    new Paragraph(
                        "Документ сформирован автоматически системой BankApp")
                    .SetFont(font)
                    .SetFontSize(9)
                    .SetFontColor(
                        iText.Kernel.Colors.ColorConstants.GRAY));
            }
            LogService.Log(
    "Экспорт в PDF",
    $"Экспортирована PDF-выписка по счету " +
    $"{SelectedAccount?.AccountNumber}");

            MessageBox.Show("Экспорт счетов успешно выполнен!");
        }

        private void UpdateStatistics()
        {
            ActiveAccountsCount =
                _allAccounts.Count(x => !x.IsClosed);

            ClosedAccountsCount =
                _allAccounts.Count(x => x.IsClosed);

            TotalBalance =
                _allAccounts
                    .Where(x => !x.IsClosed)
                    .Sum(x => x.Balance);
        }

        private void FilterClients()
        {
            if (string.IsNullOrWhiteSpace(PhoneSearch))
            {
                FilteredClients =
                    new ObservableCollection<Client>(_allClients);

                return;
            }

            var filtered = _allClients
                .Where(x => x.Phone != null &&
                            x.Phone.Contains(PhoneSearch))
                .ToList();

            FilteredClients =
                new ObservableCollection<Client>(filtered);

            OnPropertyChanged(nameof(FilteredClients));
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
            get => _newAccountNumber;
            set
            {
                if (value != null)
                {
                    value = Regex.Replace(value, @"[^a-zA-Z0-9]", "");
                }

                _newAccountNumber = value;

                OnPropertyChanged();

                ValidateAccount();

                if (!string.IsNullOrWhiteSpace(_newAccountNumber))
                {
                    GeneratedIban = GenerateIban();
                }
                else
                {
                    GeneratedIban = "";
                }

                OnPropertyChanged(nameof(CanCreateAccount));

                RefreshCommands();
            }
        }

        private string _newBalance;
        public string NewBalance
        {
            get => _newBalance;
            set
            {
                if (value != null)
                {
                    value = Regex.Replace(value, @"[^0-9\.,]", "");

                    // запрещаем больше 1 точки/запятой
                    int separators =
                        value.Count(c => c == '.' || c == ',');

                    if (separators > 1)
                        return;
                }

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

        // ================= LOAD =================
        private void LoadClients()
        {
            Clients.Clear();
            _allClients.Clear();

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query;

                if (Session.CurrentUser.Role == "Client")
                {
                    query = "SELECT Id, FullName, Phone FROM Clients WHERE Id = @id";
                }
                else
                {
                    query = "SELECT Id, FullName, Phone FROM Clients";
                }

                SqlCommand cmd = new SqlCommand(query, conn);

                if (Session.CurrentUser.Role == "Client")
                {
                    cmd.Parameters.AddWithValue("@id", Session.CurrentUser.Id);
                }

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var client = new Client
                    {
                        Id = (int)reader["Id"],
                        FullName = reader["FullName"].ToString(),
                        Phone = reader["Phone"].ToString()
                    };

                    Clients.Add(client);
                    _allClients.Add(client);
                }
            }

            FilteredClients =
                new ObservableCollection<Client>(_allClients);
        }

        private void LoadAccounts()
        {
            IsLoading = true;
            Accounts.Clear();
            OnPropertyChanged(nameof(IsEmptyStateVisible));
            _allAccounts.Clear();

            SqlConnection conn = DatabaseHelper.GetConnection();
            conn.Open();

            string query;

            if (Session.CurrentUser.Role == "Client")
            {
                query = @"
            SELECT Id, AccountNumber, Balance, ClientId, IsClosed, Iban
            FROM Accounts
            WHERE ClientId = @clientId";
            }
            else
            {
                query = @"
            SELECT Id, AccountNumber, Balance, ClientId, IsClosed, Iban
            FROM Accounts";
            }

            SqlCommand cmd = new SqlCommand(query, conn);

            if (Session.CurrentUser.Role == "Client")
            {
                cmd.Parameters.AddWithValue("@clientId", Session.CurrentUser.Id);
            }

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Account acc = new Account
                {
                    Id = (int)reader["Id"],
                    AccountNumber = reader["AccountNumber"].ToString(),
                    Balance = (decimal)reader["Balance"],
                    ClientId = (int)reader["ClientId"],
                    IsClosed = (bool)reader["IsClosed"],
                    Iban = reader["Iban"].ToString()
                };

                _allAccounts.Add(acc);
            }

            conn.Close();

            ApplyFilter();
            UpdateStatistics();
            IsLoading = false;
        }


        // ================= FILTER =================
        private void ApplyFilter()
        {
            var result = _allAccounts.AsEnumerable();

            // клиент
            if (SelectedClient != null)
                result = result.Where(x => x.ClientId == SelectedClient.Id);

            // поиск
            if (!string.IsNullOrWhiteSpace(SearchText))
                result = result.Where(x =>
                    x.AccountNumber.Contains(SearchText));

            // активные
            if (ShowOnlyActive)
                result = result.Where(x => !x.IsClosed);

            // закрытые
            if (ShowOnlyClosed)
                result = result.Where(x => x.IsClosed);

            // баланс > 0
            if (ShowPositiveBalance)
                result = result.Where(x => x.Balance > 0);

            // сортировка
            switch (SortIndex)
            {
                case 1:
                    result = result.OrderBy(x => x.Balance);
                    break;

                case 2:
                    result = result.OrderByDescending(x => x.Balance);
                    break;

                case 3:
                    result = result.OrderBy(x => x.AccountNumber);
                    break;
            }

            Accounts.Clear();

            foreach (var item in result)
                Accounts.Add(item);
            OnPropertyChanged(nameof(IsEmptyStateVisible));
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

            string normalizedBalance = NewBalance.Replace(",", ".");

            if (!decimal.TryParse(normalizedBalance,NumberStyles.Any,CultureInfo.InvariantCulture,out balance))
            {
                MessageBox.Show("Введите корректный баланс!");
                return;
            }

            SqlConnection conn = DatabaseHelper.GetConnection();
            conn.Open();

            SqlCommand cmd = new SqlCommand(@"
                INSERT INTO Accounts
(AccountNumber, Balance, ClientId, Iban)
VALUES
(@n, @b, @c, @iban)", conn);

            cmd.Parameters.AddWithValue("@n", NewAccountNumber);
            cmd.Parameters.AddWithValue("@b", balance);
            cmd.Parameters.AddWithValue("@c", SelectedClient.Id);
            cmd.Parameters.AddWithValue("@iban", GeneratedIban);

            cmd.ExecuteNonQuery();
            conn.Close();

            NewAccountNumber = "";
            NewBalance = "";
            GeneratedIban = "";

            LoadAccounts();
            LogService.Log("Создание счета", $"Создан счет {NewAccountNumber}, IBAN {GeneratedIban}");
        }

        // ================= CLOSE =================
        private void CloseAccount()
        {
            if (SelectedAccount == null)
            {
                MessageBox.Show("Выберите счет!");
                return;
            }

            if (SelectedAccount.Balance > 0)
            {
                MessageBox.Show(
                    "Нельзя закрыть счет с положительным балансом!");

                return;
            }

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(
                    "UPDATE Accounts SET IsClosed = 1 WHERE Id = @id", conn);

                cmd.Parameters.AddWithValue("@id", SelectedAccount.Id);

                cmd.ExecuteNonQuery();

                LogService.Log("Закрытие счета", $"Закрыт счет {SelectedAccount.AccountNumber}");
            }

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

            LogService.Log("Открытие счета", $"Открыт счет {SelectedAccount.AccountNumber}");

            conn.Close();
            LoadAccounts();
        }

        // ================= CLOSE ALL =================
        private void CloseAllAccounts()
        {
            // сначала пробуем взять клиента из ComboBox
            int? clientId = SelectedClient?.Id;

            // если клиент не выбран — берем из выбранного счета
            if (clientId == null && SelectedAccount != null)
            {
                clientId = SelectedAccount.ClientId;
            }

            if (clientId == null)
            {
                MessageBox.Show(
                    "Выберите счет или клиента!");
                return;
            }

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                // есть ли счета с деньгами
                string checkQuery = @"
SELECT COUNT(*)
FROM Accounts
WHERE ClientId=@ClientId
AND Balance>0
AND IsClosed=0";

                SqlCommand checkCmd =
                    new SqlCommand(checkQuery, conn);

                checkCmd.Parameters.AddWithValue(
                    "@ClientId",
                    clientId);

                int count =
                    Convert.ToInt32(
                        checkCmd.ExecuteScalar());

                if (count > 0)
                {
                    MessageBox.Show(
                        "Нельзя закрыть счета. У клиента есть положительный баланс!");

                    return;
                }

                string query = @"
UPDATE Accounts
SET IsClosed = 1
WHERE ClientId=@ClientId
AND IsClosed=0";

                SqlCommand cmd =
                    new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue(
                    "@ClientId",
                    clientId);

                int rows = cmd.ExecuteNonQuery();

                LogService.Log(
                    "Закрытие всех счетов",
                    $"Закрыты все счета клиента ID={clientId}. Закрыто: {rows}");

                MessageBox.Show(
                    $"Успешно закрыто счетов: {rows}");
            }

            LoadAccounts();
        }
    }
}