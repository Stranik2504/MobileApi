using Newtonsoft.Json;

namespace MobileApi.Utils;

/// <summary>
/// Класс для управления сериализацией и десериализацией объектов в файл с поддержкой потокобезопасности.
/// </summary>
public class Manager(string filename)
{
    /// <summary>
    /// Событие, вызываемое после загрузки объекта.
    /// </summary>
    public Action? Loaded;

    /// <summary>
    /// Событие, вызываемое после сохранения объекта.
    /// </summary>
    public Action? Saved;

    /// <summary>
    /// Мьютекс для синхронизации операций сохранения и загрузки.
    /// </summary>
    private readonly Mutex _saveLoad = new();

    /// <summary>
    /// Загружает объект типа T из файла.
    /// </summary>
    /// <typeparam name="T">Тип объекта для загрузки</typeparam>
    /// <returns>Загруженный объект или значение по умолчанию</returns>
    public T? Load<T>()
    {
        try
        {
            _saveLoad.WaitOne();

            using var stream = File.OpenText(filename);

            var obj = JsonConvert.DeserializeObject<T>(stream.ReadToEnd());

            _saveLoad.ReleaseMutex();

            Loaded?.Invoke();

            return obj;
        }
        catch
        {
            _saveLoad.Close();
        }

        return default;
    }

    /// <summary>
    /// Сохраняет объект типа T в файл.
    /// </summary>
    /// <typeparam name="T">Тип объекта для сохранения</typeparam>
    /// <param name="obj">Объект для сохранения</param>
    public void Save<T>(T obj)
    {
        try
        {
            _saveLoad.WaitOne();

            using var stream = File.CreateText(filename);

            stream.WriteLine(JsonConvert.SerializeObject(obj));

            _saveLoad.ReleaseMutex();

            Saved?.Invoke();
        }
        catch
        {
            _saveLoad.Close();
        }
    }
}
