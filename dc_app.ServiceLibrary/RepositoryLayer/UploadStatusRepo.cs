using dc_app.ServiceLibrary.Entities;
using Microsoft.Data.SqlClient;
using ServiceLibrary.Entities;
using Dapper;

namespace dc_app.ServiceLibrary.RepositoryLayer;

public interface IUploadStatusRepo
{
    Task<UploadStatusEntity?> SelectAsync(string uploadId);
    Task<int> InsertAsync(UploadStatusEntity uploadStatusEntity);
    Task<int> UpdateAsync(UploadStatusEntity uploadStatusEntity);
    Task<int> DeleteAsync(UploadStatusEntity uploadStatusEntity);
}

public class UploadStatusRepo:IUploadStatusRepo
{
    public async Task<UploadStatusEntity?> SelectAsync(string uploadId)
    {
        using (var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            var query = await _connection.QuerySingleOrDefaultAsync<UploadStatusEntity>($"SELECT * FROM UploadStatus WHERE uploadId = '{uploadId}';");

            return query;
        }
    }
    public async Task<int> InsertAsync(UploadStatusEntity uploadStatusEntity)
    {
        await using (var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            await _connection.OpenAsync();
            await using var transaction = await _connection.BeginTransactionAsync();

            string executionString = String.Format(
                $@"
                INSERT INTO UploadStatus 
                (uploadId, status, value_percent) 
                VALUES ('{uploadStatusEntity.uploadId}', '{uploadStatusEntity.status}', {uploadStatusEntity.value_percent});"
            );

            int rowsAffected = await _connection.ExecuteAsync(executionString, transaction: transaction);

            transaction.Commit();

            return rowsAffected;
        }
    }
    public async Task<int> UpdateAsync(UploadStatusEntity uploadStatusEntity)
    {
        await using (var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            await _connection.OpenAsync();
            await using var transaction = await _connection.BeginTransactionAsync();

            string executionString = String.Format(
                $@"
                UPDATE UploadStatus 
                SET status = '{uploadStatusEntity.status}', value_percent = {uploadStatusEntity.value_percent}, location = '{uploadStatusEntity.location}'
                WHERE uploadId = '{uploadStatusEntity.uploadId}';"
            );

            int rowsAffected = await _connection.ExecuteAsync(executionString, transaction: transaction);

            Console.WriteLine("updateAsync rowsAffected: " + rowsAffected);

            transaction.Commit();

            return rowsAffected;
        }
    }
    public async Task<int> DeleteAsync(UploadStatusEntity uploadStatusEntity)
    {
        await using (var _connection = new SqlConnection(SqlConnectionFactory.GetConnection().ConnectionString))
        {
            await _connection.OpenAsync();
            await using var transaction = await _connection.BeginTransactionAsync();

            string executionString = String.Format(
                $@"
                DELETE UploadStatus
                WHERE uploadId = '{uploadStatusEntity.uploadId}';"
            );

            int rowsAffected = await _connection.ExecuteAsync(executionString, transaction: transaction);

            Console.WriteLine("updateAsync rowsAffected: " + rowsAffected);

            transaction.Commit();

            return rowsAffected;
        }
    }
}
