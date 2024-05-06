using Dapper;
using dc_app.ServiceLibrary.RepositoryLayer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using ServiceLibrary.Entities;

namespace ServiceLibrary.RepositoryLayer;

public interface ISpreadsheetConfigRepo
{
    /*Task<IEnumerable<SpreadshMeta>> SelectAllAsync();*/
    Task<SpreadsheetConfig> SelectAsync(uint id);
    Task<uint?> SelectIdByUrlId(string url_id);
    Task<uint> InsertAsync(SpreadsheetConfig entities);
    Task<int> UpdateAsync(SpreadsheetConfig entity);
    Task<int> DeleteAsync(uint id);
}

public class SpreadsheetConfigRepo:ISpreadsheetConfigRepo
{
    public async Task<SpreadsheetConfig> SelectAsync(uint id)
    {
        using(var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            var query = await _connection.QuerySingleOrDefaultAsync<SpreadsheetConfig>($"SELECT * FROM SpreadsheetConfig WHERE id = {id};");
        
            return query; 
        }
    }

    public async Task<uint?> SelectIdByUrlId(string url_id)
    {
        await using(var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString)) 
        {

            uint? result = await _connection.QuerySingleOrDefaultAsync<uint?>(
                @$"
                SELECT id FROM SpreadsheetConfig
                WHERE url_id = '{url_id}';
                "
            );

            return result;
        }
    }

    public async Task<uint> InsertAsync(SpreadsheetConfig entity)
    {
        await using(var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString)) 
        {
            await _connection.OpenAsync();
            await using var transaction = await _connection.BeginTransactionAsync();
        
            var row_id = await _connection.QuerySingleAsync<uint>(
                @"INSERT INTO SpreadsheetConfig (name, url_id, creation_date, type_spreadsheet) 
                OUTPUT INSERTED.ID
                VALUES (@name,@url_id,@creation_date,@type_spreadsheet)",
                new
                {
                    entity.name,
                    entity.url_id,
                    entity.creation_date,
                    entity.type_spreadsheet
                }, transaction:transaction);

            // add any 1 to M relations here that also need to be generated.

            transaction.Commit();

            return row_id;
        }
    }

    public async Task<int> UpdateAsync(SpreadsheetConfig entity)
    {
        await using(var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString)) 
        {
            await _connection.OpenAsync();
            await using var transaction = await _connection.BeginTransactionAsync();

            var rowsAffected = await _connection.ExecuteAsync(
                @$"UPDATE SpreadsheetConfig
                SET name = @name, url_id = @url_id, creation_date = @creation_date, type_spreadsheet = @type_spreadsheet, dynamic_table_name = @dynamic_table_name, total = @total
                WHERE id = {entity.id};",
                new
                {
                    entity.name,
                    entity.url_id,
                    entity.creation_date,
                    entity.type_spreadsheet,
                    entity.dynamic_table_name,
                    entity.total
                }, transaction: transaction);

            transaction.Commit();

            return rowsAffected;
        }
    }
    public async Task<int> DeleteAsync(uint id)
    {
        await using(var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString)) 
        {
            await _connection.OpenAsync();
            await using var transaction = await _connection.BeginTransactionAsync();

            String executionString = String.Format(@$"DELETE FROM SpreadsheetConfig WHERE id = {id};");

            int rowsAffected = await _connection.ExecuteAsync(executionString, transaction: transaction);

            transaction.Commit();

            return rowsAffected;
        }
    }
}

public interface IColumnConfigRepo
{
    Task<IEnumerable<ColumnConfig>> SelectByIdAsync(uint id);
    Task<int> InsertAsync(ColumnConfig entity);
    Task<int> DeleteAsync(uint id);
}

public class ColumnConfigRepo : IColumnConfigRepo
{

    public async Task<IEnumerable<ColumnConfig>> SelectByIdAsync(uint id)
    {
        await using(var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString)) {

        var queryArray = (await _connection.QueryAsync<ColumnConfig>(
            $@"
            SELECT * FROM ColumnConfig
            WHERE spr_id = {id};
            "
        )).ToList();

        return queryArray;
        }
    }

    public async Task<int> InsertAsync(ColumnConfig entity)
    {
        await using(var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString)) 
        {
            await _connection.OpenAsync();
            await using var transaction = await _connection.BeginTransactionAsync();

            string executionString = String.Format(
                $@"
                INSERT INTO ColumnConfig 
                (spr_id, col_id, col_order, col_name_web, col_type) 
                VALUES ({entity.spr_id},{entity.col_id},{entity.col_order},'{entity.col_name_web}','{entity.col_type}')"
            );

            //Console.WriteLine("hasCol xS: " + executionString);

            var rowsAffected = await _connection.ExecuteAsync(executionString, transaction: transaction);

            transaction.Commit();

            return rowsAffected;
        }
    }

    public async Task<int> DeleteAsync(uint id)
    {
        await using (var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            await _connection.OpenAsync();
            await using var transaction = await _connection.BeginTransactionAsync();

            string executionString = String.Format($"DELETE FROM ColumnConfig WHERE spr_id = {id};");

            int rowsAffected = await _connection.ExecuteAsync(executionString, transaction: transaction);

            transaction.Commit();

            return rowsAffected;
        }
    }
}

public interface IUserHasSpreadsheetRepo
{
    Task<IEnumerable<SpreadsheetConfig>> SelectMetaByUsrId(Guid usr_id);
    Task<UserHasSpreadsheet?> SelectValidAsync(Guid usr_id, uint spr_id);
    Task<IEnumerable<UserHasSpreadsheet>> SelectUsersBySprId(uint spr_id);
    Task<UserHasSpreadsheet?> SelectUserHasSpreadsheetBySprId(uint spr_id, Guid usr_id);
    Task<int> InsertAsync(UserHasSpreadsheet userHasSpreadsheet);
    Task<int> DeleteAsync(uint spr_id);
}

public class userHasSpreadsheetRepo : IUserHasSpreadsheetRepo
{
    public async Task<IEnumerable<SpreadsheetConfig>> SelectMetaByUsrId(Guid usr_id)
    {
        await using (var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            var queryArray = (await _connection.QueryAsync<SpreadsheetConfig>(
                $@"
                SELECT * FROM SpreadsheetConfig
                INNER JOIN UserHasSpreadsheet ON UserHasSpreadsheet.spr_id = SpreadsheetConfig.id
                WHERE usr_id = '{usr_id}';
            "
            )).ToList();

            return queryArray;
        }
    }
    public async Task<UserHasSpreadsheet?> SelectValidAsync(Guid usr_id, uint spr_id)
    {
        await using (var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            UserHasSpreadsheet? result = (await _connection.QuerySingleOrDefaultAsync<UserHasSpreadsheet>(
                $@"
                SELECT TOP(1) * FROM UserHasSpreadsheet
                WHERE usr_id = '{usr_id}' AND spr_id = {spr_id};"
            ));

            return result;
        }
    }

    public async Task<IEnumerable<UserHasSpreadsheet>> SelectUsersBySprId(uint spr_id)
    {
        await using (var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            string sql = @$"SELECT usr_id, username, spr_id, permission
                           FROM UserHasSpreadsheet
                           INNER JOIN UserAccount ON UserAccount.id = UserHasSpreadsheet.usr_id
                           WHERE UserHasSpreadsheet.spr_id = {spr_id};";

            var result = await _connection.QueryAsync<UserHasSpreadsheet>(sql);

            return result;
        }
    }

    public async Task<UserHasSpreadsheet?> SelectUserHasSpreadsheetBySprId(uint spr_id, Guid usr_id)
    {
        await using (var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            string sql = @$"SELECT usr_id, username, spr_id, permission
                           FROM UserHasSpreadsheet
                           INNER JOIN UserAccount ON UserAccount.id = UserHasSpreadsheet.usr_id
                           WHERE UserHasSpreadsheet.spr_id = {spr_id} AND UserHasSpreadsheet.usr_id = '{usr_id}';";

            UserHasSpreadsheet? result = await _connection.QuerySingleOrDefaultAsync<UserHasSpreadsheet?>(sql);

            return result;
        }
    }

    public async Task<int> InsertAsync(UserHasSpreadsheet userHasSpreadsheet)
    {
        await using (var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            await _connection.OpenAsync();
            await using var transaction = await _connection.BeginTransactionAsync();

            string executionString = String.Format(
                $@"
                INSERT INTO UserHasSpreadsheet 
                (usr_id, spr_id, permission) 
                VALUES ('{userHasSpreadsheet.usr_id}',{userHasSpreadsheet.spr_id}, '{userHasSpreadsheet.permission}');"
            );

            int rowsAffected = await _connection.ExecuteAsync(executionString, transaction: transaction);

            transaction.Commit();

            return rowsAffected;
        }
    }

    public async Task<int> DeleteAsync(uint spr_id)
    {
        await using (var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            await _connection.OpenAsync();
            await using var transaction = await _connection.BeginTransactionAsync();

            string executionString = String.Format($"DELETE FROM UserHasSpreadsheet WHERE spr_id = {spr_id};");

            int rowsAffected = await _connection.ExecuteAsync(executionString, transaction: transaction);

            transaction.Commit();

            return rowsAffected;
        }
    }
}