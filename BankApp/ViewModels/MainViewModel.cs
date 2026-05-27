using BankApp.Views;
using BankApp.Services;
using System.Windows;
using System.Collections.Generic;
using BankApp.Data;
using System.Data.SqlClient;

namespace BankApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public GridLength SidebarWidth
        {
            get
            {
                return IsClient
                    ? new GridLength(0)
                    : new GridLength(220);
            }
        }

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
        public RelayCommand ShowDashboardCommand { get; set; }
        public RelayCommand OpenTransferFromDashboardCommand { get; set; }
        public RelayCommand OpenTopUpFromDashboardCommand { get; set; }
        public RelayCommand OpenSecurityCommand { get; set; }
        public RelayCommand ShowUsersCommand { get; set; }
        public RelayCommand ShowAdminDashboardCommand { get; set; }
        public RelayCommand LogoutCommand { get; set; }
        public RelayCommand ShowHelpCommand { get; set; }

        private Stack<object> _history = new Stack<object>();

        public MainViewModel()
        {
            ShowHelpCommand = new RelayCommand(() =>
            {
                Navigate(new HelpView());
            });
            LogoutCommand = new RelayCommand(Logout);

            ShowDashboardCommand = new RelayCommand(OpenDashboard);

            ShowAdminDashboardCommand = new RelayCommand(() =>
            {
                Navigate(new AdminDashboardView());
            });

            ShowUsersCommand = new RelayCommand(() =>
            {
                Navigate(new UserManagementView());
            });

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
            OpenSecurityCommand = new RelayCommand(() =>
            {
                Navigate(new SecurityView());
            });

            // старт
            switch (Session.CurrentUser.Role)
            {
                case "Admin":
                    CurrentView = new AdminDashboardView();
                    break;

                case "Operator":
                    CurrentView = new OperatorDashboardView();
                    break;

                case "Client":
                    CurrentView = new ClientDashboardView();
                    break;

                default:
                    MessageBox.Show("Неизвестная роль");
                    break;
            }
        }

        private void Logout()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand(@"
UPDATE UserSessions
SET
    IsActive = 0,
    IsCurrent = 0,
    LastActivity = GETDATE()
WHERE UserId = @userId
AND IsCurrent = 1", conn);

                cmd.Parameters.AddWithValue(
                    "@userId",
                    Session.CurrentUser.Id);

                cmd.ExecuteNonQuery();
            }

            Session.CurrentUser = null;

            LoginWindow login = new LoginWindow();
            login.Show();

            foreach (Window window in Application.Current.Windows)
            {
                if (window is MainWindow)
                {
                    window.Close();
                    break;
                }
            }
        }

        private void OpenDashboard()
        {
            CurrentView = new OperatorDashboardView();
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