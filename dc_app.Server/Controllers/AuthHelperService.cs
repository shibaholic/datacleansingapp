using dc_app.ServiceLibrary.ServiceLayer;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace dc_app.Server.Controllers;

public interface IAuthHelperService
{
    Task<uint?> ConvertUrlId_CheckAuth(string url_id, ClaimsPrincipal currentUser);
}

public class AuthHelperService: IAuthHelperService
{
    private readonly ISpreadsheetConfigService _spreadsheetConfigService;
    private readonly IAuthorizationService _authorizationService;

    public AuthHelperService(ISpreadsheetConfigService spreadsheetConfigService, IAuthorizationService authorizationService)
    {
        _spreadsheetConfigService = spreadsheetConfigService;
        _authorizationService = authorizationService;
    }

    public async Task<uint?> ConvertUrlId_CheckAuth(string url_id, ClaimsPrincipal currentUser)
    {
        uint? spreadsheetId = await _spreadsheetConfigService.ReadIdFromUrlId(url_id);
        if (spreadsheetId == null) return null;

        // authorize if user has permission to view this spreadsheet
        var auth_result = await _authorizationService.AuthorizeAsync(currentUser, (uint)spreadsheetId, "UserHasSpreadshAllowed");
        if (!auth_result.Succeeded) return null;

        return spreadsheetId;
    }
}
