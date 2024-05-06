using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;

namespace dc_app.ServiceLibrary.RepositoryLayer;

public static class SqlConnectionFactory
{
    private static string _connectionString;

    public static void SetConfig(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public static IDbConnection GetConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public class ConnectionResult
    {
        public bool success;
        public string message;

        public ConnectionResult(bool _success, string _message)
        {
            success = _success;
            message = _message;
        }
    }

    public static async Task<ConnectionResult> TestConnection()
    {
        try
        {
            using (var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
            {
                await _connection.OpenAsync();
                var one = await _connection.ExecuteAsync("SELECT 1");
            }

        } catch (SqlException e) when (e.IsTransient)
        {
            Console.WriteLine("Sql Error IsTransient: " + e.Message);
            return new ConnectionResult(false, "Server Error. Sometimes the database needs 1 minute to wake up from sleep.");
        } catch (SqlException e)
        {
            Console.WriteLine("Sql Error unknown: " + e.Message);
            return new ConnectionResult(false, "Server Error.");
        }

        Console.WriteLine("Successfully connected to the database.");
        return new ConnectionResult(true, "Successfully connected to the database.");
    }
}
