namespace YoutubeToMp4.Services;

public class SingletonService<T> where T : new()
{
    public static void Create() => _instance ??= new T();
    public static T Instance => _instance ??= new T();
    static T? _instance;
}