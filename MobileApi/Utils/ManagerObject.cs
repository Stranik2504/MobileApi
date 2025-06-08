namespace MobileApi.Utils;

/// <summary>
/// Класс-обёртка для управления объектом типа T с поддержкой событий загрузки и сохранения.
/// </summary>
/// <typeparam name="T">Тип управляемого объекта</typeparam>
public class ManagerObject<T>(string filename)
{
    /// <summary>
    /// Управляемый объект.
    /// </summary>
    public T? Obj { get; set; }

    /// <summary>
    /// Событие, вызываемое после загрузки объекта.
    /// </summary>
    public Action? Loaded { get => _manager.Loaded; set => _manager.Loaded = value; }

    /// <summary>
    /// Событие, вызываемое после сохранения объекта.
    /// </summary>
    public Action? Saved { get => _manager.Saved; set => _manager.Saved = value; }

    /// <summary>
    /// Менеджер для сериализации/десериализации объекта.
    /// </summary>
    private readonly Manager _manager = new(filename);

    /// <summary>
    /// Загружает объект из файла, если файл не существует — сохраняет текущий объект.
    /// </summary>
    public void Load()
    {
        if (!File.Exists(filename))
            _manager.Save(Obj);

        Obj = _manager.Load<T>();
    }

    /// <summary>
    /// Сохраняет объект в файл.
    /// </summary>
    public void Save() => _manager.Save(Obj);
}
