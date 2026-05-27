using BankApp.Models;
using System.Windows;
using System.Windows.Controls;

namespace BankApp.Views
{
    public partial class HelpView : UserControl
    {
        public HelpView()
        {
            InitializeComponent();
        }

        private void ToggleFaq(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is FaqItem faq)
            {
                faq.IsOpen = !faq.IsOpen;
            }
        }
    }
}