using AutoMapper;
using dc_app.ServiceLibrary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using dc_app.ServiceLibrary.ServiceLayer;
using ServiceLibrary.Entities;
using System.ComponentModel.DataAnnotations;
using dc_app.Server.Controllers;
using dc_app.ServiceLibrary.Utilities;
using Microsoft.AspNetCore.Http.HttpResults;

namespace main.Controllers;

[ApiController]
[Route("api/spreadsheet")] // api/spreadsheet
[Authorize(Policy = "MustBeUser")]
public class SpreadsheetController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly IAuthHelperService _authHelperService;
    private readonly ISpreadsheetConfigService _spreadsheetConfigService;
    private readonly ISpreadsheetDataService _spreadsheetDataService;
    private readonly ISpreadsheetCreateDeleteService _spreadsheetCreateDeleteService;
    private readonly UserManager<IdentityUser> _userManager;

    public SpreadsheetController(
        IMapper mapper,
        IAuthHelperService authHelperService,
        ISpreadsheetConfigService spreadsheetConfigService,
        ISpreadsheetDataService spreadsheetDataService,
        ISpreadsheetCreateDeleteService spreadsheetCreateDeleteService,
        UserManager<IdentityUser> userManager)
    {
        _mapper = mapper;
        _authHelperService = authHelperService;
        _spreadsheetConfigService = spreadsheetConfigService;
        _spreadsheetDataService = spreadsheetDataService;
        _spreadsheetCreateDeleteService = spreadsheetCreateDeleteService;
        _userManager = userManager;
    }

    [HttpGet] // api/spreadsheet
    public async Task<IActionResult> GetAllSpreadsheetConfigsFromUser()
    {
        // get the user id guid
        System.Security.Claims.ClaimsPrincipal currentUser = this.User;
        var usr_id = _userManager.GetUserId(currentUser);
        var usr_id_guid = Guid.Parse(usr_id);

        IEnumerable<SpreadsheetConfig> result = await _spreadsheetConfigService.ReadSpreadsheetConfigsFromUser(usr_id_guid);

        IEnumerable<SpreadsheetConfigDto> result_dto = _mapper.Map<IEnumerable<SpreadsheetConfig>, IEnumerable<SpreadsheetConfigDto>>(result);

        return Ok(result_dto);
    }

/*    static int draft = 0;
    static int newguy = 0;*/

    // GET: /api/sync
    [HttpGet("/api/sync/{type}/{id?}")]
    public async Task<IActionResult> GetDashboardSync([FromRoute] string type, [FromRoute] string? id)
    {
        string channel;

        System.Security.Claims.ClaimsPrincipal currentUser = this.User;
        var usr_id = _userManager.GetUserId(currentUser);
        
        if(type == "user")
        {
/*            if (usr_id.StartsWith("0a")) Console.WriteLine("lp user draft " + ++draft);
            if (usr_id.StartsWith("83")) Console.WriteLine("lp user newguy" + ++newguy);*/
            channel = usr_id;
        } else if(type == "spreadsheet")
        {
/*            if (usr_id.StartsWith("0a")) Console.WriteLine("lp spr draft " + ++draft);
            if (usr_id.StartsWith("83")) Console.WriteLine("lp spr newguy" + ++newguy);*/
            uint? spreadsheetId = await _authHelperService.ConvertUrlId_CheckAuth(id, this.User);
            if (spreadsheetId == null) return StatusCode(404);

            channel = id;
        } else
        {
            return StatusCode(404);
        }

        LongPolling lp = new LongPolling(channel);
        LongPollMessage message = await lp.WaitAsync();

        if(message == null)
        {
            return new ObjectResult(new { message = "long poll timeout" });
        }

        return Ok(message);
    }

    [HttpPost("{url_id}/share/{username}")] // api /spreadsheet/{url_id}/share/{username}
    public async Task<IActionResult> PostShareSpreadsheet([FromRoute] string url_id, [FromRoute] string username)
    {
        uint? spreadsheetId = await _authHelperService.ConvertUrlId_CheckAuth(url_id, this.User);
        if (spreadsheetId == null) return StatusCode(404);

        IdentityUser? identityUserGuest = await _userManager.FindByNameAsync(username);
        if (identityUserGuest == null) return new ObjectResult(new { success=false, message="User not found."});

        // first check if the userHasSpreadsheet relation already exists
        UserHasSpreadsheet? maybeAlreadyHasPermission = await _spreadsheetConfigService.ReadSpreadsheetUser((uint)spreadsheetId, Guid.Parse(identityUserGuest.Id));
        if (maybeAlreadyHasPermission != null) return new ObjectResult(new { success = false, message = "User already has permission." });

        int rowsAffected = await _spreadsheetConfigService.InsertUserHasSpreadsheet(new UserHasSpreadsheet
        {
            usr_id = Guid.Parse(identityUserGuest.Id),
            spr_id = (uint)spreadsheetId,
            permission = "editor"
        });

        if(rowsAffected != 1)
        {
            throw new Exception("PostShareSpreadsheet InsertUserHasSpreadsheet bad result");
        }

        Console.WriteLine("shared to " + identityUserGuest.Id);
        // propagate an event to identityUserGuest to refresh their dashboard
        LongPolling.Publish(identityUserGuest.Id, new LongPollMessage { message="dashboard_sync" });
        return new ObjectResult(new {success=true});
    }

    [HttpGet("{url_id}/users")] // api/spreadsheet/{url_id}/users
    public async Task<IActionResult> GetSpreadsheetUsers([FromRoute] string url_id)
    {
        uint? spreadsheetId = await _authHelperService.ConvertUrlId_CheckAuth(url_id, this.User);
        if (spreadsheetId == null) return StatusCode(404);

        var result = await _spreadsheetConfigService.ReadSpreadsheetUsers((uint)spreadsheetId);

        var result_dto = _mapper.Map<IEnumerable<UserHasSpreadsheet>, IEnumerable<UserHasSpreadsheetDto>>(result);

        return Ok(result_dto);
    }

    [HttpGet("{url_id}")] // api/spreadsheet/{id}
    public async Task<IActionResult> GetSpreadsheetConfigById([FromRoute] string url_id)
    {
        uint? spreadsheetId = await _authHelperService.ConvertUrlId_CheckAuth(url_id, this.User);
        if(spreadsheetId == null) return StatusCode(404);
        
        var result = await _spreadsheetConfigService.ReadSpreadsheetConfig((uint)spreadsheetId);

        SpreadsheetConfigDto result_dto = _mapper.Map<SpreadsheetConfig, SpreadsheetConfigDto>(result);
        if (result_dto != null)
        {
            return Ok(result_dto);
        }
        return StatusCode(500); // error occured...
    }

    [HttpDelete("{url_id}")] // api/spreadsheet/{id}
    public async Task<IActionResult> DeleteSpreadsheetById([FromRoute] string url_id)
    {
        uint? spreadsheetId = await _authHelperService.ConvertUrlId_CheckAuth(url_id, this.User);
        if (spreadsheetId == null) return StatusCode(404);

        // get userHasSpreads of this spreadsheet before it is deleted in DeleteSpreadsheet.
        List<UserHasSpreadsheet> userHasSpreads = (await _spreadsheetConfigService.ReadSpreadsheetUsers((uint)spreadsheetId)).ToList();

        var result = await _spreadsheetCreateDeleteService.DeleteSpreadsheet((uint)spreadsheetId);
        // assume that all spreadsheets have at least 1 column, so that would be rowsAffected is at least 2
        if (result >= 2)
        {
            // propagate change to others
            foreach (var userHasSpread in userHasSpreads)
            {
                LongPolling.Publish(userHasSpread.usr_id.ToString(), new LongPollMessage { message = "dashboard_sync" });
            }

            return NoContent();
        }
        else
        {
            return StatusCode(500); // error occured...
        }
    }

    [HttpGet("{url_id}/columns")] // api/spreadsheet/{url_id}/columns
    public async Task<IActionResult> GetColumnConfigById([FromRoute] string url_id)
    {
        uint? spreadsheetId = await _authHelperService.ConvertUrlId_CheckAuth(url_id, this.User);
        if (spreadsheetId == null) return StatusCode(404);

        var result = await _spreadsheetConfigService.ReadColumnConfig((uint)spreadsheetId.Value);
        IEnumerable<ColumnConfigDto> result_dto = _mapper.Map<IEnumerable<ColumnConfig>, IEnumerable<ColumnConfigDto>>(result);
        return result == null ? NotFound() : Ok(result_dto);
    }

    [HttpGet("{url_id}/cells")] // api/spreadsheet/{url_id}/cells?pageStart={pageStart}&perPage={perPage}
    public async Task<IActionResult> GetSpreadsheetDataPaginated(
        [FromRoute] string url_id, 
        [FromQuery, BindRequired, Range(1,UInt32.MaxValue)] uint pageStart, 
        [FromQuery, BindRequired, Range(1,UInt32.MaxValue)] uint perPage)
    {
        uint? spreadsheetId = await _authHelperService.ConvertUrlId_CheckAuth(url_id, this.User);
        if (spreadsheetId == null) return StatusCode(404);

        var spreadsheetConfig = await _spreadsheetConfigService.ReadSpreadsheetConfig((uint)spreadsheetId.Value);
        if (spreadsheetConfig == null) return NotFound();

        var cell_data = await _spreadsheetDataService.ReadCellDataPaginated((uint)spreadsheetId.Value, pageStart, perPage);

        var result_dto = new
        {
            page = pageStart,
            per_page = Globals.spreadsheetPageSize,
            total = spreadsheetConfig.total,
            total_pages = (uint) spreadsheetConfig.total / Globals.spreadsheetPageSize + 1,
            data = cell_data
        };

        return Ok(result_dto);
    }
    
    public class PatchCellDataBody
    {
        public uint id_p { get; set; }
        public string col_id { get; set; }
        public string new_value { get; set; }
    }
    // like PUT, but only update a resource partially. Patch is usually not idempotent, but it is here.
    [HttpPatch("{url_id}/cells")] // api/spreadsheet/{url_id}/cells
    public async Task<IActionResult> PatchCellData(
        [FromRoute] string url_id, 
        [FromBody] PatchCellDataBody body)
    {
        uint? spreadsheetId = await _authHelperService.ConvertUrlId_CheckAuth(url_id, this.User);
        if (spreadsheetId == null) return StatusCode(404);

        // 2. Get dynamic spreadsheet name
        // is it inefficient to make to calls to the same table? yes
        SpreadsheetConfig spreadshMeta = await _spreadsheetConfigService.ReadSpreadsheetConfig((uint)spreadsheetId);
        string dynamicSpreadshName = spreadshMeta.dynamic_table_name;

        // 3. UPDATE {dynamic_#} SET {col_name_sql} = {value from body} WHERE ID_P = {id_p}
        int rowsAffected = await _spreadsheetDataService.UpdateCellDataSingle(dynamicSpreadshName, body.id_p, body.col_id, body.new_value);
        if(rowsAffected == 1)
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            var user = await _userManager.GetUserAsync(currentUser);
            var username = user.UserName;

            // propagate change to other users on the spreadsheet
            LongPolling.Publish(url_id, new LongPollMessage { 
                message = username, data = new { 
                    row=body.id_p, 
                    col_id=body.col_id, 
                    value=body.new_value 
                } 
            });

            return Ok();
        } else if(rowsAffected > 1)
        {
            return StatusCode(500, "Error but still works, but maybe something wrong on server.");
        } else {
            return StatusCode(500, "Error: Could not update cell value.");
        }
    }

    [HttpPut("{url_id}/name/{spreadsheet_name}")]
    public async Task<IActionResult> PutSpreadsheetName([FromRoute] string url_id, [FromRoute] string spreadsheet_name)
    {
        uint? spreadsheetId = await _authHelperService.ConvertUrlId_CheckAuth(url_id, this.User);
        if (spreadsheetId == null) return StatusCode(404);

        SpreadsheetConfig spreadsheetConfig = await _spreadsheetConfigService.ReadSpreadsheetConfig((uint)spreadsheetId);

        spreadsheetConfig.name = spreadsheet_name;

        int rowsAffected = await _spreadsheetConfigService.UpdateSpreadsheetConfig(spreadsheetConfig);

        if(rowsAffected == 1)
        {
            // propagate change to others
            List<UserHasSpreadsheet> userHasSpreads = (await _spreadsheetConfigService.ReadSpreadsheetUsers((uint)spreadsheetId)).ToList();
            foreach(var userHasSpread in userHasSpreads)
            {
                LongPolling.Publish(userHasSpread.usr_id.ToString(), new LongPollMessage { message = "dashboard_sync" });
            }

            return Ok(spreadsheetConfig);
        }
        return StatusCode(500);
    }
}