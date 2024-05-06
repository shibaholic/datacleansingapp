using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Dapper;

namespace dc_app.ServiceLibrary.RepositoryLayer;

public interface IUserRepository
{
    Task<IdentityResult> CreateAsync(IdentityUser user);
    Task<IdentityResult> DeleteAsync(IdentityUser user);
    Task<IdentityUser> FindByIdAsync(Guid userId);
    Task<IdentityUser> FindByNameAsync(string userName);
}


public class UserRepo : IUserRepository
{
    #region createuser
    public async Task<IdentityResult> CreateAsync(IdentityUser user)
    {
        using (var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            await _connection.OpenAsync();

            string confirmed = "false";
            string sql = "INSERT INTO UserAccount (id, username, email, email_confirmed, password_hash)" +
            $"VALUES (@Id, @UserName, @Email, '{confirmed}', @PasswordHash);";

            Console.WriteLine(sql);

            int rows = await _connection.ExecuteAsync(sql, new
            {
                Id=new Guid(user.Id),
                user.UserName,
                user.Email,
                user.PasswordHash
            });

            if (rows > 0)
            {
                return IdentityResult.Success;
            }
            return IdentityResult.Failed(new IdentityError { Description = $"Could not insert user {user.Email}." });
        }
    }
    #endregion

    public async Task<IdentityResult> DeleteAsync(IdentityUser user)
    {
        using (var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            await _connection.OpenAsync();
            string sql = "DELETE FROM UserAccount WHERE Id = @Id";
            int rows = await _connection.ExecuteAsync(sql, new { user.Id });

            if (rows > 0)
            {
                return IdentityResult.Success;
            }
            return IdentityResult.Failed(new IdentityError { Description = $"Could not delete user {user.Email}." });
        }
    }


    public async Task<IdentityUser> FindByIdAsync(Guid userId)
    {
        using (var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            string sql = "SELECT * " +
                    "FROM UserAccount " +
                    "WHERE Id = @Id;";

            dynamic? temp = await _connection.QuerySingleOrDefaultAsync<dynamic>(sql, new
            {
                Id = userId
            });

            if (temp != null)
            {
                IdentityUser result = new IdentityUser
                {
                    Id = temp.id.ToString(),
                    UserName = temp.username,
                    Email = temp.email,
                    EmailConfirmed = temp.email_confirmed,
                    PasswordHash = temp.password_hash
                };

                return result;
            }
            return null;
        }
    }


    public async Task<IdentityUser?> FindByNameAsync(string userName)
    {
        using (var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            string sql = "SELECT * " +
                    "FROM UserAccount " +
                    "WHERE UserName = @UserName;";

            dynamic? temp = await _connection.QueryFirstOrDefaultAsync<dynamic>(sql, new
            {
                UserName = userName
            });

            if(temp != null)
            {
                IdentityUser result = new IdentityUser
                {
                    Id = temp.id.ToString(),
                    UserName = temp.username,
                    Email = temp.email,
                    EmailConfirmed = temp.email_confirmed,
                    PasswordHash = temp.password_hash
                };

                return result;
            }

            return null;
        }
    }
}