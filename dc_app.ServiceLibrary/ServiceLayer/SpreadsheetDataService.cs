using ServiceLibrary.Entities;
using ServiceLibrary.RepositoryLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dc_app.ServiceLibrary.ServiceLayer;

public interface ISpreadsheetDataService
{
    Task<int> CreateSpreadsheetData(string dynamicTableName, List<string> col_ids, List<string> col_types);
    Task<IEnumerable<dynamic>> ReadAllCellData(uint id);
    Task<IEnumerable<object>> ReadCellDataPaginated(uint id, uint pageStart, uint perPage);
    Task<IEnumerable<dynamic>> ReadCellDataByCol(uint id, int[] col_ids);
    Task<int> UpdateCellDataSingle(string tableName, uint id_p, string col_name, string new_value);
    Task<int> InsertCellDataBulk(string tableName, List<List<object>> rows);
    Task<int> InsertCopyFromTable(string source_table, string destination_table);
    Task<int> DeleteSpreadsheetData(string dynamicTableName);
    Task<int> AlterTableAddColumn(string tableName, List<ColumnConfig> columnConfigs);
}

public class SpreadsheetDataService: ISpreadsheetDataService
{
    private readonly ISpreadsheetDataRepo _spreadshDynamicRepo;

    public SpreadsheetDataService(ISpreadsheetDataRepo spreadshDynamicRepo)
    {
        _spreadshDynamicRepo = spreadshDynamicRepo;
    }

    public async Task<int> CreateSpreadsheetData(string dynamicTableName, List<string> col_ids, List<string> col_types)
    {
        return await _spreadshDynamicRepo.CreateTableAsync(dynamicTableName, col_ids, col_types);
    }

    public async Task<IEnumerable<dynamic>> ReadAllCellData(uint id)
    {
        return await _spreadshDynamicRepo.SelectAllAsync(id);
    }

    public async Task<IEnumerable<object>> ReadCellDataPaginated(uint id, uint pageStart, uint perPage)
    {
        return await _spreadshDynamicRepo.SelectPaginatedAsync(id, pageStart, perPage);
    }

    public async Task<IEnumerable<dynamic>> ReadCellDataByCol(uint id, int[] col_ids)
    {
        return await _spreadshDynamicRepo.SelectEntireCols(id, col_ids);
    }

    public async Task<int> UpdateCellDataSingle(string tableName, uint id_p, string col_name, string new_value)
    {
        return await _spreadshDynamicRepo.UpdateSingleAsync(tableName, id_p, col_name, new_value);
    }

    public async Task<int> InsertCellDataBulk(string tableName, List<List<object>> rows)
    {
        return await _spreadshDynamicRepo.InsertBulkAsync(tableName, rows); 
    }

    public async Task<int> InsertCopyFromTable(string source_table, string destination_table)
    {
        return await _spreadshDynamicRepo.InsertCopyFromTableAsync(source_table, destination_table);
    }

    public async Task<int> DeleteSpreadsheetData(string dynamicTableName)
    {
        return await _spreadshDynamicRepo.DropTableAsync(dynamicTableName);
    }

    public async Task<int> AlterTableAddColumn(string tableName, List<ColumnConfig> columnConfigs)
    {
        return await _spreadshDynamicRepo.AlterTableAddColumn(tableName, columnConfigs);
    }
}
