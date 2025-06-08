namespace Database;

// Row to create table
public class DbBase(string name)
{
    public string Name { get; set; } = name;
}

public class DbParam(string name, Type typeField) : DbBase(name)
{
    private readonly object? _defaultValue;

    public Type TypeField { get; private set; } = typeField;
    public bool PrimaryKey { get; set; } = false;
    public DbForeignKey? ForeignKey { get; set; }
    public bool Unique { get; set; } = false;
    public bool? AutoIncrement { get; set; } = false;
    public bool? CanNull { get; set; } = null;
    public bool HaveDefaultValue { get; private set; } = false;

    public object? DefaultValue
    {
        get => _defaultValue;
        init
        {
            HaveDefaultValue = true;
            _defaultValue = value;
        }
    }

    public void ClearDefaultValue() => HaveDefaultValue = false;
}