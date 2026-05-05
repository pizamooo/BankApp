using BankApp.Views;

namespace BankApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private object _currentView;

        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand ShowTransactionsCommand { get; set; }
        public RelayCommand ShowAccountsCommand { get; set; }
        public RelayCommand ShowTransfersCommand { get; set; }

        public MainViewModel()
        {
            ShowTransactionsCommand = new RelayCommand(() => CurrentView = new TransactionsView());
            ShowAccountsCommand = new RelayCommand(() => CurrentView = new AccountsView());
            ShowTransfersCommand = new RelayCommand(() => CurrentView = new TransfersView());

            CurrentView = new TransactionsView();
        }
    }
}