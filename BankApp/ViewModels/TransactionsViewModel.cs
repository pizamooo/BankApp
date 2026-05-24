using BankApp.Data;
using BankApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using ClosedXML.Excel;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using System.Windows.Documents;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using Paragraph = iText.Layout.Element.Paragraph;
using Microsoft.Win32;
using iText.Layout.Properties;
using Table = iText.Layout.Element.Table;
using iText.IO.Font;
using BankApp.Services;

namespace BankApp.ViewModels
{
    public class TransactionsViewModel : BaseViewModel
    {
        // =========================
        // DATA SOURCE (главный список)
        // =========================
        private List<Transaction> _all = new List<Transaction>();
        PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

        // =========================
        // UI коллекции
        // =========================
        public ObservableCollection<Transaction> Transactions { get; set; }
        public ObservableCollection<string> OperationTypes { get; set; }

        public ObservableCollection<ChartPoint> IncomeChart { get; set; }
        public ObservableCollection<ChartPoint> ExpenseChart { get; set; }

        // =========================
        // Accounts
        // =========================
        private ObservableCollection<Account> _accounts;
        public ObservableCollection<Account> Accounts
        {
            get => _accounts;
            set { _accounts = value; OnPropertyChanged(); }
        }

        private Transaction _selectedTransaction;
        public Transaction SelectedTransaction
        {
            get => _selectedTransaction;
            set
            {
                _selectedTransaction = value;
                OnPropertyChanged();
            }
        }

        private bool _isCanceled;
        public bool IsCanceled
        {
            get => _isCanceled;
            set
            {
                _isCanceled = value;
                OnPropertyChanged();
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();

                ApplyCurrentAccountFilter();
            }
        }

        private Account _selectedAccount;
        public Account SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                _selectedAccount = value;
                OnPropertyChanged();
                ApplyCurrentAccountFilter();
            }
        }

        // =========================
        // FILTER
        // =========================
        private DateTime? _dateFrom;
        public DateTime? DateFrom
        {
            get => _dateFrom;
            set { _dateFrom = value; OnPropertyChanged(); }
        }

        private DateTime? _dateTo;
        public DateTime? DateTo
        {
            get => _dateTo;
            set { _dateTo = value; OnPropertyChanged(); }
        }

        public RelayCommand FilterCommand { get; set; }
        public RelayCommand ResetFilterCommand { get; set; }
        public RelayCommand ExportExcelCommand { get; set; }
        public RelayCommand ExportPdfCommand { get; set; }

        public RelayCommand CancelTransactionCommand { get; set; }

        // =========================
        // INPUT
        // =========================
        private string _amountInput;
        public string AmountInput
        {
            get => _amountInput;
            set { _amountInput = value; OnPropertyChanged(); }
        }

        private string _typeInput;
        public string TypeInput
        {
            get => _typeInput;
            set { _typeInput = value; OnPropertyChanged(); }
        }

        private string _descriptionInput;
        public string DescriptionInput
        {
            get => _descriptionInput;
            set { _descriptionInput = value; OnPropertyChanged(); }
        }

        // =========================
        // STATS
        // =========================
        private decimal _totalIncome;
        public decimal TotalIncome
        {
            get => _totalIncome;
            set { _totalIncome = value; OnPropertyChanged(); }
        }

        private decimal _totalExpense;
        public decimal TotalExpense
        {
            get => _totalExpense;
            set { _totalExpense = value; OnPropertyChanged(); }
        }

        // =========================
        // COMMANDS
        // =========================
        public RelayCommand AddTransactionCommand { get; set; }

        // =========================
        // CONSTRUCTOR
        // =========================
        public TransactionsViewModel()
        {
            Transactions = new ObservableCollection<Transaction>();
            IncomeChart = new ObservableCollection<ChartPoint>();
            ExpenseChart = new ObservableCollection<ChartPoint>();

            OperationTypes = new ObservableCollection<string>
            {
                "Пополнение",
                "Списание"
            };

            FilterCommand = new RelayCommand(ApplyCurrentAccountFilter);
            ResetFilterCommand = new RelayCommand(ResetFilter);
            ExportExcelCommand = new RelayCommand(ExportExcel);
            ExportPdfCommand = new RelayCommand(ExportPdf);
            CancelTransactionCommand = new RelayCommand(CancelTransaction);
            AddTransactionCommand = new RelayCommand(AddTransaction);

            LoadAccounts();
            LoadTransactions();
        }

        private void CancelTransaction()
        {
            if (SelectedTransaction == null)
            {
                MessageBox.Show("Выберите операцию!");
                return;
            }

            if (SelectedTransaction.IsCanceled)
            {
                MessageBox.Show("Операция уже отменена!");
                return;
            }

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                // отменяем операцию
                string cancelQuery =
                    @"UPDATE Transactions
              SET IsCanceled = 1
              WHERE Id = @Id";

                SqlCommand cancelCmd = new SqlCommand(cancelQuery, conn);
                cancelCmd.Parameters.AddWithValue("@Id", SelectedTransaction.Id);
                cancelCmd.ExecuteNonQuery();

                if (SelectedTransaction.Type == "Пополнение")
                {
                    string balanceQuery =
                        "SELECT Balance FROM Accounts WHERE Id = @Id";

                    SqlCommand balanceCheck = new SqlCommand(balanceQuery, conn);

                    balanceCheck.Parameters.AddWithValue("@Id",
                        SelectedTransaction.AccountId);

                    decimal currentBalance =
                        Convert.ToDecimal(balanceCheck.ExecuteScalar());

                    if (currentBalance < SelectedTransaction.Amount)
                    {
                        MessageBox.Show(
                            "Нельзя отменить операцию. Недостаточно средств для возврата!");

                        return;
                    }
                }

                // возврат баланса
                string updateBalance;

                if (SelectedTransaction.Type == "Пополнение")
                {
                    updateBalance =
                        @"UPDATE Accounts
                  SET Balance = Balance - @Amount
                  WHERE Id = @AccountId";
                }
                else
                {
                    updateBalance =
                        @"UPDATE Accounts
                  SET Balance = Balance + @Amount
                  WHERE Id = @AccountId";
                }

                SqlCommand balanceCmd = new SqlCommand(updateBalance, conn);

                balanceCmd.Parameters.AddWithValue("@Amount", SelectedTransaction.Amount);
                balanceCmd.Parameters.AddWithValue("@AccountId", SelectedTransaction.AccountId);

                balanceCmd.ExecuteNonQuery();

                LogService.Log(
    "Отмена операции",
    $"Отменена операция #{SelectedTransaction.Id} " +
    $"({SelectedTransaction.Type}) " +
    $"на сумму {SelectedTransaction.Amount:N2} ₽");
            }

            MessageBox.Show("Операция отменена!");

            LoadAccounts();
            LoadTransactions();
        }
        private void LoadAccounts()
        {
            Accounts = new ObservableCollection<Account>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query;

                if (Session.CurrentUser.Role == "Admin" ||
                    Session.CurrentUser.Role == "Operator")
                {
                    query = @"
        SELECT Id, ClientId, AccountNumber, Balance, IsClosed
        FROM Accounts";
                }
                else
                {
                    query = @"
        SELECT Id, ClientId, AccountNumber, Balance, IsClosed
        FROM Accounts
        WHERE ClientId = @clientId";
                }

                SqlCommand cmd = new SqlCommand(query, conn);

                if (Session.CurrentUser.Role == "Client")
                {
                    cmd.Parameters.AddWithValue("@clientId", Session.CurrentUser.Id);
                }
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Accounts.Add(new Account
                    {
                        Id = (int)reader["Id"],
                        ClientId = (int)reader["ClientId"],
                        AccountNumber = reader["AccountNumber"].ToString(),
                        Balance = (decimal)reader["Balance"],
                        IsClosed = (bool)reader["IsClosed"]
                    });
                }
                if (SelectedAccount == null)
                {
                    SelectedAccount = Accounts
                        .FirstOrDefault(a => !a.IsClosed);
                }
            }
            SelectedAccount = Accounts.FirstOrDefault(x => x.Id == SelectedAccount?.Id);
        }

        // =========================
        // LOAD TRANSACTIONS
        // =========================
        public void LoadTransactions()
        {
            var list = new List<Transaction>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query;

                if (Session.CurrentUser.Role == "Admin" ||
                    Session.CurrentUser.Role == "Operator")
                {
                    query = @"
    SELECT t.Id,
           t.AccountId,
           t.Amount,
           t.Type,
           t.Date,
           t.Description,
t.IsCanceled,
           a.AccountNumber
    FROM Transactions t
    JOIN Accounts a ON t.AccountId = a.Id
    ORDER BY t.Date DESC";
                }
                else
                {
                    query = @"
    SELECT t.Id,
           t.AccountId,
           t.Amount,
           t.Type,
           t.Date,
           t.Description,
t.IsCanceled,
           a.AccountNumber
    FROM Transactions t
    JOIN Accounts a ON t.AccountId = a.Id
    WHERE a.ClientId = @clientId
    ORDER BY t.Date DESC";
                }

                SqlCommand cmd = new SqlCommand(query, conn);

                if (Session.CurrentUser.Role == "Client")
                {
                    cmd.Parameters.AddWithValue("@clientId", Session.CurrentUser.Id);
                }
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new Transaction
                    {
                        Id = (int)reader["Id"],
                        AccountId = (int)reader["AccountId"],
                        Amount = (decimal)reader["Amount"],
                        Type = reader["Type"].ToString() == "Income"
                            ? "Пополнение"
                            : "Списание",
                        Date = (DateTime)reader["Date"],
                        Description = reader["Description"].ToString(),
                        IsCanceled = reader["IsCanceled"] != DBNull.Value && Convert.ToBoolean(reader["IsCanceled"]),
                        AccountNumber = reader["AccountNumber"].ToString()
                    });
                }
            }

            _all = list;

            ApplyCurrentAccountFilter();
        }

        // =========================
        // FILTER LOGIC
        // =========================
        private void ApplyCurrentAccountFilter()
        {
            if (_all == null) return;

            var data = _all.AsEnumerable();

            if (SelectedAccount != null)
                data = data.Where(x => x.AccountNumber == SelectedAccount.AccountNumber);

            if (DateFrom != null)
                data = data.Where(x => x.Date >= DateFrom.Value);

            if (DateTo != null)
                data = data.Where(x => x.Date <= DateTo.Value);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                data = data.Where(x =>
                    x.Description != null &&
                    x.Description.ToLower().Contains(SearchText.ToLower()));
            }

            Transactions = new ObservableCollection<Transaction>(data);
            OnPropertyChanged(nameof(Transactions));

            UpdateStatsAndCharts();
        }

        // =========================
        // RESET FILTER
        // =========================
        private void ResetFilter()
        {
            DateFrom = null;
            DateTo = null;
            SearchText = "";

            OnPropertyChanged(nameof(DateFrom));
            OnPropertyChanged(nameof(DateTo));

            ApplyCurrentAccountFilter();
        }

        // =========================
        // STATS + CHARTS
        // =========================
        private void UpdateStatsAndCharts()
        {
            TotalIncome = Transactions
                .Where(x => x.Type == "Пополнение" && !x.IsCanceled)
                .Sum(x => x.Amount);

            TotalExpense = Transactions
                .Where(x => x.Type == "Списание" && !x.IsCanceled)
                .Sum(x => x.Amount);

            IncomeChart.Clear();
            ExpenseChart.Clear();

            var grouped = Transactions
                .GroupBy(x => x.Date.ToString("dd.MM"))
                .OrderBy(x => x.Key);

            foreach (var g in grouped)
            {
                IncomeChart.Add(new ChartPoint
                {
                    Label = g.Key,
                    Value = g.Where(x => x.Type == "Пополнение" &&
        !x.IsCanceled).Sum(x => x.Amount)
                });

                ExpenseChart.Add(new ChartPoint
                {
                    Label = g.Key,
                    Value = g.Where(x => x.Type == "Списание" && !x.IsCanceled).Sum(x => x.Amount)
                });
            }
        }

        // =========================
        // ADD TRANSACTION
        // =========================
        private void AddTransaction()
        {
            if (SelectedAccount == null)
            {
                MessageBox.Show("Выберите счет!");
                return;
            }

            if (SelectedAccount.IsClosed)
            {
                MessageBox.Show("Счет закрыт!");
                return;
            }

            if (!decimal.TryParse(AmountInput, out decimal amount))
            {
                MessageBox.Show("Введите корректную сумму!");
                return;
            }

            string typeDb = TypeInput == "Пополнение" ? "Income" : "Expense";

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                if (typeDb == "Expense")
                {
                    string check = "SELECT Balance FROM Accounts WHERE Id = @Id";
                    SqlCommand checkCmd = new SqlCommand(check, conn);
                    checkCmd.Parameters.AddWithValue("@Id", SelectedAccount.Id);

                    decimal balance = (decimal)checkCmd.ExecuteScalar();

                    if (balance < amount)
                    {
                        MessageBox.Show("Недостаточно средств!");
                        return;
                    }
                    if (amount <= 0)
                    {
                        MessageBox.Show("Сумма должна быть больше нуля!");
                        return;
                    }
                }

                string insert = @"
                INSERT INTO Transactions (AccountId, Amount, Type, Date, Description)
                VALUES (@AccountId, @Amount, @Type, @Date, @Description)";

                SqlCommand cmd = new SqlCommand(insert, conn);

                cmd.Parameters.AddWithValue("@AccountId", SelectedAccount.Id);
                cmd.Parameters.AddWithValue("@Amount", amount);
                cmd.Parameters.AddWithValue("@Type", typeDb);
                cmd.Parameters.AddWithValue("@Date", DateTime.Now);
                cmd.Parameters.AddWithValue("@Description", DescriptionInput ?? "");

                cmd.ExecuteNonQuery();

                string update =
                    typeDb == "Income"
                        ? "UPDATE Accounts SET Balance = Balance + @Amount WHERE Id = @Id"
                        : "UPDATE Accounts SET Balance = Balance - @Amount WHERE Id = @Id";

                SqlCommand updateCmd = new SqlCommand(update, conn);
                updateCmd.Parameters.AddWithValue("@Amount", amount);
                updateCmd.Parameters.AddWithValue("@Id", SelectedAccount.Id);

                updateCmd.ExecuteNonQuery();

                LogService.Log(
    "Создание операции",
    $"Создана операция '{TypeInput}' " +
    $"на сумму {amount:N2} ₽ " +
    $"для счета {SelectedAccount.AccountNumber}");
            }

            LoadAccounts();
            LoadTransactions();

            AmountInput = "";
            DescriptionInput = "";
        }

        private void ExportExcel()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel file (*.xlsx)|*.xlsx",
                FileName = "выписка.xlsx"
            };

            if (dialog.ShowDialog() != true)
                return;

            var path = dialog.FileName;

            var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Выписка");

            // =========================
            // ЗАГОЛОВОК ОТЧЁТА
            // =========================
            sheet.Cell(1, 1).Value = "БАНКОВСКАЯ ВЫПИСКА";
            sheet.Range(1, 1, 1, 6).Merge();
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 16;
            sheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            sheet.Cell(2, 1).Value = $"Счёт: {SelectedAccount?.AccountNumber ?? "Все счета"}";
            sheet.Range(2, 1, 2, 6).Merge();

            sheet.Cell(3, 1).Value =
                $"Период: {DateFrom?.ToString("dd.MM.yyyy") ?? "начало"} - {DateTo?.ToString("dd.MM.yyyy") ?? "сегодня"}";
            sheet.Range(3, 1, 3, 6).Merge();

            // =========================
            // ИТОГИ
            // =========================
            decimal income = Transactions.Where(x => x.Type == "Пополнение").Sum(x => x.Amount);
            decimal expense = Transactions.Where(x => x.Type == "Списание").Sum(x => x.Amount);
            decimal balance = income - expense;

            sheet.Cell(5, 1).Value = "Доход";
            sheet.Cell(5, 2).Value = "Расход";
            sheet.Cell(5, 3).Value = "Баланс";

            sheet.Cell(6, 1).Value = income;
            sheet.Cell(6, 2).Value = expense;
            sheet.Cell(6, 3).Value = balance;

            sheet.Range(5, 1, 5, 3).Style.Font.Bold = true;

            // =========================
            // ТАБЛИЦА ЗАГОЛОВКИ
            // =========================
            int startRow = 8;

            sheet.Cell(startRow, 1).Value = "ID";
            sheet.Cell(startRow, 2).Value = "Счёт";
            sheet.Cell(startRow, 3).Value = "Сумма";
            sheet.Cell(startRow, 4).Value = "Тип";
            sheet.Cell(startRow, 5).Value = "Дата";
            sheet.Cell(startRow, 6).Value = "Описание";

            sheet.Range(startRow, 1, startRow, 6).Style.Font.Bold = true;
            sheet.Range(startRow, 1, startRow, 6).Style.Fill.BackgroundColor = XLColor.LightGray;

            // =========================
            // ДАННЫЕ
            // =========================
            int row = startRow + 1;

            foreach (var t in Transactions)
            {
                sheet.Cell(row, 1).Value = t.Id;
                sheet.Cell(row, 2).Value = t.AccountNumber;
                sheet.Cell(row, 3).Value = t.Amount;
                sheet.Cell(row, 4).Value = t.Type;
                sheet.Cell(row, 5).Value = t.Date.ToString("dd.MM.yyyy HH:mm");
                sheet.Cell(row, 6).Value = t.Description;

                // цвет суммы
                if (t.Type == "Пополнение")
                    sheet.Cell(row, 3).Style.Font.FontColor = XLColor.Green;
                else
                    sheet.Cell(row, 3).Style.Font.FontColor = XLColor.Red;

                row++;
            }

            // =========================
            // АВТО ШИРИНА
            // =========================
            sheet.Columns().AdjustToContents();

            workbook.SaveAs(path);

            LogService.Log(
    "Экспорт в Excel",
    $"Экспортирована Excel-выписка по счету " +
    $"{SelectedAccount?.AccountNumber}");

            MessageBox.Show("Excel выписка готов!");
        }

        private void ExportPdf()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF file (*.pdf)|*.pdf",
                FileName = "выписка.pdf"
            };

            if (dialog.ShowDialog() != true)
                return;

            var path = dialog.FileName;

            var font = PdfFontFactory.CreateFont("c:/windows/fonts/arial.ttf", PdfEncodings.IDENTITY_H);
            var bold = PdfFontFactory.CreateFont("c:/windows/fonts/arialbd.ttf", PdfEncodings.IDENTITY_H);

            using (var writer = new PdfWriter(path))
            using (var pdf = new PdfDocument(writer))
            using (var doc = new Document(pdf))
            {
                // =========================
                // ЗАГОЛОВОК
                // =========================
                doc.Add(new Paragraph("БАНКОВСКАЯ ВЫПИСКА")
                    .SetFont(bold)
                    .SetFontSize(20)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                doc.Add(new Paragraph($"Счёт: {SelectedAccount?.AccountNumber ?? "Все счета"}")
                    .SetFont(font)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                doc.Add(new Paragraph($"Период: {DateFrom?.ToString("dd.MM.yyyy") ?? "начало"} - {DateTo?.ToString("dd.MM.yyyy") ?? "сегодня"}")
                    .SetFont(font)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                doc.Add(new Paragraph("\n"));

                // =========================
                // ИТОГИ
                // =========================
                decimal income = Transactions.Where(x => x.Type == "Пополнение").Sum(x => x.Amount);
                decimal expense = Transactions.Where(x => x.Type == "Списание").Sum(x => x.Amount);
                decimal balance = income - expense;

                Table summary = new Table(3).UseAllAvailableWidth();

                summary.AddCell(new Cell().Add(new Paragraph("Доход").SetFont(bold)));
                summary.AddCell(new Cell().Add(new Paragraph("Расход").SetFont(bold)));
                summary.AddCell(new Cell().Add(new Paragraph("Баланс").SetFont(bold)));

                summary.AddCell(new Cell().Add(new Paragraph(income.ToString("N2")).SetFont(font)));
                summary.AddCell(new Cell().Add(new Paragraph(expense.ToString("N2")).SetFont(font)));
                summary.AddCell(new Cell().Add(new Paragraph(balance.ToString("N2")).SetFont(font)));

                doc.Add(summary);

                doc.Add(new Paragraph("\n"));

                // =========================
                // ТАБЛИЦА
                // =========================
                Table table = new Table(6).UseAllAvailableWidth();

                void Header(string text)
                {
                    table.AddHeaderCell(new Cell()
                        .Add(new Paragraph(text).SetFont(bold))
                        .SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY));
                }

                Header("ID");
                Header("Счёт");
                Header("Сумма");
                Header("Тип");
                Header("Дата");
                Header("Описание");

                foreach (var t in Transactions)
                {
                    table.AddCell(new Cell().Add(new Paragraph(t.Id.ToString()).SetFont(font)));
                    table.AddCell(new Cell().Add(new Paragraph(t.AccountNumber).SetFont(font)));

                    var amountCell = new Cell().Add(new Paragraph(t.Amount.ToString("N2")).SetFont(font));

                    if (t.Type == "Пополнение")
                        amountCell.SetFontColor(iText.Kernel.Colors.ColorConstants.GREEN);
                    else
                        amountCell.SetFontColor(iText.Kernel.Colors.ColorConstants.RED);

                    table.AddCell(amountCell);

                    table.AddCell(new Cell().Add(new Paragraph(t.Type).SetFont(font)));
                    table.AddCell(new Cell().Add(new Paragraph(t.Date.ToString("dd.MM.yyyy HH:mm")).SetFont(font)));
                    table.AddCell(new Cell().Add(new Paragraph(t.Description ?? "").SetFont(font)));
                }

                doc.Add(table);

                doc.Add(new Paragraph("\n"));

                doc.Add(new Paragraph("Документ сформирован автоматически системой BankApp")
                    .SetFont(font)
                    .SetFontSize(9)
                    .SetFontColor(iText.Kernel.Colors.ColorConstants.GRAY));

            }
                LogService.Log(
    "Экспорт в PDF",
    $"Экспортирована PDF-выписка по счету " +
    $"{SelectedAccount?.AccountNumber}");

            MessageBox.Show("Выписка успешно экспортирована!");
        }
    }
}