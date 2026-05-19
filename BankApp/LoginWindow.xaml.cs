using BankApp.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BankApp
{
    public partial class LoginWindow : Window
    {
        private LoginViewModel vm;

        public LoginWindow()
        {
            InitializeComponent();

            vm = new LoginViewModel();

            DataContext = vm;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            vm.Password = PasswordBox.Password;

            vm.IsCapsLockOn = Keyboard.IsKeyToggled(Key.CapsLock);

            if (!vm.IsPasswordHidden)
            {
                PasswordTextBox.Text = PasswordBox.Password;
            }
        }

        private void TogglePassword(object sender, RoutedEventArgs e)
        {
            vm.IsPasswordHidden = !vm.IsPasswordHidden;

            if (vm.IsPasswordHidden)
            {
                PasswordBox.Password = PasswordTextBox.Text;
            }
        }
    }
}