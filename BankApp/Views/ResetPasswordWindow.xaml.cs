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
using System.Windows.Shapes;
using BankApp.ViewModels;

namespace BankApp.Views
{
    /// <summary>
    /// Логика взаимодействия для ResetPasswordWindow.xaml
    /// </summary>
    public partial class ResetPasswordWindow : Window
    {
        private readonly ResetPasswordViewModel _viewModel;

        public ResetPasswordWindow(string identifier)
        {
            InitializeComponent();

            _viewModel = new ResetPasswordViewModel(identifier, this);  // ← передаём текущее окно
            DataContext = _viewModel;
        }
    }
}
