using System.Data.SqlClient;

namespace BankApp.Data
{
    public static class DatabaseHelper
    {
        private static string connectionString =
            "Server=pizamooo\\SQLEXPRESS;Database=BankDB;Trusted_Connection=True;";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }
    }
}