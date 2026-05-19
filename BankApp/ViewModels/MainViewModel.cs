using BankApp.Views;
using BankApp.Services;
using System.Windows;
using System.Collections.Generic;

namespace BankApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private object _currentView;

        public object CurrentView
        {
            get { return _currentView; }
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public bool IsAdmin => Session.IsAdmin;
        public bool IsOperator => Session.IsOperator;
        public bool IsClient => Session.IsClient;

        public RelayCommand ShowTransactionsCommand { get; set; }
        public RelayCommand ShowAccountsCommand { get; set; }
        public RelayCommand ShowTransfersCommand { get; set; }
        public RelayCommand OpenTransferFromDashboardCommand { get; set; }
        public RelayCommand OpenTopUpFromDashboardCommand { get; set; }

        private Stack<object> _history = new Stack<object>();

        public MainViewModel()
        {
            NavService.VM = this;
            ShowTransactionsCommand = new RelayCommand(() =>
            {
                Navigate(new TransactionsView());
            });

            ShowAccountsCommand = new RelayCommand(() =>
            {
                Navigate(new AccountsView());
            });

            ShowTransfersCommand = new RelayCommand(() =>
            {
                Navigate(new TransfersView());
            });

            OpenTransferFromDashboardCommand = new RelayCommand(() =>
            {
                Navigate(new TransfersView());
            });

            OpenTopUpFromDashboardCommand = new RelayCommand(() =>
            {
                Navigate(new TopUpView());
            });

            // старт
            switch (Session.CurrentUser.Role)
            {
                case "Admin":
                    CurrentView = new AccountsView();
                    break;

                case "Operator":
                    CurrentView = new TransactionsView();
                    break;

                case "Client":
                    CurrentView = new ClientDashboardView();
                    break;

                default:
                    MessageBox.Show("Неизвестная роль");
                    break;
            }
        }

        // =====================
        // NAVIGATION CORE
        // =====================
        public void Navigate(object view)
        {
            if (CurrentView != null)
                _history.Push(CurrentView);

            CurrentView = null;
            CurrentView = view;

            OnPropertyChanged(nameof(CurrentView));
        }

        public void GoBack()
        {
            if (_history.Count == 0)
                return;

            CurrentView = null; 

            CurrentView = _history.Pop();

            OnPropertyChanged(nameof(CurrentView));
        }
    }
}