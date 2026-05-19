using BankApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BankApp.Services
{
    public static class NavService
    {
        public static MainViewModel VM { get; set; }

        public static void GoBack()
        {
            try
            {
                if (VM == null)
                {
                    // Находим MainWindow и получаем его ViewModel
                    var mainWindow = Application.Current.Windows
                        .OfType<MainWindow>()
                        .FirstOrDefault();

                    if (mainWindow?.DataContext is MainViewModel mainVM)
                    {
                        VM = mainVM;
                    }
                    else
                    {
                        MessageBox.Show("Ошибка: не удалось найти главное окно",
                            "Ошибка навигации",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                }

                // Вызываем GoBack у ViewModel
                VM.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при возврате: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}
