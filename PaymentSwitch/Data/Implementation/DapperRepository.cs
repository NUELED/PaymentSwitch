using Dapper;
using Microsoft.Data.SqlClient;
using PaymentSwitch.Data.Abstraction;
using System.Data;

namespace PaymentSwitch.Data.Implementation
{
    public class DapperRepository : IDapperRepository
    {
        private readonly ILogger<DapperRepository> _logger;
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        public DapperRepository(ILogger<DapperRepository> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection")!;
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        #region Stored Procedures   
        public async Task<T?> GetAsync<T>(string query, DynamicParameters sp_params, CommandType commandType = CommandType.StoredProcedure)
        {
            await using var dbConnection = CreateConnection();
            await dbConnection.OpenAsync();

            using var transaction = await dbConnection.BeginTransactionAsync();

            try
            {
                var result = await dbConnection.QueryFirstOrDefaultAsync<T>(query, sp_params, transaction, commandType: commandType);
                await transaction.CommitAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Repo} GetAsync method generated an error", typeof(DapperRepository));
                await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                await dbConnection.CloseAsync();
            }
        }
        public async Task<List<T>> GetAllAsync<T>(string query, DynamicParameters sp_params, CommandType commandType = CommandType.StoredProcedure)
        {
            await using var dbConnection = CreateConnection();
            await dbConnection.OpenAsync();

            using var transaction = await dbConnection.BeginTransactionAsync();

            try
            {
                var result = await dbConnection.QueryAsync<T>(query, sp_params, transaction, commandType: commandType);
                await transaction.CommitAsync();
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Repo} GetAllAsync method generated an error", typeof(DapperRepository));
                await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                await dbConnection.CloseAsync();
            }
        }
        public async Task<(List<T>, V)> GetAllAsync<T, V>(string query, DynamicParameters sp_params, CommandType commandType = CommandType.StoredProcedure)
        {
            using var dbConnection = CreateConnection();
            await dbConnection.OpenAsync();

            try
            {
                var multi = await dbConnection.QueryMultipleAsync(query, sp_params, commandType: commandType);
                var records = await multi.ReadAsync<T>();
                var paginationData = await multi.ReadFirstAsync<V>();
                return (records.ToList(), paginationData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Repo} GetAllAsync method generated an error", typeof(DapperRepository));
                throw;
            }
            finally
            {
                await dbConnection.CloseAsync();
            }
        }
        public async Task<T> Execute_sp<T>(string query, DynamicParameters sp_params, CommandType commandType = CommandType.StoredProcedure)
        {
            T result;

            await using var dbConnection = CreateConnection();
            await dbConnection.OpenAsync();

            using var transaction = await dbConnection.BeginTransactionAsync();

            try
            {
                await dbConnection.QueryAsync<T>(query, sp_params, transaction, commandType: commandType);
                result = sp_params.Get<T>("Result");
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Repo} Execute_sp method generated an error", typeof(DapperRepository));
                await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                await dbConnection.CloseAsync();
            }

            return result;
        }
        public T Execute_Sp<T>(string query, DynamicParameters sp_params, CommandType commandType = CommandType.StoredProcedure)
        {
            T result;

            using var dbConnection = CreateConnection();

            dbConnection.Open();

            using var transaction = dbConnection.BeginTransaction();

            try
            {
                dbConnection.Query<T>(query, sp_params, transaction, commandType: commandType);
                result = sp_params.Get<T>("Result");
                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                dbConnection.Close();
            }
            return result;
        }
        public async Task<List<T>> GetAllAsync<T>(string query, CommandType commandType = CommandType.StoredProcedure)
        {
            await using var dbConnection = CreateConnection();
            await dbConnection.OpenAsync();

            using var transaction = await dbConnection.BeginTransactionAsync();

            try
            {
                var result = await dbConnection.QueryAsync<T>(query, transaction, commandType: commandType);
                await transaction.CommitAsync();
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Repo} GetAllAsync method generated an error", typeof(DapperRepository));
                await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                await dbConnection.CloseAsync();
            }
        }
        public async Task<List<T>> GetAllAsync2<T>(string query, CommandType commandType = CommandType.StoredProcedure)
        {
            await using var dbConnection = CreateConnection();  // Assuming CreateConnection returns a valid connection
            await dbConnection.OpenAsync();

            try
            {
                // No transaction needed for simple SELECT operations like this
                var result = await dbConnection.QueryAsync<T>(query, commandType: commandType);
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Repo} GetAllAsync method generated an error", typeof(DapperRepository));
                throw;
            }
            finally
            {
                await dbConnection.CloseAsync();
            }
        }
        public async Task<List<T>> InsertBulk<T>(string procedureName, List<DynamicParameters> parametersList, CommandType commandType = CommandType.StoredProcedure)
        {
            var result = new List<T>(); // Initialize result as a new List
            await using var dbConnection = CreateConnection();

            try
            {
                await dbConnection.OpenAsync();

                foreach (var parameters in parametersList)
                {
                    dbConnection.Query<T>(procedureName, parameters, commandType: commandType);
                    var res = parameters.Get<T>("Result");
                    if (result == null) // Assuming affectedRows == 0 means failure
                    {
                        _logger.LogWarning("InsertBulk: Insertion failed for a set of parameters.");
                        return result; // Stop further execution and return failure
                    }
                    result.Add(res);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Repo} InsertBulk method generated an error", typeof(DapperRepository));
                throw;
            }
            finally
            {
                await dbConnection.CloseAsync();
            }
            return result;
        }
       #endregion


        #region SQL Queries
        public async Task<int> ExecuteSqlAsync(string sql, DynamicParameters parameters)
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var affectedRows = await connection.ExecuteAsync(sql, parameters, transaction);
                await transaction.CommitAsync();
                return affectedRows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL: {SQL}", sql);
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<T> QuerySingleAsync<T>(string sql, DynamicParameters parameters)
        {
            await using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
        }

        public async Task<IEnumerable<T>> QueryListAsync<T>(string sql, DynamicParameters parameters)
        {
            await using var connection = CreateConnection();
            return await connection.QueryAsync<T>(sql, parameters);
        }
        #endregion
    }
}
