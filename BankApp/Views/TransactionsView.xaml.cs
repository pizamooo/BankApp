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
    /// Логика взаимодействия для TransactionsView.xaml
    /// </summary>
    public partial class TransactionsView : UserControl
    {
        private static readonly Regex _regex = new Regex(@"^[0-9]+([.,][0-9]{0,2})?$");
        public TransactionsView()
        {
            InitializeComponent();
        }

        private void Amount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            string fullText = textBox.Text.Insert(
                textBox.SelectionStart,
                e.Text);

            e.Handled = !_regex.IsMatch(fullText);
        }

        private void Amount_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // запрещаем пробел
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }
    }
}
