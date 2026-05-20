using BankApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Логика взаимодействия для SecurityView.xaml
    /// </summary>
    public partial class SecurityView : UserControl
    {
        public SecurityView()
        {
            InitializeComponent();
            DataContext = new SecurityViewModel();
        }

        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is SecurityViewModel vm)
                vm.NewPassword = NewPasswordBox.Password;
        }

        private void RepeatPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is SecurityViewModel vm)
                vm.RepeatPassword = RepeatPasswordBox.Password;
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SecurityViewModel vm)
            {
                vm.ChangePasswordCommand.Execute(null);

                // если пароль успешно изменён
                if (vm.Info.Contains("успешно"))
                {
                    NewPasswordBox.Clear();
                    RepeatPasswordBox.Clear();
                }
            }
        }
    }
}
