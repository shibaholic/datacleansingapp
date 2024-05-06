using Dapper;
using dc_app.ServiceLibrary;
using dc_app.ServiceLibrary.RepositoryLayer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ServiceLibrary.Entities;

namespace ServiceLibrary.RepositoryLayer;

public static class TypeConversionSQL
{
    // ClosedXML type: C# type
    public static readonly Dictionary<string, string> typeConversionDictionary = new Dictionary<string, string> {
        { "string", "NVARCHAR(MAX) NOT NULL" },
        { "stringMax", "NVARCHAR(MAX) DEFAULT('') NOT NULL" },
        { "boolean", "BIT DEFAULT(0) NOT NULL" },
        { "integer", "INTEGER" }
    };
    public static List<string> convertTypesList(List<string> list)
    {
        List<string> result = new List<string>(list.Count());
        for (int i = 0; i < list.Count(); i++)
        {
            string type = list[i];
            string value;
            typeConversionDictionary.TryGetValue(type, out value);
            result.Add(value);
        }
        return result;
    }
    public static string convertType(string type)
    {
        string result;
        typeConversionDictionary.TryGetValue(type, out result);
        return result;
    }
}

public interface ISpreadsheetDataRepo // doesn't use models/entitites because of dynamic tables.
{
    Task<int> CreateTableAsync(string tableName, List<string> columnNames, List<string> columnTypes);
    Task<IEnumerable<dynamic>> SelectAllAsync(uint id);
    Task<IEnumerable<object>> SelectPaginatedAsync(uint id, uint pageStart, uint perPage);
    Task<IEnumerable<dynamic>> SelectEntireCols(uint id, int[] col_ids);
    Task<int> InsertBulkAsync(string tableName, List<List<object>> rows);
    Task<int> InsertCopyFromTableAsync(string tableToCopy, string tableToPaste);
    Task<int> UpdateSingleAsync(string tableName, uint id_p, string col_name, string new_value);
    Task<int> DropTableAsync(String dynamic_table_name);
    Task<int> AlterTableAddColumn(string tableName, List<ColumnConfig> columnConfigs);
}

public class SpreadsheetDataRepo:ISpreadsheetDataRepo
{
    public async Task<int> CreateTableAsync(string dynamicTableName, List<string> col_names, List<string> col_types)
    {

        await using(var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString)) 
        {
            await _connection.OpenAsync();
            await using var transaction = await _connection.BeginTransactionAsync();
            var creationString = $"CREATE TABLE {dynamicTableName} ( ID_P INTEGER IDENTITY NOT NULL PRIMARY KEY, ";

            foreach (var (col_name, col_type) in col_names.Zip(col_types))
            {
                // no col_name checks as they are done in the domain level

                // col_type checks
                string col_type2 = TypeConversionSQL.convertType(col_type);
                string columnString = $"{col_name} {col_type2}, ";
                //Console.WriteLine("colS: " + columnString);
                creationString += columnString;
            }

            creationString += ");";
        
            Console.WriteLine("colS cS: " + creationString);

            var something = await _connection.ExecuteAsync(creationString, transaction: transaction);

            //Console.WriteLine("something: " + something);

            transaction.Commit();

            return something;
        }
    }

    public async Task<IEnumerable<dynamic>> SelectAllAsync(uint id)
    {
        await using (var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            return await _connection.QueryAsync($"SELECT * FROM dynamic_{id};");
        }
    }

    public async Task<IEnumerable<object>> SelectPaginatedAsync(uint id, uint pageStart, uint perPage)
    {
        await using(var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString)) 
        {

            if (perPage > 100)
            {
                perPage = 100;
            }

            uint indexFrom = (pageStart - 1) * Globals.spreadsheetPageSize + 1;
            uint indexTo = pageStart * Globals.spreadsheetPageSize + 1;

            var result = (await _connection.QueryAsync($"SELECT TOP({perPage}) * FROM dynamic_{id} WHERE ID_P >= {indexFrom} AND ID_P < {indexTo};"));

            return result;
        }
    }

    public async Task<IEnumerable<dynamic>> SelectEntireCols(uint id, int[] col_ids)
    {
        await using(var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString)) 
        {
            var queryString = "SELECT ";
            for(int i = 0; i < col_ids.Length; i++) {
                queryString += "col" + col_ids[i] + ", ";
            }
            queryString = queryString.Substring(0, queryString.Length - 2); // removes last comma-space
            queryString += " FROM " + "dynamic_" + id + ";";

            // await Console.Out.WriteLineAsync(queryString);

            var result = (await _connection.QueryAsync(queryString)); // we want to index through it

            return result;
        }
    }

    public async Task<int> InsertBulkAsync(string tableName, List<List<object>> rows)
    {   
        await using(var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString)) 
        {
            await _connection.OpenAsync();
            await using var transaction = await _connection.BeginTransactionAsync();

            int batch_size = 10;
            int rowsAffected = 0;

            string insertString_base = $"INSERT INTO {tableName} VALUES \n";

            string insertString_batch = insertString_base;

            for (int counter = 0; counter < rows.Count; counter++) // counter starts at 0, because we already pulled all rows from [2-LastRowUsed()] in spreadsheet
            {
                string insertString_row = "(";
                var row = rows[counter];
                for (int index = 0; index < row.Count; index++)
                {
                    object value = row[index];
                    if (value.GetType() == typeof(string))
                    {
                        string value_string = (string)value;
                        value_string = value_string.Replace("'", "''");
                        insertString_row += $"'{value_string}'";
                    }
                    else if (value.GetType() == typeof(int))
                    {
                        insertString_row += $"{value}";
                    }
                    else
                    {
                        Console.WriteLine("bad typeof");
                        insertString_row += $"'{value}'";
                    }
                    if(index != row.Count - 1)
                    {
                        insertString_row += ", " ;
                    }
                }
            
                if ((counter + 1) % batch_size != 0 && counter != rows.Count - 1)
                {
                    insertString_row += "), \n";
                    insertString_batch += insertString_row;
                }
                else // (counter + 1) % batch_size == 0 || counter == rows.Length - 1
                {
                    insertString_row += ");";
                    insertString_batch += insertString_row;
                    rowsAffected += await _connection.ExecuteAsync(insertString_batch, transaction: transaction);
                    insertString_batch = insertString_base;
                }
            }

            transaction.Commit();

            return rowsAffected;
        }
    }

    public async Task<int> InsertCopyFromTableAsync(string source_table, string destination_table)
    {
        await using (var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            await _connection.OpenAsync();
            await using var transaction = await _connection.BeginTransactionAsync();
            int rowsAffected = 0;

            string executionString = $"INSERT INTO {destination_table} SELECT col1, col2, col3, col4, col5, col6 FROM {source_table};";

            rowsAffected += await _connection.ExecuteAsync(executionString, transaction: transaction);
            transaction.Commit();

            return rowsAffected;
        }
    }

    public async Task<int> UpdateSingleAsync(string tableName, uint id_p, string col_name, string new_value)
    {
        await using (var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            await _connection.OpenAsync();
            await using var transaction = await _connection.BeginTransactionAsync();

            new_value = new_value.Replace("'", "''");

            string executionString = $"UPDATE {tableName} SET {col_name} = '{new_value}' WHERE ID_P = {id_p}";

            int rowsAffected = await _connection.ExecuteAsync(executionString, transaction: transaction);

            transaction.Commit();

            return rowsAffected;
        }
        
    }

    public async Task<int> DropTableAsync(string tableName)
    {
        using(var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            await _connection.OpenAsync();
            await using var transaction = await _connection.BeginTransactionAsync();

            string executionString = $"DROP TABLE {tableName};";

            var something = await _connection.ExecuteAsync(executionString, transaction: transaction);

            Console.WriteLine("droptable something: " + something);

            transaction.Commit();

            return 0;
        }
    }

    public async Task<int> AlterTableAddColumn(string tableName, List<ColumnConfig> columnConfigs)
    {
        using (var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            await _connection.OpenAsync();
            await using var transaction = await _connection.BeginTransactionAsync();

            string sql = String.Format($"ALTER TABLE {tableName} ADD ");

            foreach(var columnConfig in columnConfigs)
            {
                sql += "col" + columnConfig.col_id + " " + TypeConversionSQL.convertType(columnConfig.col_type) + ", ";
            }
            sql = sql.Substring(0, sql.Length -2); // remove the last comma and space
            sql += ";";

            Console.WriteLine("sql: " + sql);

            var something = await _connection.ExecuteAsync(sql, transaction: transaction);

            transaction.Commit();

            return something;
        }
    }
}