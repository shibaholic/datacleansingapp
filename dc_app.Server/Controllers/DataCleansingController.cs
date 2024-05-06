using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using dc_app.ServiceLibrary.ServiceLayer;
using ServiceLibrary.Entities;
using static Dapper.SqlMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace dc_app.Server.Controllers;

[ApiController]
[Route("api/datacleanse")]
[Authorize(Policy = "MustBeUser")]
public class DataCleansingController : ControllerBase
{
    private readonly IAuthHelperService _authHelperService;
    private readonly ISpreadsheetConfigService _spreadsheetConfigService;
    private readonly ISpreadsheetCreateDeleteService _spreadsheetCreateDeleteService;
    private readonly IDataCleansingService _dataCleansingService;
    private readonly UserManager<IdentityUser> _userManager;

    public DataCleansingController(IAuthHelperService authHelperService,
        ISpreadsheetConfigService spreadsheetConfigService,
        ISpreadsheetCreateDeleteService spreadsheetCreateDeleteService,
        IDataCleansingService dataCleansingService,
        UserManager<IdentityUser> userManager)
    {
        _authHelperService = authHelperService;
        _spreadsheetConfigService = spreadsheetConfigService;
        _spreadsheetCreateDeleteService = spreadsheetCreateDeleteService;
        _dataCleansingService = dataCleansingService;
        _userManager = userManager;
    }

    public class DeduplicateResult
    {
        public bool Success { get; set; }
        public string? NewSpreadsheetId { get; set; }

        public DeduplicateResult(bool _success)
        {
            Success = _success;
        }
        public DeduplicateResult(bool _success, string _newSpreadsheetId)
        {
            Success = _success;
            NewSpreadsheetId = _newSpreadsheetId;
        }
    }


    [HttpPost("{url_id}/deduplicate")] // api/dataclean/{url_id}/deduplicate
    public async Task<IActionResult> Deduplicate([FromRoute, BindRequired] string url_id)
    {
        // TODO: do a bunch of validations

        uint? spreadsheetId = await _authHelperService.ConvertUrlId_CheckAuth(url_id, this.User);
        if (spreadsheetId == null) return StatusCode(404);

        // read bodyFormData
        var formCollection = await HttpContext.Request.ReadFormAsync();
        int[] dedup_col_ids = formCollection["dedup_col_ids"].Select(int.Parse).ToArray();
        Console.WriteLine("dedup_col_ids:");
        foreach (var item in dedup_col_ids)
        {
            Console.Write(item.ToString() + " ");
        }
        Console.WriteLine();
        int[] assoc_col_ids = formCollection["assoc_col_ids"].Select(int.Parse).ToArray();
        Console.WriteLine("assoc_col_ids:");
        foreach (var item in assoc_col_ids)
        {
            Console.Write(item.ToString() + " ");
        }
        Console.WriteLine();

        var result = await _dataCleansingService.DeduplicateAlgorithm((uint)spreadsheetId, dedup_col_ids, assoc_col_ids);
        // convert results
        List<List<object>> results = new List<List<object>>();
        for (int i = 0; i < result.Count; i++)
        {
            var row = new List<object>();
            foreach (var item in result[i])
            {
                row.Add(item);
            }
            results.Add(row);
        }

        // get spreadsheet config and create new spreadsheet name and col_name_web based on it
        SpreadsheetConfig spreadshMeta = await _spreadsheetConfigService.ReadSpreadsheetConfig((uint)spreadsheetId);
        string spreadsheetName = spreadshMeta.name + " (Deduplicated)";
        List<ColumnConfig> columnConfigs = (await _spreadsheetConfigService.ReadColumnConfig((uint)spreadsheetId)).ToList();

        string dedup_col_name = "";
        foreach (var col_web_name in columnConfigs.Where(columnConfig => dedup_col_ids.Contains(columnConfig.col_id)).Select(columnConfig => columnConfig.col_name_web))
        {
            dedup_col_name += col_web_name + "+";
        }
        dedup_col_name = dedup_col_name.Substring(0, dedup_col_name.Length - 1);

        // assoc col names
        List<string> assoc_col_names = new List<string>();
        foreach (var col_web_name in columnConfigs.Where(columnConfig => assoc_col_ids.Contains(columnConfig.col_id)).Select(columnConfig => columnConfig.col_name_web))
        {
            assoc_col_names.Add(col_web_name);
        }

        // get usr_id_guid
        System.Security.Claims.ClaimsPrincipal currentUser = this.User;
        var usr_id_guid = new Guid(_userManager.GetUserId(currentUser));

        // create a new spreadsheet with the deduplicated results
        Console.WriteLine("new deduplicated spreadsheet is: " + spreadsheetName);
        var newSpreadshMeta = await _spreadsheetCreateDeleteService.CreateDeduplicatedSpreadsheet(spreadsheetName, dedup_col_name, assoc_col_names, results, usr_id_guid);

        // now return a json object that redirects to a new tab with the deduplicated spreadsheet

        return StatusCode(200, new DeduplicateResult(true, newSpreadshMeta.url_id.ToString()));
    }

    [HttpPost("{url_id}/addreviewcol")]
    public async Task<IActionResult> AddReviewColumns([FromRoute] string url_id)
    {
        uint? spreadsheetId = await _authHelperService.ConvertUrlId_CheckAuth(url_id, this.User);
        if (spreadsheetId == null) return StatusCode(404);

        int rowsAffected = await _dataCleansingService.AddReviewColumns((uint)spreadsheetId);
        // returns ok, which on the client side will trigger a refresh so that data with new columns are refreshed

        return StatusCode(200);
    }
}
