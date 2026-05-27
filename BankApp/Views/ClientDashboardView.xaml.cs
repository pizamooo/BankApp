using BankApp.ViewModels;
using System.Windows.Controls;

namespace BankApp.Views
{
    public partial class ClientDashboardView : UserControl
    {
        public ClientDashboardView()
        {
            InitializeComponent();

            DataContext = new ClientDashboardViewModel();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var viewModel = DataContext as ViewModels.ClientDashboardViewModel;
            if (viewModel != null && viewModel.SelectedAccount != null)
            {
                viewModel.UpdateBalance();
            }
        }
    }
}