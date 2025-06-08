namespace Database;

public class SearchField(
    object field,
    string nameField,
    Match match = Match.Exact,
    Connection con = Connection.NONE
)
{
    public object Field { get; private set; } = field;
    public Type TypeField { get; private set; } = field.GetType();
    public string NameField { get; private set; } = nameField;
    public Match Match { get; private set; } = match;
    public Connection Connection { get; private set; } = con;
}