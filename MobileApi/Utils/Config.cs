namespace MobileApi.Utils;

/// <summary>
/// Класс конфигурации приложения для хранения параметров подключения к базе данных и других настроек.
/// </summary>
public class Config
{
    /// <summary>
    /// Хост основной базы данных.
    /// </summary>
    public string MainDbHost { get; set; }

    /// <summary>
    /// Порт основной базы данных.
    /// </summary>
    public int MainDbPort { get; set; }

    /// <summary>
    /// Имя основной базы данных.
    /// </summary>
    public string MainDbName { get; set; }

    /// <summary>
    /// Имя пользователя для подключения к основной базе данных.
    /// </summary>
    public string MainDbUser { get; set; }

    /// <summary>
    /// Пароль пользователя для подключения к основной базе данных.
    /// </summary>
    public string MainDbPassword { get; set; }
}
