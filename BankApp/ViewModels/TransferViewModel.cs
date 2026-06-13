using BankApp.Data;
using BankApp.Models;
using BankApp.Models.Dashboard;
using BankApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;

namespace BankApp.ViewModels
{
    public class TransferViewModel : BaseViewModel
    {
        // =====================
        // ACCOUNTS
        // =====================
        public ObservableCollection<AccountItem> Accounts { get; set; }
        public ObservableCollection<TransferHistoryItem> Transfers { get; set; }
        private readonly PdfReceiptService _receiptService = new PdfReceiptService();

        private AccountItem _selectedFromAccount;
        public AccountItem SelectedFromAccount
        {
            get => _selectedFromAccount;
            set
            {
                _selectedFromAccount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AvailableBalance));
                OnPropertyChanged(nameof(CanTransfer));
                OnPropertyChanged(nameof(SelectedBalance));
            }
        }

        // =====================
        // INPUT
        // =====================
        private string _iban;
        public string Iban
        {
            get => _iban;
            set
            {
                if (value == null)
                    value = "";

                // оставляем только буквы и цифры
                string filtered = new string(value
                    .Where(char.IsLetterOrDigit)
                    .ToArray());

                if (filtered.Length > 19)
                    filtered = filtered.Substring(0, 19);

                _iban = filtered;

                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(_iban) && _iban.Length >= 6)
                {
                    FindReceiverByIban();
                }
                else
                {
                    ReceiverName = "";
                }

                OnPropertyChanged(nameof(CanTransfer));
            }
        }

        private string _phoneNumber;

        public string PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                _phoneNumber = value;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(_phoneNumber)
                    && _phoneNumber.Length == 11)
                {
                    FindReceiverByPhone();
                }
                else
                {
                    ReceiverName = "";
                    // Очищаем IBAN если номер неполный
                    if (string.IsNullOrWhiteSpace(_phoneNumber) || _phoneNumber.Length < 11)
                    {
                        _iban = "";
                        OnPropertyChanged(nameof(Iban));
                    }
                    OnPropertyChanged(nameof(ReceiverName));
                }
            }
        }

        private string _amountText = "";

        public string AmountText
        {
            get => _amountText;
            set
            {
                // Фильтруем ввод: только цифры, точка и запятая
                string filtered = new string(value.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());

                // Проверяем количество цифр после разделителя
                int separatorIndex = filtered.IndexOfAny(new[] { '.', ',' });

                if (separatorIndex >= 0)
                {
                    char separator = filtered[separatorIndex];
                    string beforeSeparator = filtered.Substring(0, separatorIndex + 1);
                    string afterSeparator = filtered.Substring(separatorIndex + 1)
                        .Replace(".", "")
                        .Replace(",", "");

                    // Ограничиваем до 2 цифр после запятой/точки
                    if (afterSeparator.Length > 2)
                        afterSeparator = afterSeparator.Substring(0, 2);

                    filtered = beforeSeparator + afterSeparator;
                }

                _amountText = filtered;

                OnPropertyChanged();
                OnPropertyChanged(nameof(Amount));
                OnPropertyChanged(nameof(Commission));
                OnPropertyChanged(nameof(Total));
                OnPropertyChanged(nameof(CanTransfer));
            }
        }
        public decimal Amount
        {
            get
            {
                string normalized = (AmountText ?? "")
                    .Replace(',', '.');

                if (decimal.TryParse(normalized,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal value))
                {
                     return Math.Round(value, 2);
                }

                return 0;
            }
        }

        public decimal SelectedBalance => SelectedFromAccount?.Balance ?? 0;


        // =====================
        // RECEIVER INFO (ВАЖНО)
        // =====================
        private string _receiverName;
        public string ReceiverName
        {
            get => _receiverName;
            set
            {
                _receiverName = value;
                OnPropertyChanged();
            }
        }

        public string TransferInfo { get; set; }

        // =====================
        // COMMISSION
        // =====================
        public decimal Commission => Math.Round(Amount * 0.015m, 2);

        public decimal Total => Amount + Commission;

        public decimal AvailableBalance =>
            SelectedFromAccount?.Balance ?? 0;

        public bool CanTransfer =>
            SelectedFromAccount != null
            && Amount > 0
            && !string.IsNullOrWhiteSpace(Iban)
            && AvailableBalance >= Total;

        private TransferHistoryItem _selectedTransfer;
        public TransferHistoryItem SelectedTransfer
        {
            get => _selectedTransfer;
            set
            {
                _selectedTransfer = value;
                OnPropertyChanged();
            }
        }

        // =====================
        // COMMANDS
        // =====================
        public RelayCommand TransferCommand { get; set; }
        public RelayCommand GoBackCommand { get; set; }
        public RelayCommand ExportReceiptCommand { get; }

        public TransferViewModel()
        {
            Accounts = new ObservableCollection<AccountItem>();
            TransferCommand = new RelayCommand(Transfer);
            GoBackCommand = new RelayCommand(GoBack);
            Transfers = new ObservableCollection<TransferHistoryItem>();
            ExportReceiptCommand = new RelayCommand(ExportReceipt);

            LoadAccounts();
            LoadTransfers();
        }

        private void ExportReceipt()
        {
            if (SelectedTransfer == null)
            {
                MessageBox.Show("Выберите перевод для печати чека", "Внимание",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string clientName = Session.CurrentUser?.FullName ?? "Клиент";

                var bytes = _receiptService.GenerateTransferReceipt(SelectedTransfer, clientName);

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF файлы (*.pdf)|*.pdf",
                    FileName = $"Чек_перевод_{SelectedTransfer.Id}_{DateTime.Now:yyyy-MM-dd_HH-mm}.pdf"
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
        private void LoadAccounts()
        {
            Accounts.Clear();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand(@"
SELECT a.Id, a.Iban, a.Balance, c.FullName
FROM Accounts a
JOIN Clients c ON a.ClientId = c.Id
WHERE a.ClientId = @id AND a.IsClosed = 0", conn);

                cmd.Parameters.AddWithValue("@id", Session.CurrentUser.Id);

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Accounts.Add(new AccountItem
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Iban = reader["Iban"].ToString(),
                        Balance = Convert.ToDecimal(reader["Balance"]),
                        Name = reader["FullName"].ToString()
                    });
                }
            }
        }

        private void LoadTransfers()
        {
            Transfers.Clear();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand(@"
SELECT TOP 50
    t.Id,
    t.Amount,
    t.Date,
    t.Category,
    t.Description
FROM Transactions t
WHERE t.AccountId IN (
    SELECT Id FROM Accounts WHERE ClientId = @id
)
AND t.Category IN ('TransferIn','TransferOut')
ORDER BY t.Date DESC", conn);

                cmd.Parameters.AddWithValue("@id", Session.CurrentUser.Id);

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    bool incoming =
                        reader["Category"].ToString() == "TransferIn";

                    decimal amount =
                        Convert.ToDecimal(reader["Amount"]);

                    string desc =
                        reader["Description"].ToString();

                    Transfers.Add(new TransferHistoryItem
                    {
                        Id = Convert.ToInt32(reader["Id"]),

                        Description = desc,

                        Title = incoming
                            ? "📥 Входящий перевод"
                            : "📤 Исходящий перевод",

                        Amount = amount,

                        AmountText =
                            (incoming ? "+" : "-")
                            + amount.ToString("N2")
                            + " ₽",

                        DateText =
                            Convert.ToDateTime(reader["Date"])
                            .ToString("dd.MM.yyyy HH:mm"),

                        IsIncoming = incoming
                    });
                }
            }

            OnPropertyChanged(nameof(Transfers));
        }

        private void FindReceiverByPhone()
        {
            ReceiverName = "";

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand(@"
SELECT TOP 1
    a.Id,
    a.Iban,
    c.FullName
FROM Accounts a
JOIN Clients c ON a.ClientId = c.Id
WHERE c.Phone = @phone
AND a.IsClosed = 0
ORDER BY a.Id", conn);

                cmd.Parameters.AddWithValue("@phone", PhoneNumber);

                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    string iban = reader["Iban"].ToString();
                    string fullName = reader["FullName"].ToString();

                    _iban = iban;
                    OnPropertyChanged(nameof(Iban));

                    ReceiverName = $"👤 {fullName}";
                }
                else
                {
                    ReceiverName = "❌ Получатель не найден";
                    _iban = "";
                    OnPropertyChanged(nameof(Iban));
                }
            }

            OnPropertyChanged(nameof(ReceiverName));
        }

        // =====================
        // VALIDATE RECEIVER
        // =====================
        private void FindReceiverByIban()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand(@"
SELECT TOP 1
    c.FullName,
    c.Phone
FROM Accounts a
JOIN Clients c ON a.ClientId = c.Id
WHERE a.Iban = @iban
AND a.IsClosed = 0", conn);

                cmd.Parameters.Add("@iban", SqlDbType.NVarChar).Value = Iban;

                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    string fullName = reader["FullName"].ToString();
                    string phone = reader["Phone"].ToString();

                    ReceiverName = $"👤 {fullName}";
                    OnPropertyChanged(nameof(PhoneNumber));

                    // Автозаполнение номера телефона
                    if (!string.IsNullOrWhiteSpace(phone))
                    {
                        _phoneNumber = phone;
                        OnPropertyChanged(nameof(PhoneNumber));
                    }
                }
                else
                {
                    ReceiverName = "❌ Получатель не найден";
                    _phoneNumber = "";
                    OnPropertyChanged(nameof(PhoneNumber));
                }
            }

            OnPropertyChanged(nameof(ReceiverName));
        }

        private AccountItem GetReceiverAccount(string iban)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand(@"
SELECT a.Id, a.Iban, a.Balance, c.FullName
FROM Accounts a
JOIN Clients c ON a.ClientId = c.Id
WHERE a.Iban = @iban", conn);

                cmd.Parameters.AddWithValue("@iban", iban);

                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    string name = reader["FullName"].ToString();

                    ReceiverName = name;

                    return new AccountItem
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Iban = reader["Iban"].ToString(),
                        Balance = Convert.ToDecimal(reader["Balance"]),
                        Name = name
                    };
                }
            }

            return null;
        }

        // =====================
        // TRANSFER LOGIC
        // =====================
        private void Transfer()
        {
            if (string.IsNullOrWhiteSpace(Iban))
            {
                TransferInfo = "Введите IBAN получателя";
                OnPropertyChanged(nameof(TransferInfo));
                return;
            }

            if (SelectedFromAccount == null)
            {
                TransferInfo = "Выберите счет отправителя";
                OnPropertyChanged(nameof(TransferInfo));
                return;
            }

            if (Amount <= 0)
            {
                TransferInfo = "Введите сумму";
                OnPropertyChanged(nameof(TransferInfo));
                return;
            }

            var receiver = GetReceiverAccount(Iban);

            if (receiver == null)
            {
                TransferInfo = "❌ Получатель не найден";
                OnPropertyChanged(nameof(TransferInfo));
                return;
            }

            if (receiver.Id == SelectedFromAccount.Id)
            {
                TransferInfo = "❌ Нельзя переводить самому себе";
                OnPropertyChanged(nameof(TransferInfo));
                return;
            }

            if (SelectedFromAccount.Balance < Total)
            {
                TransferInfo = "❌ Недостаточно средств";
                OnPropertyChanged(nameof(TransferInfo));
                return;
            }

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        // списание
                        var cmd1 = new SqlCommand(@"
UPDATE Accounts
SET Balance = Balance - @total
WHERE Id = @id", conn, tx);

                        cmd1.Parameters.AddWithValue("@total", Total);
                        cmd1.Parameters.AddWithValue("@id", SelectedFromAccount.Id);
                        cmd1.ExecuteNonQuery();

                        // начисление
                        var cmd2 = new SqlCommand(@"
UPDATE Accounts
SET Balance = Balance + @amount
WHERE Id = @id", conn, tx);

                        cmd2.Parameters.AddWithValue("@amount", Amount);
                        cmd2.Parameters.AddWithValue("@id", receiver.Id);
                        cmd2.ExecuteNonQuery();

                        // OUT
                        var cmd3 = new SqlCommand(@"
INSERT INTO Transactions(AccountId, Amount, Type, Category, Date, Description)
VALUES(@a,@am,'Expense','TransferOut',GETDATE(),@desc)", conn, tx);

                        cmd3.Parameters.AddWithValue("@a", SelectedFromAccount.Id);
                        cmd3.Parameters.AddWithValue("@am", Amount);
                        cmd3.Parameters.AddWithValue("@desc",
                            $"Перевод → {receiver.Name} ({receiver.Iban})");

                        cmd3.ExecuteNonQuery();

                        // IN
                        var cmd4 = new SqlCommand(@"
INSERT INTO Transactions(AccountId, Amount, Type, Category, Date, Description)
VALUES(@a,@am,'Income','TransferIn',GETDATE(),@desc)", conn, tx);

                        cmd4.Parameters.AddWithValue("@a", receiver.Id);
                        cmd4.Parameters.AddWithValue("@am", Amount);
                        cmd4.Parameters.AddWithValue("@desc",
    $"Перевод от {SelectedFromAccount.Name} ({SelectedFromAccount.Iban})");

                        cmd4.ExecuteNonQuery();
                        
                        tx.Commit();

                        // обновляем dashboard
                        ClientDashboardViewModel.Instance?.Refresh();

                        // обновляем счета
                        int currentId = SelectedFromAccount.Id;

                        LoadAccounts();
                        LoadTransfers();

                        // заново выбираем счет
                        SelectedFromAccount = Accounts
                            .FirstOrDefault(x => x.Id == currentId);

                        // обновляем UI
                        OnPropertyChanged(nameof(SelectedBalance));
                        OnPropertyChanged(nameof(AvailableBalance));
                        OnPropertyChanged(nameof(CanTransfer));

                        TransferInfo = $"✅ Успешно переведено {Amount:N2} ₽ → {receiver.Name}";

                        AmountText = "";
                        Iban = "";
                        ReceiverName = "";

                        OnPropertyChanged(nameof(TransferInfo));
                        OnPropertyChanged(nameof(ReceiverName));
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        TransferInfo = "❌ Ошибка: " + ex.Message;
                        OnPropertyChanged(nameof(TransferInfo));
                    }
                }
            }
        }

        // =====================
        // BACK
        // =====================
        private void GoBack()
        {
            NavService.GoBack();
        }
    }
}