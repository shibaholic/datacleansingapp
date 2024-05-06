using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using dc_app.ServiceLibrary.ServiceLayer;
using Azure.Core;
using dc_app.ServiceLibrary.Entities;
using DocumentFormat.OpenXml.Office2016.Excel;
using dc_app.ServiceLibrary.Utilities;
using System.Data;
using ServiceLibrary.Entities;
using dc_app.Server.Controllers;
using ClosedXML.Excel;

namespace main.Controllers;

public class CreateUploadResult
{
    public bool success { get; set; }
    public string? error { get; set; }
    public string? uploadLocation { get; set; }
    public string? uploadStatus { get; set; }
}

/// <summary>
/// controller for upload large file
/// </summary>
[ApiController]
[Route("api/spreadsheet")]
[Authorize(Policy = "MustBeUser")]
public class FileUploadController : ControllerBase
{
    private readonly ISpreadsheetCreateDeleteService _createService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IAuthHelperService _authHelperService;
    private readonly ISpreadsheetFileService _fileService;
    private readonly ISpreadsheetConfigService _configService;

    public FileUploadController(ISpreadsheetCreateDeleteService createService, UserManager<IdentityUser> userManager, IAuthHelperService authHelperService, ISpreadsheetFileService fileService, ISpreadsheetConfigService configService)
    {
        _createService = createService;
        _userManager = userManager;
        _authHelperService = authHelperService;
        _fileService = fileService;
        _configService = configService;
    }

    [HttpPost("{type}")]
    public async Task<IActionResult> CreateSpreadsheet([FromRoute] string type)
    {
        if (type == "demo")
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            var usr_id = _userManager.GetUserId(currentUser);
            var usr_id_guid = new Guid(usr_id);

            int status = await _createService.CreateDemoSpreadsheet(usr_id_guid);

            if (status == 0)
            {
                // return ok
                return Ok();
            }

            return StatusCode(500);

        }
        else if (type == "upload")
        {
            // will allow the client to upload a spreadsheet

            // 1. create a new uploadStatus
            var uploadStatus = await _createService.CreateUploadStatus();
            if(uploadStatus == null)
            return StatusCode(500, new CreateUploadResult { success = false, error = "Server error encountered" });
            
            string uploadId = uploadStatus.uploadId;
            Console.WriteLine("uploadId: " + uploadId);

            // 2. return 201 Created with uploadLocation and trackLocation
            return StatusCode(201, new CreateUploadResult { success = true, uploadLocation = $"/api/spreadsheet/uploadfile/{uploadId}", uploadStatus = $"/api/spreadsheet/upload/{uploadId}/status" });

        }
        else
        {
            return StatusCode(400, new CreateUploadResult { success = false, error = $"{type} is not a valid spreadsheet creation type." });
        }
    }

    [HttpPost("uploadfile/{uploadId}")] // /api/spreadsheet/upload/{uploadId}
    [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadSpreadsheetFile()
    {
        // cannot use model binding since it uses middleware that changes the HttpContext.Request which will read through the entire request body.
        // because the entire request body is then read, then the multipartreader will not be able to read it.
        // because of that, there would be a duplicate route error if /upload/{uploadId} was used, so it is changed to /uploadFile/{uploadId}

        string uploadId = (string)RouteData.Values["uploadId"];

        // 1. validate uploadId
        var uploadStatus = await _createService.GetUploadStatus(uploadId);

        if (uploadStatus is null)
        {
            Console.WriteLine("uploadSpreadsheetFile no uploadId");
            return StatusCode(404);
        }

        /*uploadStatus.status = "uploading";
        if ((await _createService.UpdateUploadStatus(uploadStatus)) != 1)
        {
            Console.WriteLine("first updateUploadStatus failed!");
        }*/

        // TODO: use authorization to check if the same client that requested /spreadsheet/upload is the same that is doing this endpoint

        var request = HttpContext.Request;

        // validation of Content-Type
        // 1. first, it must be a form-data request
        // 2. a boundary should be found in the Content-Type
        if (!request.HasFormContentType ||
            !MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaTypeHeader) ||
            string.IsNullOrEmpty(mediaTypeHeader.Boundary.Value))
        {
            return new UnsupportedMediaTypeResult();
        }

        var boundary = HeaderUtilities.RemoveQuotes(mediaTypeHeader.Boundary.Value).Value;
        var reader = new MultipartReader(boundary, request.Body);
        var section = await reader.ReadNextSectionAsync();

        // This sample try to get the first file from request and save it
        // Make changes according to your needs in actual use
        while (section != null)
        {
            var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);

            if (hasContentDispositionHeader && 
                contentDisposition.DispositionType.Equals("form-data") &&
                !string.IsNullOrEmpty(contentDisposition.FileName.Value))
            {
                var origFileName = contentDisposition.FileName.Value;

                if (!origFileName.Substring(origFileName.Length - 5, 5).Equals(".xlsx"))
                {
                    await _createService.DeleteUploadStatus(uploadStatus);
                    return BadRequest("File has to be .xlsx file type.");
                }

                var randFileName = Path.GetRandomFileName() + ".xlsx";
                var tmpFilePath = Path.Combine(Path.GetTempPath(), randFileName);
                Console.WriteLine(tmpFilePath);

                using (var targetStream = System.IO.File.Create(tmpFilePath))
                {
                    using (var uploadStream = new UploadStream(section.Body, Request.ContentLength.Value, _createService, uploadStatus))
                    {
                        await uploadStream.CopyToAsync(targetStream);
                    }
                }

                System.Security.Claims.ClaimsPrincipal currentUser = this.User;
                var usr_id = _userManager.GetUserId(currentUser);
                var usr_id_guid = new Guid(usr_id);

                SpreadsheetConfig spreadsheetConfig = await _createService.CreateSpreadsheetFromXMLFile(origFileName, tmpFilePath, usr_id_guid);
                if(spreadsheetConfig == null) 
                {
                    return StatusCode(500);
                }

                // update the updateStatus to completed with location link
                uploadStatus.status = "completed";
                uploadStatus.value_percent = 100;
                uploadStatus.location = "/api/spreadsheet/" + spreadsheetConfig.url_id;
                await _createService.UpdateUploadStatus(uploadStatus);

                // delete the tempfile
                System.IO.File.Delete(tmpFilePath);

                return StatusCode(201);
            }

            section = await reader.ReadNextSectionAsync();
        }

        await _createService.DeleteUploadStatus(uploadStatus);
        // If the code runs to this location, it means that no files have been saved
        return BadRequest("No files data in the request.");
    }

    // returns the status of the uploadId provided
    [HttpGet("upload/{uploadId}/status")] // /api/spreadsheet/upload/{uploadId}/status
    public async Task<IActionResult> GetUploadStatus([FromRoute] string uploadId)
    {
        // use the create delete service to get the status
        UploadStatusEntity uploadStatusEntity = await _createService.GetUploadStatus(uploadId);
        if (uploadStatusEntity == null)
        {
            return StatusCode(404);
        }
        else
        {
            return StatusCode(200, uploadStatusEntity);
        }
    }

    [HttpGet("{url_id}/download")]
    public async Task<IActionResult> GetDownloadSpreadsheet([FromRoute] string url_id)
    {
        Console.WriteLine("download");

        uint? spreadsheetId = await _authHelperService.ConvertUrlId_CheckAuth(url_id, this.User);
        if (spreadsheetId == null) return StatusCode(404);

        IXLWorkbook workbook = await _fileService.GetXLWorksheet((uint)spreadsheetId);

        var spreadsheetConfig = await _configService.ReadSpreadsheetConfig((uint)spreadsheetId);

        Console.WriteLine("spreadsheet downloaded");

        // using memoryStream, the workbook file is not actually stored as a file on the server to be downloaded.
        using (var stream = new MemoryStream())
        {
            // save the workbook to the stream
            workbook.SaveAs(stream);

            // seek to the beginning position
            stream.Position = 0;

            string fileName = spreadsheetConfig.name.Replace(".xlsx", ""); // prevents bug from occuring where there are multiple file types in the name

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}