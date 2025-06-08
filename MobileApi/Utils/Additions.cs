namespace MobileApi.Utils;

/// <summary>
/// Статический класс с методами-расширениями для работы со словарями и строками.
/// </summary>
public static class Additions
{
    /// <summary>
    /// Получает строковое значение по ключу из словаря типа IDictionary&lt;T, string&gt;.
    /// </summary>
    /// <typeparam name="T">Тип ключа</typeparam>
    /// <param name="dct">Словарь</param>
    /// <param name="val">Ключ</param>
    /// <returns>Строковое значение или пустая строка</returns>
    public static string GetString<T>(this IDictionary<T, string> dct, T val) where T : notnull => dct.TryGetValue(val, out var res) ? res : "";

    /// <summary>
    /// Получает строковое представление значения по ключу из словаря типа IDictionary&lt;T, J&gt;.
    /// </summary>
    /// <typeparam name="T">Тип ключа</typeparam>
    /// <typeparam name="J">Тип значения</typeparam>
    /// <param name="dct">Словарь</param>
    /// <param name="val">Ключ</param>
    /// <returns>Строковое значение или пустая строка</returns>
    public static string GetString<T, J>(this IDictionary<T, J> dct, T val) where T : notnull =>
        dct.TryGetValue(val, out var res) && !string.IsNullOrWhiteSpace(res?.ToString()) ? res.ToString() : "";

    /// <summary>
    /// Получает целое число по ключу из словаря типа IDictionary&lt;T, J&gt;.
    /// </summary>
    /// <typeparam name="T">Тип ключа</typeparam>
    /// <typeparam name="J">Тип значения</typeparam>
    /// <param name="dct">Словарь</param>
    /// <param name="val">Ключ</param>
    /// <returns>Целое число или -1</returns>
    public static int GetInt<T, J>(this IDictionary<T, J> dct, T val) where T : notnull => int.TryParse(dct.GetString(val), out var res) ? res : -1;

    /// <summary>
    /// Получает значение типа ulong по ключу из словаря типа IDictionary&lt;T, J&gt;.
    /// </summary>
    /// <typeparam name="T">Тип ключа</typeparam>
    /// <typeparam name="J">Тип значения</typeparam>
    /// <param name="dct">Словарь</param>
    /// <param name="val">Ключ</param>
    /// <returns>Значение типа ulong или 0</returns>
    public static ulong GetUlong<T, J>(this IDictionary<T, J> dct, T val) where T : notnull => ulong.TryParse(dct.GetString(val), out var res) ? res : 0;

    /// <summary>
    /// Получает значение типа double по ключу из словаря типа IDictionary&lt;T, J&gt;.
    /// </summary>
    /// <typeparam name="T">Тип ключа</typeparam>
    /// <typeparam name="J">Тип значения</typeparam>
    /// <param name="dct">Словарь</param>
    /// <param name="val">Ключ</param>
    /// <returns>Значение типа double или -1.0d</returns>
    public static double GetDouble<T, J>(this IDictionary<T, J> dct, T val) where T : notnull => double.TryParse(dct.GetString(val), out var res) ? res : -1.0d;

    /// <summary>
    /// Получает значение типа DateTime по ключу из словаря типа IDictionary&lt;T, J&gt;.
    /// </summary>
    /// <typeparam name="T">Тип ключа</typeparam>
    /// <typeparam name="J">Тип значения</typeparam>
    /// <param name="dct">Словарь</param>
    /// <param name="val">Ключ</param>
    /// <returns>Значение типа DateTime или текущая дата и время (UTC)</returns>
    public static DateTime GetDateTime<T, J>(this IDictionary<T, J> dct, T val) where T : notnull => DateTime.TryParse(dct.GetString(val), out var res) ? res : DateTime.UtcNow;

    /// <summary>
    /// Преобразует строку в целое число.
    /// </summary>
    /// <param name="val">Строка</param>
    /// <returns>Целое число или -1</returns>
    public static int ToInt(this string val) => int.TryParse(val, out var res) ? res : -1;

}
