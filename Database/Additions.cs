namespace Database;

public static class Additions
{
    public static long ToLong(this object obj) => long.TryParse(obj.ToString(), out var res) ? res : -1;

    public static T? To<T>(this object obj)
    {
        if (obj is T res)
            return res;

        return default;
    }
}