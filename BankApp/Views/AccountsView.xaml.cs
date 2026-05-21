using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BankApp.Views
{
    /// <summary>
    /// Логика взаимодействия для AccountsView.xaml
    /// </summary>
    public partial class AccountsView : UserControl
    {
        public AccountsView()
        {
            InitializeComponent();
        }

        private void AccountNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^a-zA-Z0-9]+");

            e.Handled = regex.IsMatch(e.Text);
        }

        private void Balance_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // запрещаем пробел
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private void Balance_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            string futureText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            Regex regex = new Regex(@"^\d*([.,]\d{0,2})?$");

            e.Handled = !regex.IsMatch(futureText);
        }
    }
}
