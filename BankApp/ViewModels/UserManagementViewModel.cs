using BankApp.Data;
using BankApp.Models;
using BankApp.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;

namespace BankApp.ViewModels
{
    public class UserManagementViewModel : BaseViewModel
    {
        private List<Client> _allUsers = new List<Client>();
        public ObservableCollection<string> Roles { get; set; }
        public ObservableCollection<string> Statuses { get; set; }
        public ObservableCollection<Client> Users { get; set; }

        private Client _selectedUser;
        public Client SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
            }
        }

        private string _searchText;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();

                ApplyFilters();
            }
        }

        private string _selectedStatus;

        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                _selectedStatus = value;
                OnPropertyChanged();

                ApplyFilters();
            }
        }

        private string _selectedRole;

        public string SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                OnPropertyChanged();

                ApplyFilters();
            }
        }

        public RelayCommand BlockUserCommand { get; set; }
        public RelayCommand UnblockUserCommand { get; set; }
        public RelayCommand MakeOperatorCommand { get; set; }
        public RelayCommand MakeClientCommand { get; set; }

        public UserManagementViewModel()
        {
            Users = new ObservableCollection<Client>();

            Roles = new ObservableCollection<string>
{
    "Все",
    "Admin",
    "Operator",
    "Client"
};

            Statuses = new ObservableCollection<string>
{
    "Все",
    "Активные",
    "Заблокированные"
};

            SelectedRole = "Все";
            SelectedStatus = "Все";

            LoadUsers();

            BlockUserCommand = new RelayCommand(BlockUser);
            UnblockUserCommand = new RelayCommand(UnblockUser);
            MakeOperatorCommand = new RelayCommand(MakeOperator);
            MakeClientCommand = new RelayCommand(MakeClient);
        }

        private bool IsSelfAction()
        {
            return SelectedUser != null &&
                   SelectedUser.Id == Session.CurrentUser.Id;
        }

        private void ApplyFilters()
        {
            if (_allUsers == null)
                return;

            var query = _allUsers.AsEnumerable();

            // ПОИСК
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string search = SearchText.ToLower();

                query = query.Where(x =>
                    x.FullName.ToLower().Contains(search) ||
                    x.Phone.ToLower().Contains(search));
            }

            // РОЛЬ
            if (SelectedRole != "Все")
            {
                query = query.Where(x => x.Role == SelectedRole);
            }

            // СТАТУС
            if (SelectedStatus == "Активные")
            {
                query = query.Where(x => !x.IsBlocked);
            }

            if (SelectedStatus == "Заблокированные")
            {
                query = query.Where(x => x.IsBlocked);
            }

            Users.Clear();

            foreach (var user in query)
            {
                Users.Add(user);
            }

            OnPropertyChanged(nameof(Users));
        }

        private void LoadUsers()
        {
            Users.Clear();
            _allUsers.Clear();

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
SELECT 
    c.Id,
    c.FullName,
    c.Phone,
    c.Role,
    c.IsBlocked,
    COUNT(a.Id) AS AccountsCount
FROM Clients c
LEFT JOIN Accounts a
    ON a.ClientId = c.Id
GROUP BY
    c.Id,
    c.FullName,
    c.Phone,
    c.Role,
    c.IsBlocked
ORDER BY c.Id DESC", conn);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Client client = new Client
                    {
                        Id = (int)reader["Id"],
                        FullName = reader["FullName"].ToString(),
                        Phone = reader["Phone"].ToString(),
                        Role = reader["Role"].ToString(),
                        IsBlocked = (bool)reader["IsBlocked"],
                        AccountsCount = (int)reader["AccountsCount"]
                    };

                    _allUsers.Add(client);
                }
            }

            ApplyFilters();
        }

        private void BlockUser()
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("Выберите пользователя");
                return;
            }

            if (SelectedUser.Id == Session.CurrentUser.Id)
            {
                MessageBox.Show("Нельзя заблокировать самого себя");
                return;
            }

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
UPDATE Clients
SET IsBlocked = 1
WHERE Id = @Id", conn);

                cmd.Parameters.AddWithValue("@Id", SelectedUser.Id);

                cmd.ExecuteNonQuery();
            }

            LogService.Log(
                "Блокировка пользователя",
                $"Администратор заблокировал пользователя {SelectedUser.FullName}");

            LoadUsers();

            MessageBox.Show("Пользователь заблокирован");
        }

        private void UnblockUser()
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("Выберите пользователя");
                return;
            }

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
UPDATE Clients
SET IsBlocked = 0
WHERE Id = @Id", conn);

                cmd.Parameters.AddWithValue("@Id", SelectedUser.Id);

                cmd.ExecuteNonQuery();
            }

            LogService.Log(
                "Разблокировка пользователя",
                $"Администратор разблокировал пользователя {SelectedUser.FullName}");

            LoadUsers();

            MessageBox.Show("Пользователь разблокирован");
        }

        private void MakeOperator()
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("Выберите пользователя");
                return;
            }
            if (IsSelfAction())
            {
                MessageBox.Show("Админ не может изменить свою роль");
                return;
            }

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
UPDATE Clients
SET Role = 'Operator'
WHERE Id = @Id", conn);

                cmd.Parameters.AddWithValue("@Id", SelectedUser.Id);

                cmd.ExecuteNonQuery();
            }

            LogService.Log(
                "Смена роли",
                $"{SelectedUser.FullName} назначен оператором");

            LoadUsers();

            MessageBox.Show("Роль обновлена");
        }

        private void MakeClient()
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("Выберите пользователя");
                return;
            }
            if (IsSelfAction())
            {
                MessageBox.Show("Админ не может изменить свою роль");
                return;
            }

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
UPDATE Clients
SET Role = 'Client'
WHERE Id = @Id", conn);

                cmd.Parameters.AddWithValue("@Id", SelectedUser.Id);

                cmd.ExecuteNonQuery();
            }

            LogService.Log(
                "Смена роли",
                $"{SelectedUser.FullName} назначен клиентом");

            LoadUsers();

            MessageBox.Show("Роль обновлена");
        }
    }
}