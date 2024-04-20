using YoutubeToMp4.Shared.Enums;

namespace YoutubeToMp4.Shared.Dtos;

public sealed record Reply<T>
{
    public Reply( DlError dlError, string? message = null )
    {
        Data = default;
        DlError = dlError;
        Message = message ?? "No error message provided.";
    }
    public Reply( T data )
    {
        Data = data;
        Message = string.Empty;
    }
    
    public T? Data { get; init; }
    public string Message { get; init; }
    public DlError DlError { get; init; } = DlError.None;
}