using BankApp.ViewModels;
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
    /// Логика взаимодействия для TopUpView.xaml
    /// </summary>
    public partial class TopUpView : UserControl
    {
        public TopUpView()
        {
            InitializeComponent();
            DataContext = new TopUpViewModel();
        }

        private void CvvBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is TopUpViewModel vm)
            {
                vm.CVV = CvvBox.Password;
            }
        }

        private void AmountBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox tb = sender as TextBox;

            string text = tb.Text + e.Text;

            e.Handled = !Regex.IsMatch(text, @"^\d*([.,]\d{0,2})?$");
        }

        private void ScrollToTop_Click(object sender, RoutedEventArgs e)
        {
            MainScroll.ScrollToTop();
        }
    }
}
