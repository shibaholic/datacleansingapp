using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using System.Configuration;
using dc_app.ServiceLibrary.ServiceLayer;
using dc_app.ServiceLibrary.RepositoryLayer;

using System.Text.Encodings;

namespace dc_app.ServiceLibrary.Tests;

public class IdentityTests
{
    private IUserStore<IdentityUser> userStore;

    public static byte[] ConvertToByteArray(string str, Encoding encoding)
    {
        return encoding.GetBytes(str);
    }

    public static String ToBinary(Byte[] data)
    {
        return string.Join(" ", data.Select(byt => Convert.ToString(byt, 2).PadLeft(8, '0')));
    }

    public void setup()
    {
        var connectionString = "Server=localhost,1433;User Id=sa;Password=Password!1; MultipleActiveResultSets=true; Encrypt=false";
        var _connection = new SqlConnection(connectionString);
        userStore = new UserService(new UserRepo());
        SqlConnectionFactory.SetConfig(connectionString);
    }

    //[Fact]
    async public void FindById()
    {
        setup();
        
        IdentityUser? result = await userStore.FindByIdAsync("45789a0f-fe3c-4641-a493-b357e88d3696", new CancellationToken());

        Assert.NotNull(result);
    }

    [Fact]
    async public void CreateUser()
    {
        setup();

        IdentityUser user = new IdentityUser("Alpha");
        user.UserName = "alpha1";
        user.Email = "alpha@alpha.com";
        user.EmailConfirmed = false;
        user.PasswordHash = "this_is_supposed_to_be_hashed";

        IdentityResult? result = await userStore.CreateAsync(user, new CancellationToken());

        Assert.NotNull(result);
        Assert.True(result.Succeeded);

        IdentityUser? result2 = await userStore.FindByNameAsync(user.UserName, new CancellationToken());

        Assert.NotNull(result2);
    }
}
