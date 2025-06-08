namespace Database;

public class DbForeignKey(string name, string table, string nameField) : DbBase(name)
{
    public string Table { get; set; } = table;
    public string NameField { get; set; } = nameField;
}