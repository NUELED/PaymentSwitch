using Dapper;
using System.Data;

namespace PaymentSwitch.Data.Abstraction
{
    public interface IDapperRepository
    {
        Task<T?> GetAsync<T>(string query, DynamicParameters sp_params, CommandType commandType = CommandType.StoredProcedure);
        Task<List<T>> GetAllAsync<T>(string query, DynamicParameters sp_params, CommandType commandType = CommandType.StoredProcedure);
        Task<(List<T>, V)> GetAllAsync<T, V>(string query, DynamicParameters sp_params, CommandType commandType = CommandType.StoredProcedure);
        Task<T> Execute_sp<T>(string query, DynamicParameters sp_params, CommandType commandType = CommandType.StoredProcedure);
        T Execute_Sp<T>(string query, DynamicParameters sp_params, CommandType commandType = CommandType.StoredProcedure);
        Task<List<T>> GetAllAsync<T>(string query, CommandType commandType = CommandType.StoredProcedure);
        Task<List<T>> GetAllAsync2<T>(string query, CommandType commandType = CommandType.StoredProcedure);
        Task<List<T>> InsertBulk<T>(string procedureName, List<DynamicParameters> parametersList, CommandType commandType = CommandType.StoredProcedure);
        Task<int> ExecuteSqlAsync(string sql, DynamicParameters parameters);
        Task<T> QuerySingleAsync<T>(string sql, DynamicParameters parameters);
        Task<IEnumerable<T>> QueryListAsync<T>(string sql, DynamicParameters parameters);
    }
}
