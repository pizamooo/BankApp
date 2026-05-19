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
using System.Windows.Shapes;

namespace BankApp
{
    public partial class RegisterWindow : Window
    {
        private RegisterViewModel vm;

        public RegisterWindow()
        {
            InitializeComponent();

            vm = new RegisterViewModel();

            DataContext = vm;
        }

        private void Phone_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(vm.PhoneDigits))
            {
                vm.PhoneDigits = "7";
            }
        }

        private void TogglePassword(object sender, RoutedEventArgs e)
        {
            vm.IsPasswordHidden = !vm.IsPasswordHidden;

            if (vm.IsPasswordHidden)
                PasswordBox.Password = PasswordTextBox.Text;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            vm.Password = PasswordBox.Password;
            PasswordTextBox.Text = PasswordBox.Password;
        }

        private void RepeatPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ((RegisterViewModel)DataContext).RepeatPassword =
                ((PasswordBox)sender).Password;
        }

        private void OnlyLetters_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Zа-яА-Я\s]+$");
        }

        private void OnlyDigits_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]+$");
        }

        private void Login_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Z0-9\.]+$");
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            string text = (string)e.DataObject.GetData(DataFormats.Text);

            if (!Regex.IsMatch(text, @"^[a-zA-Zа-яА-Я\s]+$"))
                e.CancelCommand();
        }

        private void Phone_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            string text = (string)e.DataObject.GetData(DataFormats.Text);

            if (!Regex.IsMatch(text, @"^[0-9]+$"))
                e.CancelCommand();
        }

        private void Login_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            string text = (string)e.DataObject.GetData(DataFormats.Text);

            if (!Regex.IsMatch(text, @"^[a-zA-Z0-9\.]+$"))
                e.CancelCommand();
        }

        private void Password_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            e.CancelCommand(); // запрещаем вставку пароля (как в банках)
        }
    }
}
