using System.Data;
using System.Data.Common;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace Database;

public class MySql(string host, int port, string database, string user, string password, ILogger<MySql>? logger = null)
    : IDatabase
{
    private readonly string _connectionString = $"Database={database};Server={host};Port={port};User={user};Password={password};";

    public void Start()
    {
        logger?.LogInformation("Start connection to SQLite database");
        logger?.LogInformation("Connection to SQLite database is successful");
    }

    public void End()
    {
        logger?.LogInformation("Close connection to SQLite database");
        logger?.LogInformation("Connection to SQLite database is closed");
    }

    public async IAsyncEnumerable<(IDictionary<string, object> Fields, string Id)> GetAllRecords(string tableName)
    {
        logger?.LogInformation("[MySQL]: Get all records from table {TableName}", tableName);
        var connection = CreateConnection();

        var request = $"SELECT * FROM {tableName}";

        await connection.OpenAsync();
        await using var command = new MySqlCommand(request, connection);

        logger?.LogInformation("[MySQL]: Request to a database: {Request} (Command: {Command})", request, command);

        await foreach (var item in GetAllRecordsByCommand(command))
            yield return item;

        logger?.LogInformation("[SQLite]: End get all records from table {TableName}", tableName);

        await CloseConnection(connection);
    }

    public async IAsyncEnumerable<(IDictionary<string, object> Fields, string Id)> GetAllRecordsByField<T>(string tableName, string nameField, T field)
    {
        logger?.LogInformation("[MySQL]: Get all records by field from table {TableName} by field {NameField} with value {Field}", tableName, nameField, field);
        var connection = CreateConnection();

        var request = $"SELECT * FROM {tableName} WHERE {nameField} = @field";
        await using var command = new MySqlCommand(request, connection);
        command.Parameters.Add(new MySqlParameter("@field", field));

        await connection.OpenAsync();

        logger?.LogInformation("[MySQL]: Request to a database: {Request} (Command: {Command})", request, command);

        await foreach (var item in GetAllRecordsByCommand(command))
            yield return item;

        logger?.LogInformation("[MySQL]: End get all records by field from table {TableName} by field {NameField} with value {Field}", tableName, nameField, field);

        await CloseConnection(connection);
    }

    public async Task<(IDictionary<string, object> Fields, string Id)> GetRecord<T>(string tableName, string nameField, T field, bool isString = true, Match match= Match.Exact)
    {
        logger?.LogInformation("[MySQL]: Start Get record from table {TableName} by field {NameField} with value {Field}", tableName, nameField, field);
        var connection = CreateConnection();

        if (match == Match.None)
        {
            logger?.LogInformation("[MySQL]: End Get record by match None from table {TableName} by field {NameField} with value {Field}", tableName, nameField, field);
            return await GetRecordInAll(tableName, nameField, field);
        }

        var request = $"SELECT * FROM {tableName} WHERE {nameField} = ";
        const string fieldName = "@field";
        var dct = new Dictionary<string, object>();

        if (match == Match.Partial)
        {
            logger?.LogInformation("[MySQL]: Match is partial");
            request += $"%{fieldName}%";
        }
        else
        {
            logger?.LogInformation("[MySQL]: Match is exact");
            request += fieldName;
        }

        await using var command = new MySqlCommand(request, connection);
        command.Parameters.Add(new MySqlParameter("@field", field));

        logger?.LogInformation("[MySQL]: Request to database: {Request} (Command: {Command})", request, command);

        await connection.OpenAsync();
        var reader = await command.ExecuteReaderAsync();

        logger?.LogInformation("[MySQL]: Start read record by command");

        if (await reader.ReadAsync())
        {
            var values = new object[reader.FieldCount];
            var cnt = reader.GetValues(values);

            for (var i = 0; i < cnt; i++)
                dct.Add(reader.GetName(i), values[i]);
        }

        logger?.LogInformation("[MySQL]: End read record by command");

        await CloseConnection(connection);

        var id = string.Empty;

        if (dct.TryGetValue("Id", out var idObj))
            id = idObj.ToString() ?? string.Empty;

        if (dct.TryGetValue("id", out idObj))
            id = idObj.ToString() ?? string.Empty;

        logger?.LogInformation("[MySQL]: End Get record from table {TableName} by field {NameField} with value {Field}", tableName, nameField, field);

        return (dct, id);
    }

    public async Task<(IDictionary<string, object> Fields, string Id)> GetRecord(string tableName, params SearchField[] fields)
    {
        logger?.LogInformation("[MySQL]: Start Get record from table {TableName} by fields {Fields}", tableName, fields);

        if (fields == null || fields.Length == 0)
        {
            logger?.LogInformation("[MySQL]: End Get record from table {TableName} by fields {Fields} because it is empty", tableName, fields);
            return (new Dictionary<string, object>(), string.Empty);
        }

        var connection = CreateConnection();

        if (fields[0].Match == Match.None)
        {
            logger?.LogInformation("[MySQL]: End Get record by match None from table {TableName} by fields {Fields}", tableName, fields);
            return await GetRecordInAll(tableName, fields[0].NameField, fields[0].Field);
        }

        var request = $"SELECT * FROM {tableName} WHERE ";
        const string fieldName = "@f";

        await using var command = new MySqlCommand(string.Empty, connection);

        for (var i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            request += $"{field.NameField} = ";

            var currFieldName = fieldName + i;

            command.Parameters.Add(new MySqlParameter(currFieldName, field.Field));

            if (field.TypeField == typeof(string))
                currFieldName = $"'{currFieldName}'";

            if (field.Match == Match.Partial)
                request += $"%{currFieldName}%";
            else if (field.Match == Match.Max)
                request += $"(SELECT MAX({field.NameField}) FROM {tableName})";
            else if (field.Match == Match.Min)
                request += $"(SELECT MIN({field.NameField}) FROM {tableName})";
            else
                request += currFieldName;

            if (field.Connection == Connection.NONE)
                break;

            if (i + 1 >= fields.Length)
                continue;

            if (field.Connection == Connection.AND)
                request += " AND ";

            if (field.Connection == Connection.OR)
                request += " OR ";
        }

        command.CommandText = request;

        logger?.LogInformation("[MySQL]: Request to database: {Request} (Command: {Command})", request, command);

        await connection.OpenAsync();
        var reader = await command.ExecuteReaderAsync();

        logger?.LogInformation("[MySQL]: Start read record by command");

        var dct = new Dictionary<string, object>();

        if (await reader.ReadAsync())
        {
            var values = new object[reader.FieldCount];
            var cnt = reader.GetValues(values);

            for (var i = 0; i < cnt; i++)
                dct.Add(reader.GetName(i), values[i]);
        }

        logger?.LogInformation("[MySQL]: End read record by command");

        await CloseConnection(connection);

        var id = string.Empty;

        if (dct.TryGetValue("Id", out var idObj))
            id = idObj.ToString() ?? string.Empty;

        if (dct.TryGetValue("id", out idObj))
            id = idObj.ToString() ?? string.Empty;

        logger?.LogInformation("[MySQL]: End Get record from table {TableName} by fields {Fields}", tableName, fields);

        return (dct, id);
    }

    public async Task<(IDictionary<string, object> Fields, string Id)> GetRecordById(string tableName, string id)
    {
        logger?.LogInformation("[MySQL]: Start Get record by id from table {TableName} by id {Id}", tableName, id);

        if (await RowExists(tableName, "id"))
        {
            logger?.LogInformation("[MySQL]: End Get record by id from table {TableName} by id {Id}", tableName, id);
            return await GetRecord(tableName, "id", id.ToLong());
        }

        if (await RowExists(tableName, "Id"))
        {
            logger?.LogInformation("[MySQL]: End Get record by id from table {TableName} by Id {Id}", tableName, id);
            return await GetRecord(tableName, "Id", id.ToLong());
        }

        logger?.LogError("[MySQL]: Error Get record by id from table {TableName} by id {Id}", tableName, id);

        return (new Dictionary<string, object>(), "");
    }

    public async Task<bool> Delete(string tableName, string id)
    {
        logger?.LogInformation("[MySQL]: Start delete record from table {TableName} by id {Id}", tableName, id);

        if (await RowExists(tableName, "id"))
        {
            logger?.LogInformation("[MySQL]: End delete record by id from table {TableName} by id {Id}", tableName, id);
            return await DeleteByField(tableName, "id", id.ToLong());
        }

        if (await RowExists(tableName, "Id"))
        {
            logger?.LogInformation("[MySQL]: End delete record by id from table {TableName} by Id {Id}", tableName, id);
            return await DeleteByField(tableName, "Id", id.ToLong());
        }

        logger?.LogError("[MySQL]: Error delete record by id from table {TableName} by id {Id}", tableName, id);

        return false;
    }

    public async Task<bool> DeleteByField<T>(string tableName, string nameField, T field)
    {
        logger?.LogInformation("[MySQL]: Start delete record from table {TableName} by field {NameField} with value {Id}", tableName, nameField, field);

        var connection = CreateConnection();

        var request = $"DELETE FROM {tableName} WHERE {nameField} = @id";
        logger?.LogInformation("[MySQL]: Request to database: {Request}", request);

        var command = new MySqlCommand(request, connection);
        command.Parameters.Add(new MySqlParameter("@id", field));

        await connection.OpenAsync();
        var transaction = await connection.BeginTransactionAsync();
        int rows;

        try
        {
            rows = await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            logger?.LogError(e,
                "[MySQL]: Error to delete record from table {TableName} by field {NameField} with value {Id}",
                tableName, nameField, field);
            return false;
        }
        finally
        {
            await CloseConnection(connection);
        }

        logger?.LogInformation("[MySQL]: End delete record from table {TableName} by field {NameField} with value {Id} (Rows: {Rows})", tableName, nameField, field, rows);

        return rows > 0;
    }

    public async Task<bool> Update(string tableName, string id, IDictionary<string, object> dct)
    {
        logger?.LogInformation("[MySQL]: Start update record from table {TableName} by id {Id}", tableName, id);

        if (await RowExists(tableName, "id"))
        {
            logger?.LogInformation("[MySQL]: End update record by id from table {TableName} by id {Id}", tableName, id);
            return await UpdateByField(tableName, "id", id.ToLong(), dct);
        }

        if (await RowExists(tableName, "Id"))
        {
            logger?.LogInformation("[MySQL]: End update record by id from table {TableName} by Id {Id}", tableName, id);
            return await UpdateByField(tableName, "Id", id.ToLong(), dct);
        }

        logger?.LogError("[MySQL]: Error update record by id from table {TableName} by id {Id}", tableName, id);

        return false;
    }

    public async Task<bool> UpdateByField<T>(string tableName, string nameField, T field, IDictionary<string, object> dct)
    {
        logger?.LogInformation("[SQLite]: Start update record by field from table {TableName} by field {NameField} with value {Field}", tableName, nameField, field);
        var connection = CreateConnection();

        var request = $"UPDATE {tableName} SET ";
        var command = new MySqlCommand(string.Empty, connection);

        for (var i = 0; i < dct.Keys.Count; i++)
        {
            var key = dct.Keys.ToList()[i];

            request += $"{key} = @v{i}";

            command.Parameters.Add(new MySqlParameter($"@v{i}", dct[key]));

            if (i + 1 < dct.Keys.Count)
                request += ", ";
        }

        request += $" WHERE {nameField} = @id";

        command.CommandText = request;
        command.Parameters.Add(new MySqlParameter("@id", field));

        logger?.LogInformation("[MySQL]: Request to database: {Request}({Command})", request, command);

        await connection.OpenAsync();
        var transaction = await connection.BeginTransactionAsync();
        int rows;

        try
        {
            rows = await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            logger?.LogError(e,
                "[MySQL]: Error to update record by field from table {TableName} by field {NameField} with value {Field}",
                tableName, nameField, field);
            return false;
        }
        finally
        {
            await CloseConnection(connection);
        }

        logger?.LogInformation("[MySQL]: End update record by field from table {TableName} by field {NameField} with value {Field} (rows: {Rows})", tableName, nameField, field, rows);

        return rows > 0;
    }

    public async Task<(bool Success, string Id)> Create(string tableName, IDictionary<string, object?> dct)
    {
        logger?.LogInformation("[MySQL]: Start create record in table {TableName}", tableName);

        var connection = CreateConnection();

        var request = $"INSERT INTO {tableName} (";
        var command = new MySqlCommand(string.Empty, connection);

        for (var i = 0; i < dct.Keys.Count; i++)
        {
            var key = dct.Keys.ToList()[i];

            request += key;

            if (i + 1 < dct.Keys.Count)
                request += ", ";
        }

        request += ") VALUES (";

        for (var i = 0; i < dct.Keys.Count; i++)
        {
            var key = dct.Keys.ToList()[i];

            request += $"@v{i}";

            command.Parameters.Add(new MySqlParameter($"@v{i}", dct[key]));

            if (i + 1 < dct.Keys.Count)
                request += ", ";
        }

        request += ")";

        command.CommandText = request;

        logger?.LogInformation("[SQLite]: Request to database: {Request} (Command: {Command})", request, command);

        await connection.OpenAsync();
        var transaction = await connection.BeginTransactionAsync();
        int rows;

        try
        {
            rows = await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            logger?.LogError(e, "[MySQL]: Error to create record in table {TableName}", tableName);
            return (false, string.Empty);
        }
        finally
        {
            await CloseConnection(connection);
        }

        var lastId = command.LastInsertedId;

        logger?.LogInformation("[SQLite]: End create record in table {TableName} (Rows: {Rows}, LastId: {LastId})", tableName, rows, lastId);

        return rows == 0 ? (false, string.Empty) : (true, lastId.ToString());
    }

    public T? GetId<T>(object obj) => obj.To<T>();

    public async Task<bool> CreateTable(string nameTable, bool checkExists = true, params DbBase[] rows)
    {
        logger?.LogInformation("[MySQL]: Start create table {NameTable}", nameTable);

        var connection = CreateConnection();

        var request = "CREATE TABLE ";

        if (checkExists)
            request += "IF NOT EXISTS ";

        request += $"{nameTable} (";

        var dbParams = rows.OfType<DbParam>().ToArray();
        var dbForeignKeys = rows.OfType<DbForeignKey>().ToArray();

        for (var i = 0; i < dbParams.Length; i++)
        {
            request += $"{dbParams[i].Name} {GetStringType(dbParams[i].TypeField)}";

            if (dbParams[i].AutoIncrement is true)
                request += " AUTO_INCREMENT";

            if (dbParams[i].PrimaryKey)
                request += " PRIMARY KEY";

            if (dbParams[i].Unique)
                request += " UNIQUE";

            if (dbParams[i].CanNull is false)
                request += " NOT NULL";

            if (dbParams[i].CanNull is true)
                request += " NULL";

            if (dbParams[i].HaveDefaultValue)
                request += $" DEFAULT {GetDefaultValue(dbParams[i])}";

            if (i + 1 < dbParams.Length)
                request += ",";
        }

        if (dbForeignKeys.Length > 0)
            request += ",";

        for (var i = 0; i < dbForeignKeys.Length; i++)
        {
            var key = dbForeignKeys[i];

            request += $"FOREIGN KEY ({key.Name}) REFERENCES {key.Table} ({key.NameField})";

            if (i + 1 < dbForeignKeys.Length)
                request += ",";
        }

        request += ")";

        var command = new MySqlCommand(request, connection);

        logger?.LogInformation("[MySQL]: Request to database: {Request} (Command: {Command})", request, command);

        await connection.OpenAsync();
        var transaction = await connection.BeginTransactionAsync();
        int result;

        try
        {
            result = await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            logger?.LogError(e, "[MySQL]: Error to create table {NameTable}", nameTable);
            return false;
        }
        finally
        {
            await CloseConnection(connection);
        }

        logger?.LogInformation("[MySQL]: End create table {NameTable} (Result: {Result})", nameTable, result);

        return result > 0;
    }

    private MySqlConnection CreateConnection() => new(_connectionString);

    private static async Task CloseConnection(MySqlConnection connection)
    {
        if (connection.State is ConnectionState.Closed or ConnectionState.Broken)
            return;

        await connection.CloseAsync();
        await connection.DisposeAsync();
    }

    private async IAsyncEnumerable<(Dictionary<string, object> Fields, string Id)> GetAllRecordsByCommand(DbCommand command)
    {
        logger?.LogInformation("[MySQL]: Get all records by command");

        var reader = await command.ExecuteReaderAsync();

        logger?.LogInformation("[MySQL]: Start read records by command");

        while (await reader.ReadAsync())
        {
            var values = new object[reader.FieldCount];
            var cnt = reader.GetValues(values);

            var dct = new Dictionary<string, object>();

            for (var i = 0; i < cnt; i++)
                dct.Add(reader.GetName(i), values[i]);

            var id = string.Empty;

            if (dct.TryGetValue("Id", out var idObj))
                id = idObj.ToString() ?? string.Empty;

            if (dct.TryGetValue("id", out idObj))
                id = idObj.ToString() ?? string.Empty;

            yield return (dct, id);
        }

        logger?.LogInformation("[MySQL]: End read records by command");
    }

    public async Task<bool> RowExists(string tableName, string nameField)
    {
        logger?.LogInformation("[MySQL]: Check exists row in table {TableName} by field {NameField}", tableName,
            nameField);

        var connection = CreateConnection();

        try
        {
            var command = new MySqlCommand(
                $"SELECT EXISTS(SELECT 1 FROM {tableName} WHERE {nameField}=1);",
                connection
            );

            logger?.LogInformation("[MySQL]: Request to database: {Request} (Command: {Command})", command.CommandText,
                command);

            await connection.OpenAsync();

            var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return reader.GetInt32(0) == 1;
            }

            return true;
        }
        catch
        {
            // ignored
        }
        finally
        {
            await CloseConnection(connection);

            logger?.LogInformation("[MySQL]: End check exists row in table {TableName} by field {NameField}", tableName, nameField);
        }

        logger?.LogError("[MySQL]: Error check exists row in table {TableName} by field {NameField}", tableName, nameField);

        return false;
    }

    private async Task<(IDictionary<string, object> Fields, string Id)> GetRecordInAll<T>(string tableName, string nameField, T field)
    {
        logger?.LogInformation("[MySQL]: Get record in all from table {TableName} by field {NameField} with value {Field}", tableName, nameField, field);

        await foreach (var item in GetAllRecords(tableName))
        {
            if (!item.Fields.TryGetValue(nameField, out var value) || value.ToString() != field?.ToString()) continue;

            logger?.LogInformation("[MySQL]: End get record in all from table {TableName} by field {NameField} with value {Field} return Record {Record}", tableName, nameField, field, item);
            return item;
        }

        logger?.LogError("[MySQL]: Error to get record in all from table {TableName} by field {NameField} with value {Field}", tableName, nameField, field);

        return (new Dictionary<string, object>(), "");
    }

    private static string GetStringType(Type type)
    {
        if (type == typeof(int) || type == typeof(bool))
            return "INT";

        if (type == typeof(float) || type == typeof(double))
            return "REAL";

        if (type == typeof(ulong) || type == typeof(long))
            return "BIGINT";

        if (type == typeof(DateTime))
            return "DATETIME";

        return "VARCHAR(512)";
    }

    private static object? GetDefaultValue(DbParam dbParam)
    {
        if (dbParam.TypeField == typeof(bool))
            return Convert.ToInt32(dbParam.DefaultValue != null && (bool) dbParam.DefaultValue);

        if (dbParam.TypeField == typeof(string))
            return "'" + dbParam.DefaultValue + "'";

        return dbParam.DefaultValue;
    }
}