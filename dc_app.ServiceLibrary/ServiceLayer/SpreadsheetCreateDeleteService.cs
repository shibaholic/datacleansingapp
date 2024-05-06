using ClosedXML.Excel;
using dc_app.ServiceLibrary.Entities;
using dc_app.ServiceLibrary.RepositoryLayer;
using ServiceLibrary.Entities;
using System.Transactions;

using System.Security.Cryptography;
using dc_app.ServiceLibrary.Utilities;

namespace dc_app.ServiceLibrary.ServiceLayer;

public interface ISpreadsheetCreateDeleteService
{
    Task<SpreadsheetConfig?> CreateSpreadsheetFromXMLFile(string origFileName, string filePath, Guid user_id);
    Task<int> CreateDemoSpreadsheet(Guid user_id);
    Task<SpreadsheetConfig> CreateDeduplicatedSpreadsheet(string spreadsheetName, string dedup_col_name, List<string> assoc_col_names, List<List<object>> results, Guid usr_id_guid);  // not very flexible, should be able to handle different col types in the future...
    Task<int> DeleteSpreadsheet(uint id);
    Task<UploadStatusEntity> GetUploadStatus(string uploadId);
    Task<UploadStatusEntity?> CreateUploadStatus();
    Task<int> UpdateUploadStatus(UploadStatusEntity uploadStatusEntity);
    Task<int> DeleteUploadStatus(UploadStatusEntity uploadStatusEntity);
}

public class SpreadsheetCreateDeleteService: ISpreadsheetCreateDeleteService
{
    private readonly ISpreadsheetConfigService _spreadsheetConfigService;
    private readonly ISpreadsheetDataService _spreadsheetDataService;
    private readonly IUploadStatusRepo _uploadStatusRepo;

    public SpreadsheetCreateDeleteService(ISpreadsheetConfigService spreadsheetConfigService, ISpreadsheetDataService spreadsheetDataService, IUploadStatusRepo uploadStatusRepo)
    {
        _spreadsheetConfigService = spreadsheetConfigService;
        _spreadsheetDataService = spreadsheetDataService;
        _uploadStatusRepo = uploadStatusRepo;
    }

    public async Task<SpreadsheetConfig?> CreateSpreadsheetFromXMLFile(string origFileName, string filePath, Guid user_id)
    {
        // TODO: Sanitize the file name for XSS and SQL use.

        // 2.3 generate the data and instantiate the SpreadsheetMetadata entity
        var spreadsheetConfig = new SpreadsheetConfig()
        {
            // id is null initially. It will be updated in a roundtrip fashion to get the identity value created by the database. 
            name = origFileName,
            url_id = CryptographyUtility.Generate11CharRandomBase64UrlString(),
            creation_date = DateTimeOffset.UtcNow,
            type_spreadsheet = "user_created"
            // dynamic_table_name is null when initially inserted, because we need to update it with the identity value.
            // there is no other practical way of getting the identity value before this insert.
        };
        uint identity = await _spreadsheetConfigService.InsertSpreadsheetConfig(spreadsheetConfig);
        string dynamicTableName = "dynamic_" + identity;
        Console.WriteLine("identity: " + identity + " database name: " + dynamicTableName);
        // before the UpdateAsync was here. Now it's been moved to the bottom, so we can also get the 
        // total count of rows.

        // start api progress meter (not now)

        // 5. start reading the data in the spreadsheet

        // 5.1 read the column names
        if (!File.Exists(filePath))
        {
            return null; // file does not exist
        }

        var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheet(1);

        int last_cn = ws.LastColumnUsed().ColumnNumber();

        var headerColRng = ws.Range(1, 1, 1, last_cn);

        var col_names_web = new List<string>();
        var col_ids = new List<string>();
        var col_types = new List<string>();
        var col_id_counter = 1;
        foreach (var cell in headerColRng.Cells())
        {
            col_names_web.Add(cell.Value.ToString());
            col_types.Add(cell.DataType.ToString());
            col_ids.Add($"col{col_id_counter++}");
        }

        Console.WriteLine("web: " + String.Join(", ", col_names_web));
        Console.WriteLine("col_id: " + String.Join(", ", col_ids));

        col_types = TypeConversionXML.convertTypesList(col_types);
        Console.WriteLine("types: " + String.Join(", ", col_types));

        // 5.2 INSERT into SpreadsheetHasColumns

        int rowsAffected = 0;
        using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            for (int i = 0; i < col_names_web.Count; i++)
            {
                // 2.3 generate the data and instantiate the SpreadsheetMetadata entity
                rowsAffected += await _spreadsheetConfigService.InsertColumnConfig(new ColumnConfig()
                {
                    spr_id = identity,
                    col_id = i + 1,
                    col_order = i + 1,
                    col_name_web = col_names_web[i],
                    col_type = col_types[i],
                });
            }
            scope.Complete();
        }

        Console.WriteLine("hasCol rA: " + rowsAffected.ToString());

        // 5.3 create the dynamic table
        var output = await _spreadsheetDataService.CreateSpreadsheetData(dynamicTableName, col_ids, col_types);

        // 5.4 from each row each cell goes in a Object[], which all together become Object[][]
        int last_rn = ws.LastRowUsed().RowNumber();
        int row_header = 1;
        Console.WriteLine("last_rn: " + last_rn);
        Console.WriteLine("last_cn: " + last_cn);
        List<List<object>> data_rows = new List<List<object>>(); // [last_rn - row_header]

        for (int i = 0; i < last_rn - row_header; i++)
        {
            List<object> one_row = new List<object>();
            for (int j = 0; j < last_cn; j++)
            {
                one_row.Add(ws.Cell(i + 1 + row_header, j + 1).Value.ToString());
            }
            data_rows.Add(one_row);
        }

        // 5.5 INSERT into dynamic_##
        var rowsAffected2 = await _spreadsheetDataService.InsertCellDataBulk(dynamicTableName, data_rows);

        Console.WriteLine("rA2: " + rowsAffected2);

        // update the spreadsheet metadata
        spreadsheetConfig.id = identity;
        spreadsheetConfig.dynamic_table_name = dynamicTableName;
        spreadsheetConfig.total = last_rn - row_header;
        int rowsAffected1 = await _spreadsheetConfigService.UpdateSpreadsheetConfig(spreadsheetConfig);

        // insert into userHasSpreadsh m-m table

        UserHasSpreadsheet userHasSpreadsh = new UserHasSpreadsheet()
        {
            usr_id = user_id,
            spr_id = identity,
            permission = "owner"
        };

        rowsAffected1 += await _spreadsheetConfigService.InsertUserHasSpreadsheet(userHasSpreadsh);

        if (rowsAffected1 != 2)
        {
            return null; // bad update
        }

        return spreadsheetConfig;
    }

    public async Task<int> CreateDemoSpreadsheet(Guid user_id)
    {
        var spreadsheetMetadataEntity = new SpreadsheetConfig()
        {
            // id is null initially. It will be updated in a roundtrip fashion to get the identity value created by the database. 
            name = "demo",
            url_id = CryptographyUtility.Generate11CharRandomBase64UrlString(),
            creation_date = DateTimeOffset.UtcNow,
            type_spreadsheet = "user_created"
            // dynamic_table_name is null initially
        };
        uint identity = await _spreadsheetConfigService.InsertSpreadsheetConfig(spreadsheetMetadataEntity);
        string dynamicTableName = "dynamic_" + identity;
        Console.WriteLine("identity: " + identity + "database name: " + dynamicTableName);

        var demo_colConfs = await _spreadsheetConfigService.ReadColumnConfig(1);
        var col_names_web = demo_colConfs.Select(colConf => colConf.col_name_web).ToList();
        var col_ids = new List<string>() { };
        var col_types = new List<string>();
        for (int i = 1; i < col_names_web.Count + 1; i++)
        {
            col_ids.Add($"col{i}");
            col_types.Add("Text");
        }
        col_types = TypeConversionXML.convertTypesList(col_types);

        Console.WriteLine("web: " + String.Join(", ", col_names_web));
        Console.WriteLine("col_id: " + String.Join(", ", col_ids));
        Console.WriteLine("types: " + String.Join(", ", col_types));


        // 5.2 INSERT into ColumnConfig

        int rowsAffected = 0;
        using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            for (int i = 0; i < col_names_web.Count; i++)
            {
                rowsAffected += await _spreadsheetConfigService.InsertColumnConfig(new ColumnConfig()
                {
                    spr_id = identity,
                    col_id = i + 1,
                    col_order = i + 1,
                    col_name_web = col_names_web[i],
                    col_type = col_types[i],
                });
            }
            scope.Complete();
        }

        Console.WriteLine("hasCol rA: " + rowsAffected.ToString());


        // 5.3 create the dynamic table
        var output = await _spreadsheetDataService.CreateSpreadsheetData(dynamicTableName, col_ids, col_types);

        // copy the demo table to the new dynamic table
        int copy_paste_rowsAffected = await _spreadsheetDataService.InsertCopyFromTable("demo_table", dynamicTableName);

        Console.WriteLine($"InsertCopyTable rowsAffected: {copy_paste_rowsAffected}");

        // update the spreadsheet metadata
        spreadsheetMetadataEntity.id = identity;
        spreadsheetMetadataEntity.dynamic_table_name = dynamicTableName;
        spreadsheetMetadataEntity.total = 16;
        int meta_rowsAffected = await _spreadsheetConfigService.UpdateSpreadsheetConfig(spreadsheetMetadataEntity);

        // insert into userHasSpreadsh m-m table
        UserHasSpreadsheet userHasSpreadsh = new UserHasSpreadsheet()
        {
            usr_id = user_id,
            spr_id = identity,
            permission = "owner"
        };

        int userhas_rowsAffected = await _spreadsheetConfigService.InsertUserHasSpreadsheet(userHasSpreadsh);

        if (userhas_rowsAffected != 1 && meta_rowsAffected != 1)
        {
            return -2; // bad update
        }

        return 0;
    }

    public async Task<SpreadsheetConfig> CreateDeduplicatedSpreadsheet(string spreadsheetName, string dedup_col_name, List<string> assoc_col_names, List<List<object>> results, Guid usr_id_guid) 
    {
        var spreadsheetConfig = new SpreadsheetConfig()
        {
            // id is null initially. It will be updated in a roundtrip fashion to get the identity value created by the database. 
            name = spreadsheetName,
            url_id = CryptographyUtility.Generate11CharRandomBase64UrlString(),
            creation_date = DateTimeOffset.UtcNow,
            type_spreadsheet = "user_created"
            // dynamic_table_name is null initially
        };

        // insert into spreadsheet config
        uint identity = await _spreadsheetConfigService.InsertSpreadsheetConfig(spreadsheetConfig);
        string dynamicTableName = "dynamic_" + identity;
        Console.WriteLine("identity: " + identity + " database name: " + dynamicTableName);

        string col_name_web = dedup_col_name;
        var col_ids = new List<string>() { "col1" };
        var col_types = new List<string>() { "stringMax" };
        

        // insert dedup_col's column config
        int colConfRowsAffected = 0;
        colConfRowsAffected += await _spreadsheetConfigService.InsertColumnConfig(new ColumnConfig()
        {
            spr_id = identity,
            col_id = 1,
            col_order = 1,
            col_name_web = col_name_web,
            col_type = col_types[0],
        });
        // insert assoc_col's column config
        for(int i = 0; i < assoc_col_names.Count; i++)
        {
            colConfRowsAffected += await _spreadsheetConfigService.InsertColumnConfig(new ColumnConfig()
            {
                spr_id = identity,
                col_id = i + 2,
                col_order = i + 2,
                col_name_web = assoc_col_names[i],
                col_type = col_types[0],
            });
            col_ids.Add("col" + (i + 2));
            col_types.Add("stringMax");
        }

        Console.WriteLine("colConfRowsAffected: " + colConfRowsAffected);

        // create the dynamic table
        var something = await _spreadsheetDataService.CreateSpreadsheetData(dynamicTableName, col_ids, col_types);

        Console.WriteLine("create spreadsheet something: " + something);

        // update the spreadsheet config with dynamic table name
        spreadsheetConfig.id = identity;
        spreadsheetConfig.dynamic_table_name = dynamicTableName;
        spreadsheetConfig.total = results.Count; // TODO: should be able to set to null. This total is upadted after the insert.
        int sprConfRowsAffected = await _spreadsheetConfigService.UpdateSpreadsheetConfig(spreadsheetConfig);

        // insert the data
        var insertDataResult = await _spreadsheetDataService.InsertCellDataBulk(spreadsheetConfig.dynamic_table_name, results);

        // insert to userHasSpreadsheet
        UserHasSpreadsheet userHasSpreadsh = new UserHasSpreadsheet()
        {
            usr_id = usr_id_guid,
            spr_id = spreadsheetConfig.id,
            permission = "owner"
        };

        int userhas_rowsAffected = await _spreadsheetConfigService.InsertUserHasSpreadsheet(userHasSpreadsh);

        int totalRowAffected = colConfRowsAffected + something + sprConfRowsAffected;

        return spreadsheetConfig;
    }

    public async Task<int> DeleteSpreadsheet(uint id)
    {

        int rowsAffected = 0;
        var dynamicTableName = "dynamic_" + id;

        using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            // take into account the constraints and which order they should be deleted
            // delete the SpreadsheetHasColumns rows WHERE id = {id}
            rowsAffected += await _spreadsheetConfigService.DeleteColumnConfig(id);
            // delete the UserHasSpreadsh rows where spr_id = id
            rowsAffected += await _spreadsheetConfigService.DeleteUserHasSpreadsheet(id);
            // delete the SpreadsheetMetadata row WHERE id = {id}
            rowsAffected += await _spreadsheetConfigService.DeleteSpreadsheetConfig(id);
            // delete the dynamic table 'dynamic_#'
            var something = await _spreadsheetDataService.DeleteSpreadsheetData(dynamicTableName);

            scope.Complete();
        }

        return rowsAffected;
    }

    public async Task<UploadStatusEntity?> GetUploadStatus(string uploadId)
    {
        return await _uploadStatusRepo.SelectAsync(uploadId);
    }
    public async Task<UploadStatusEntity?> CreateUploadStatus()
    {
        // TODO: should I check in the very rare case that uploadId collides with another uploadId?
        var uploadStatusEntity = new UploadStatusEntity()
        {
            uploadId = CryptographyUtility.Generate11CharRandomBase64UrlString(),
            status = "waiting for upload",
            value_percent = 0
        };
        if((await _uploadStatusRepo.InsertAsync(uploadStatusEntity)) != 1) {
            return null; // database failure
        }
        return uploadStatusEntity;
    }
    public async Task<int> UpdateUploadStatus(UploadStatusEntity uploadStatusEntity)
    {
        return await _uploadStatusRepo.UpdateAsync(uploadStatusEntity);
    }
    public async Task<int> DeleteUploadStatus(UploadStatusEntity uploadStatusEntity)
    {
        return await _uploadStatusRepo.DeleteAsync(uploadStatusEntity);
    }
}

