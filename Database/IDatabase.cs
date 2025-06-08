namespace Database;

public interface IDatabase
{
    void Start();
    void End();
    IAsyncEnumerable<(IDictionary<string, object> Fields, string Id)> GetAllRecords(string tableName);
    IAsyncEnumerable<(IDictionary<string, object> Fields, string Id)> GetAllRecordsByField<T>(string tableName, string nameField, T field);
    Task<(IDictionary<string, object> Fields, string Id)> GetRecord<T>(string tableName, string nameField, T field, bool isString = true, Match match = Match.Exact);
    Task<(IDictionary<string, object> Fields, string Id)> GetRecord(string tableName, params SearchField[] fields);
    Task<(IDictionary<string, object> Fields, string Id)> GetRecordById(string tableName, string id);
    Task<bool> Delete(string tableName, string id);
    Task<bool> DeleteByField<T>(string tableName, string nameField, T field);
    Task<bool> Update(string tableName, string id, IDictionary<string, object> dct);
    Task<bool> UpdateByField<T>(string tableName, string nameField, T field, IDictionary<string, object> dct);
    Task<(bool Success, string Id)> Create(string tableName, IDictionary<string, object?> dct);
    T? GetId<T>(object obj);
    Task<bool> CreateTable(string nameTable, bool checkExists = true, params DbBase[] rows);
}