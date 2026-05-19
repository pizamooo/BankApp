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
    }
}