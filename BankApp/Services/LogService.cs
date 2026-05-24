using BankApp.Data;
using System;
using System.Data.SqlClient;

namespace BankApp.Services
{
    public static class LogService
    {
        public static void Log(
            string action,
            string description = "")
        {
            try
            {
                using (SqlConnection conn =
                    DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(@"
INSERT INTO SystemLogs
(
    UserId,
    Action,
    Description
)
VALUES
(
    @userId,
    @action,
    @description
)", conn);

                    cmd.Parameters.AddWithValue(
                        "@userId",
                        Session.CurrentUser?.Id ?? (object)DBNull.Value);

                    cmd.Parameters.AddWithValue(
                        "@action",
                        action);

                    cmd.Parameters.AddWithValue(
                        "@description",
                        description);

                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // чтобы приложение не падало
            }
        }
    }
}