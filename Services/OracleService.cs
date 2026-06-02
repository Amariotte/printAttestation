using Oracle.ManagedDataAccess.Client;

namespace OracleApi.Services
{
    public interface IOracleService
    {
        Task<bool> TestConnectionAsync();
        Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query);
        Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query, Dictionary<string, object> parameters);
        Task<int> ExecuteNonQueryAsync(string query, Dictionary<string, object>? parameters = null);
    }

    public class OracleService : IOracleService
    {
        private readonly string _connectionString;
        private readonly ILogger<OracleService> _logger;
        private const int MaxRetryAttempts = 3;
        private const int CommandTimeoutSeconds = 300;

        public OracleService(IConfiguration configuration, ILogger<OracleService> logger)
        {
            _connectionString = configuration.GetConnectionString("OracleConnection") 
                ?? throw new ArgumentNullException("OracleConnection string not found");
            _logger = logger;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();
                _logger.LogInformation("Connection to Oracle database successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Oracle database");
                return false;
            }
        }

        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query)
        {
            return await ExecuteQueryAsync(query, new Dictionary<string, object>());
        }

        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query, Dictionary<string, object> parameters)
        {
            return await RetryOnNetworkErrorAsync(async () =>
            {
                var results = new List<Dictionary<string, object>>();

                using var connection = new OracleConnection(_connectionString);
                try
                {
                    await connection.OpenAsync();

                    using var command = new OracleCommand(query, connection);
                    command.CommandTimeout = CommandTimeoutSeconds;
                    command.InitialLONGFetchSize = -1;
                    command.FetchSize = 1024 * 1024;

                    // Ajouter les paramètres si fournis
                    if (parameters != null && parameters.Count > 0)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.Add(new OracleParameter(param.Key, param.Value ?? DBNull.Value));
                        }
                    }

                    using var reader = await command.ExecuteReaderAsync();

                    var columnNames = new string[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        columnNames[i] = reader.GetName(i);
                    }

                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[columnNames[i]] = reader.IsDBNull(i) ? null! : reader.GetValue(i);
                        }
                        results.Add(row);
                    }

                    _logger.LogInformation("Requete executée avec succès. Lignes retournées: {Count}", results.Count);
                }
                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        await connection.CloseAsync();
                    }
                }

                return results;
            }, query);
        }

        public async Task<int> ExecuteNonQueryAsync(string query, Dictionary<string, object>? parameters = null)
        {
            return await RetryOnNetworkErrorAsync(async () =>
            {
                using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new OracleCommand(query, connection);
                command.CommandTimeout = CommandTimeoutSeconds;

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.Add(new OracleParameter(param.Key, param.Value));
                    }
                }

                int rowsAffected = await command.ExecuteNonQueryAsync();

                await connection.CloseAsync();

                _logger.LogInformation("Requete executée avec succès. Lignes affectées: {Count}", rowsAffected);
                return rowsAffected;
            }, query);
        }

        private async Task<T> RetryOnNetworkErrorAsync<T>(Func<Task<T>> operation, string query)
        {
            int attempt = 0;
            while (true)
            {
                attempt++;
                try
                {
                    return await operation();
                }
                catch (OracleException oex) when (IsNetworkError(oex) && attempt < MaxRetryAttempts)
                {
                    _logger.LogWarning(oex, "Tentative {Attempt}/{MaxAttempts} - Erreur réseau Oracle détectée (Code: {ErrorCode}). Nouvelle tentative...", 
                        attempt, MaxRetryAttempts, oex.Number);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de l'exécution de la requête : {Query}", query);
                    throw;
                }
            }
        }

        private bool IsNetworkError(OracleException oex)
        {
            return oex.Number switch
            {
                3113 => true, // ORA-03113: end-of-file on communication channel
                3114 => true, // ORA-03114: not connected to ORACLE
                1012 => true, // ORA-01012: not logged on
                28 => true,   // ORA-00028: your session has been killed
                1089 => true, // ORA-01089: immediate shutdown in progress
                1090 => true, // ORA-01090: shutdown in progress
                12170 => true, // TNS: Connect timeout occurred
                12535 => true, // TNS: operation timed out
                12537 => true, // TNS: connection closed
                12541 => true, // TNS: no listener
                12571 => true, // TNS: packet writer failure
                _ => false
            };
        }
    }
}
