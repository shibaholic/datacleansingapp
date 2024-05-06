using ClosedXML.Excel;
using dc_app.ServiceLibrary.RepositoryLayer;
using DocumentFormat.OpenXml.Spreadsheet;
using ServiceLibrary.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dc_app.ServiceLibrary.ServiceLayer;

public interface ISpreadsheetFileService
{
    Task<IXLWorkbook> GetXLWorksheet(uint spreadsheetId);
}

public class SpreadsheetFileService: ISpreadsheetFileService
{
    private readonly ISpreadsheetConfigService _spreadsheetConfigService;
    private readonly ISpreadsheetDataService _spreadsheetDataService;

    public SpreadsheetFileService(ISpreadsheetConfigService spreadsheetConfigService, ISpreadsheetDataService spreadsheetDataService)
    {
        _spreadsheetConfigService = spreadsheetConfigService;
        _spreadsheetDataService = spreadsheetDataService;
    }

    public async Task<IXLWorkbook> GetXLWorksheet(uint spreadsheetId)
    {
        var workbook = new XLWorkbook();
        
        var worksheet = workbook.Worksheets.Add("Sheet1");

        List<ColumnConfig> columnConfigs = (await _spreadsheetConfigService.ReadColumnConfig(spreadsheetId)).ToList();
        int number_of_columns = columnConfigs.Count();
        for(int x = 1; x < number_of_columns + 1; x++)
        {
            worksheet.Cell(1, x).Value = columnConfigs[x - 1].col_name_web;
        }

        List<dynamic> data = (await _spreadsheetDataService.ReadAllCellData(spreadsheetId)).ToList();
        for(int y = 2; y < data.Count(); y++)
        {
            int x = 1;
            var row = data[y-2];
            foreach(var kvp in row)
            {
                if(kvp.Key.Contains("col")) // is a data col, not ID_P
                {
                    worksheet.Cell(y, x).Value = kvp.Value;
                    x++;
                }
            }
        }

        return workbook;
    }
}
