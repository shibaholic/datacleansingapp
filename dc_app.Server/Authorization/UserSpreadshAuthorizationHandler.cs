using Microsoft.AspNetCore.Authorization;
using ServiceLibrary.Entities;
using ServiceLibrary.RepositoryLayer;

namespace dc_app.Server.Authorization;

public class UserHasSpreadshAllowed : IAuthorizationRequirement
{

}

public class UserSpreadshAuthorizationHandler: AuthorizationHandler<UserHasSpreadshAllowed, uint>
{
    private readonly IUserHasSpreadsheetRepo _userHasSpreadshRepo;

    public UserSpreadshAuthorizationHandler(IUserHasSpreadsheetRepo userHasSpreadshRepo)
    {
        _userHasSpreadshRepo = userHasSpreadshRepo;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, UserHasSpreadshAllowed requirement, uint spreadsheetId)
    {
        /*
        await Console.Out.WriteLineAsync("handle requirement");
        var claims = context.User.Claims;
        foreach (var claim in claims)
        {
            await Console.Out.WriteLineAsync(claim.ToString());
            await Console.Out.WriteLineAsync(claim.Type);
            await Console.Out.WriteLineAsync(claim.Value);
        }
        */
        var guid_str = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value.ToString();
        //await Console.Out.WriteLineAsync(guid_str + " " + guid_str.ToString());
        var usr_id = new Guid(guid_str);
        //await Console.Out.WriteLineAsync(usr_id.ToString());
        UserHasSpreadsheet? result = await _userHasSpreadshRepo.SelectValidAsync(usr_id, spreadsheetId);

        if (result == null)
        {
            //await Console.Out.WriteLineAsync("fail");
            context.Fail();
            return;
        }
        //await Console.Out.WriteLineAsync("success");
        context.Succeed(requirement);

        return;
    }
}
