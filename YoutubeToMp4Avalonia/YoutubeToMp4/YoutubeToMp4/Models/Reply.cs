using System;
using System.Net;

namespace YoutubeToMp4.Models;

public enum ServiceErrorType
{
    None,
    IoError,
    ValidationError,
    NotFound,
    Unauthorized,
    Conflict,
    ServerError,
    NetworkError,
    BadRequest,
    AppError,
    ExternalError
}

public sealed record Reply<T>
{
    const string MESSAGE_RESPONSE_ERROR = "Failed to produce a proper response message!";

    public Reply( ServiceErrorType errorType, string? message = null )
    {
        Data = default;
        Success = false;
        ErrorType = errorType;
        Message = message ?? GetDefaultMessage( errorType );
    }
    public Reply( T data, ServiceErrorType errorType, string? message = null )
    {
        Data = data;
        Success = false;
        ErrorType = errorType;
        Message = message ?? GetDefaultMessage( errorType );
    }
    public Reply( T data )
    {
        Data = data;
        Success = true;
        Message = string.Empty;
    }

    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public ServiceErrorType ErrorType { get; init; } = ServiceErrorType.None;

    public string PrintDetails()
    {
        return $"{ErrorType} : {Message}";
    }
    public static ServiceErrorType GetHttpError(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            // 2xx Success
            HttpStatusCode.OK => ServiceErrorType.None,
            HttpStatusCode.Created => ServiceErrorType.None,
            HttpStatusCode.Accepted => ServiceErrorType.None,
            HttpStatusCode.NonAuthoritativeInformation => ServiceErrorType.None,
            HttpStatusCode.NoContent => ServiceErrorType.None,
            HttpStatusCode.ResetContent => ServiceErrorType.None,
            HttpStatusCode.PartialContent => ServiceErrorType.None,
            HttpStatusCode.MultiStatus => ServiceErrorType.None,
            HttpStatusCode.AlreadyReported => ServiceErrorType.None,
            HttpStatusCode.IMUsed => ServiceErrorType.None,

            // 3xx Redirection
            HttpStatusCode.Ambiguous => ServiceErrorType.NotFound,
            HttpStatusCode.Moved => ServiceErrorType.NotFound,
            HttpStatusCode.Found => ServiceErrorType.None,
            HttpStatusCode.RedirectMethod => ServiceErrorType.None,
            HttpStatusCode.NotModified => ServiceErrorType.None,
            HttpStatusCode.UseProxy => ServiceErrorType.None,
            HttpStatusCode.Unused => ServiceErrorType.BadRequest, // Note: This status code is no longer used; consider removing.
            HttpStatusCode.RedirectKeepVerb => ServiceErrorType.None,
            HttpStatusCode.PermanentRedirect => ServiceErrorType.None,

            // 4xx Client Error
            HttpStatusCode.BadRequest => ServiceErrorType.BadRequest,
            HttpStatusCode.Unauthorized => ServiceErrorType.Unauthorized,
            HttpStatusCode.PaymentRequired => ServiceErrorType.BadRequest, // Consider mapping to a more specific type if needed.
            HttpStatusCode.Forbidden => ServiceErrorType.Unauthorized,
            HttpStatusCode.NotFound => ServiceErrorType.NotFound,
            HttpStatusCode.MethodNotAllowed => ServiceErrorType.BadRequest,
            HttpStatusCode.NotAcceptable => ServiceErrorType.BadRequest,
            HttpStatusCode.ProxyAuthenticationRequired => ServiceErrorType.Unauthorized,
            HttpStatusCode.RequestTimeout => ServiceErrorType.NetworkError,
            HttpStatusCode.Conflict => ServiceErrorType.Conflict,
            HttpStatusCode.Gone => ServiceErrorType.NotFound,
            HttpStatusCode.LengthRequired => ServiceErrorType.BadRequest,
            HttpStatusCode.PreconditionFailed => ServiceErrorType.BadRequest,
            HttpStatusCode.RequestEntityTooLarge => ServiceErrorType.BadRequest,
            HttpStatusCode.RequestUriTooLong => ServiceErrorType.BadRequest,
            HttpStatusCode.UnsupportedMediaType => ServiceErrorType.BadRequest,
            HttpStatusCode.RequestedRangeNotSatisfiable => ServiceErrorType.BadRequest,
            HttpStatusCode.ExpectationFailed => ServiceErrorType.BadRequest,
            HttpStatusCode.MisdirectedRequest => ServiceErrorType.BadRequest,
            HttpStatusCode.UnprocessableEntity => ServiceErrorType.ValidationError,
            HttpStatusCode.Locked => ServiceErrorType.BadRequest,
            HttpStatusCode.FailedDependency => ServiceErrorType.BadRequest,
            HttpStatusCode.TooManyRequests => ServiceErrorType.NetworkError,
            HttpStatusCode.RequestHeaderFieldsTooLarge => ServiceErrorType.BadRequest,
            HttpStatusCode.UnavailableForLegalReasons => ServiceErrorType.BadRequest,

            // 5xx Server Error
            HttpStatusCode.InternalServerError => ServiceErrorType.ServerError,
            HttpStatusCode.NotImplemented => ServiceErrorType.ServerError,
            HttpStatusCode.BadGateway => ServiceErrorType.ServerError,
            HttpStatusCode.ServiceUnavailable => ServiceErrorType.ServerError,
            HttpStatusCode.GatewayTimeout => ServiceErrorType.ServerError,
            HttpStatusCode.HttpVersionNotSupported => ServiceErrorType.ServerError,
            HttpStatusCode.VariantAlsoNegotiates => ServiceErrorType.ServerError,
            HttpStatusCode.InsufficientStorage => ServiceErrorType.ServerError,
            HttpStatusCode.LoopDetected => ServiceErrorType.ServerError,
            HttpStatusCode.NotExtended => ServiceErrorType.ServerError,
            HttpStatusCode.NetworkAuthenticationRequired => ServiceErrorType.NetworkError,

            _ => throw new ArgumentOutOfRangeException( nameof( statusCode ), statusCode, null )
        };
    }

    static string GetDefaultMessage( ServiceErrorType errorType )
    {
        return errorType switch
        {
            ServiceErrorType.IoError => "An IO error occured!",
            ServiceErrorType.Unauthorized => "Unauthorized!",
            ServiceErrorType.NotFound => "Data not found!",
            ServiceErrorType.ServerError => "Internal Server Error!",
            ServiceErrorType.ValidationError => "Validation failed!",
            _ => MESSAGE_RESPONSE_ERROR
        };
    }
}