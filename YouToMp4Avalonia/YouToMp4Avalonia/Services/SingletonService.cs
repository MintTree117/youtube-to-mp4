namespace YouToMp4Avalonia.Services;

public class SingletonService<T> : BaseService where T : new()
{
    public static void Create() => _instance ??= new T();
    public static T Instance => _instance ??= new T();
    static T? _instance;
}