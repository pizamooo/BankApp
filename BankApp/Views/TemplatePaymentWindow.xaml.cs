using BankApp.Data;
using BankApp.Models.Dashboard;
using BankApp.Services;
using BankApp.ViewModels;
using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BankApp.Views
{
    public partial class TemplatePaymentWindow : Window
    {
        private TemplateItem _template;

        public TemplatePaymentWindow(TemplateItem template)
        {
            InitializeComponent();

            _template = template;

            LoadTemplate();
        }

        private void LoadTemplate()
        {
            TitleText.Text = _template.Name;
            IconText.Text = _template.Icon;

            switch (_template.Category)
            {
                case "Mobile":
                    InputLabel.Text = "Номер телефона";
                    InputBox.Text = "+7";
                    break;

                case "Internet":
                    InputLabel.Text = "Номер договора";
                    break;

                case "Steam":
                    InputLabel.Text = "Steam Login";
                    break;

                case "Housing":
                    InputLabel.Text = "Лицевой счет";
                    break;

                default:
                    InputLabel.Text = "Введите данные";
                    break;
            }
        }

        // =========================
        // VALIDATION
        // =========================

        private void InputBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // телефон → только цифры
            if (_template.Category == "Mobile")
            {
                e.Handled = !Regex.IsMatch(e.Text, "[0-9]");
            }

            // договор/счет → только цифры
            else if (_template.Category == "Internet"
                  || _template.Category == "Housing")
            {
                e.Handled = !Regex.IsMatch(e.Text, "[0-9]");
            }

            // steam → буквы/цифры/_
            else if (_template.Category == "Steam")
            {
                e.Handled = !Regex.IsMatch(e.Text, "[a-zA-Z0-9_]");
            }
        }

        private void AmountBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            string newText =
                textBox.Text.Insert(textBox.SelectionStart, e.Text);

            bool isValid = Regex.IsMatch(
                newText,
                @"^\d*([.,]\d{0,2})?$");

            e.Handled = !isValid;
        }

        // =========================
        // PAYMENT
        // =========================

        private void PayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string target = InputBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(target))
                {
                    MessageBox.Show("Заполните поле.");
                    return;
                }

                string amountText = AmountBox.Text.Replace('.', ',');

                if (!decimal.TryParse(amountText, out decimal amount))
                {
                    MessageBox.Show("Введите корректную сумму.");
                    return;
                }

                if (amount <= 0)
                {
                    MessageBox.Show("Сумма должна быть больше 0.");
                    return;
                }

                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    // =========================
                    // ПОЛУЧАЕМ СЧЕТ
                    // =========================

                    SqlCommand accountCmd = new SqlCommand(@"
SELECT TOP 1 Id, Balance
FROM Accounts
WHERE ClientId = @clientId
AND IsClosed = 0", conn);

                    accountCmd.Parameters.AddWithValue(
                        "@clientId",
                        Session.CurrentUser.Id);

                    SqlDataReader reader =
                        accountCmd.ExecuteReader();

                    if (!reader.Read())
                    {
                        MessageBox.Show("Счет не найден.");
                        return;
                    }

                    int accountId =
                        Convert.ToInt32(reader["Id"]);

                    decimal balance =
                        Convert.ToDecimal(reader["Balance"]);

                    reader.Close();

                    // =========================
                    // ПРОВЕРКА БАЛАНСА
                    // =========================

                    if (balance < amount)
                    {
                        MessageBox.Show("Недостаточно средств.");
                        return;
                    }

                    // =========================
                    // СПИСАНИЕ ДЕНЕГ
                    // =========================

                    SqlCommand updateCmd = new SqlCommand(@"
UPDATE Accounts
SET Balance = Balance - @amount
WHERE Id = @id", conn);

                    updateCmd.Parameters.AddWithValue(
                        "@amount",
                        amount);

                    updateCmd.Parameters.AddWithValue(
                        "@id",
                        accountId);

                    updateCmd.ExecuteNonQuery();

                    // =========================
                    // СОХРАНЕНИЕ ОПЕРАЦИИ
                    // =========================

                    SqlCommand transactionCmd =
                        new SqlCommand(@"
INSERT INTO Transactions
(
    AccountId,
    Amount,
    Type,
    Category,
    Description,
    Date
)
VALUES
(
    @accountId,
    @amount,
    'Expense',
    'Fee',
    @description,
    GETDATE()
)", conn);

                    transactionCmd.Parameters.AddWithValue(
                        "@accountId",
                        accountId);

                    transactionCmd.Parameters.AddWithValue(
                        "@amount",
                        amount);

                    transactionCmd.Parameters.AddWithValue(
                        "@description",
                        $"{_template.Name}: {target}");

                    transactionCmd.ExecuteNonQuery();

                    MessageBox.Show(
                        "Оплата успешно выполнена!");

                    // ОБНОВЛЯЕМ DASHBOARD
                    ClientDashboardViewModel
                        .Instance?
                        .Refresh();

                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}