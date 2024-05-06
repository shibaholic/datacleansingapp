
using System.Transactions;
using ServiceLibrary.Entities;
using ServiceLibrary.RepositoryLayer;
using ClosedXML.Excel;
using dc_app.ServiceLibrary.RepositoryLayer;
using Microsoft.AspNetCore.Identity;

namespace dc_app.ServiceLibrary.ServiceLayer;

public static class TypeConversionXML
{
    // ClosedXML type: C# type
    public static readonly Dictionary<string, string> typeConversionDictionary = new Dictionary<string, string> { 
        { "Text", "string" },
        { "TextMax", "stringMax" }
    };

    public static List<string> convertTypesList(List<string> list)
    {
        List<string> result = new List<string>(list.Count());
        for(int i = 0; i < list.Count(); i++)
        {
            string type = list[i];
            string value;
            if(typeConversionDictionary.TryGetValue(type, out value))
            {
                result.Add(value);
            } else {
                // fail to get key
                result[i] = "string"; // string can contain all other types in string format..
            }
        }
        return result;
    }
}


public interface ISpreadsheetConfigService
{
    Task<IEnumerable<SpreadsheetConfig>> ReadSpreadsheetConfigsFromUser(Guid usr_id);
    Task<SpreadsheetConfig> ReadSpreadsheetConfig(uint id);
    Task<IEnumerable<UserHasSpreadsheet>> ReadSpreadsheetUsers(uint id);
    Task<UserHasSpreadsheet?> ReadSpreadsheetUser(uint id, Guid usr_id);
    Task<uint?> ReadIdFromUrlId(string url_id);
    Task<IEnumerable<ColumnConfig>> ReadColumnConfig(uint id);
    Task<uint> InsertSpreadsheetConfig(SpreadsheetConfig spreadsheetConfig);
    Task<int> UpdateSpreadsheetConfig(SpreadsheetConfig spreadsheetConfig);
    Task<int> InsertColumnConfig(ColumnConfig columnConfig);
    Task<int> InsertUserHasSpreadsheet(UserHasSpreadsheet userHasSpreadsh);
    Task<int> DeleteSpreadsheetConfig(uint id);
    Task<int> DeleteColumnConfig(uint id);
    Task<int> DeleteUserHasSpreadsheet(uint id);
}

// method names use the Create Read Update Delete verbs, followed by how and what.
public class SpreadsheetConfigService : ISpreadsheetConfigService
{
    private readonly ISpreadsheetConfigRepo _spreadsheetConfigRepo;
    private readonly IColumnConfigRepo _columnConfigRepo;
    private readonly IUserHasSpreadsheetRepo _userHasSpreadshRepo;

    public SpreadsheetConfigService(ISpreadsheetConfigRepo spreadshMetaRepo, IColumnConfigRepo spreadshColRepo, IUserHasSpreadsheetRepo userHasSpreadshRepo)
    {
        _spreadsheetConfigRepo = spreadshMetaRepo;
        _columnConfigRepo = spreadshColRepo;
        _userHasSpreadshRepo = userHasSpreadshRepo;
    }

    public async Task<IEnumerable<SpreadsheetConfig>> ReadSpreadsheetConfigsFromUser(Guid usr_id)
    {
        return await _userHasSpreadshRepo.SelectMetaByUsrId(usr_id);
    }

    public async Task<SpreadsheetConfig> ReadSpreadsheetConfig(uint id)
    {
        return await _spreadsheetConfigRepo.SelectAsync(id);
    }

    public async Task<IEnumerable<UserHasSpreadsheet>> ReadSpreadsheetUsers(uint id)
    {
        return await _userHasSpreadshRepo.SelectUsersBySprId(id);
    }

    public async Task<UserHasSpreadsheet?> ReadSpreadsheetUser(uint id, Guid usr_id)
    {
        return await _userHasSpreadshRepo.SelectUserHasSpreadsheetBySprId(id, usr_id);
    }

    public async Task<uint?> ReadIdFromUrlId(string url_id)
    {
        return await _spreadsheetConfigRepo.SelectIdByUrlId(url_id);
    }

    public async Task<IEnumerable<ColumnConfig>> ReadColumnConfig(uint id)
    {
        return await _columnConfigRepo.SelectByIdAsync(id);
    }

    public async Task<uint> InsertSpreadsheetConfig(SpreadsheetConfig spreadsheetConfig) // returns the IDENTITY of the row that was inserted
    {
        return await _spreadsheetConfigRepo.InsertAsync(spreadsheetConfig);
    }
    public async Task<int> UpdateSpreadsheetConfig(SpreadsheetConfig spreadsheetConfig)
    {
        return await _spreadsheetConfigRepo.UpdateAsync(spreadsheetConfig);
    }

    public async Task<int> InsertColumnConfig(ColumnConfig columnConfig)
    {
        return await _columnConfigRepo.InsertAsync(columnConfig);
    }

    public async Task<int> InsertUserHasSpreadsheet(UserHasSpreadsheet userHasSpreadsh)
    {
        return await _userHasSpreadshRepo.InsertAsync(userHasSpreadsh);
    }

    public async Task<int> DeleteSpreadsheetConfig(uint id) // returns the IDENTITY of the row that was inserted
    {
        return await _spreadsheetConfigRepo.DeleteAsync(id);
    }

    public async Task<int> DeleteColumnConfig(uint id)
    {
        return await _columnConfigRepo.DeleteAsync(id);
    }

    public async Task<int> DeleteUserHasSpreadsheet(uint id)
    {
        return await _userHasSpreadshRepo.DeleteAsync(id);
    }
}
