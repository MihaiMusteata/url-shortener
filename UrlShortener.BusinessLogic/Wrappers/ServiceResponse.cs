namespace UrlShortener.BusinessLogic.Wrappers;

public abstract class ServiceResponseBase
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class ServiceResponse : ServiceResponseBase
{
    public static ServiceResponse Ok(string? message = null) =>
        new ServiceResponse
        {
            Success = true,
            Message = message
        };

    public static ServiceResponse Fail(string message) =>
        new ServiceResponse
        {
            Success = false,
            Message = message
        };
}

public class ServiceResponse<T> : ServiceResponseBase
{
    public T? Data { get; private set; }

    public static ServiceResponse<T> Ok(T data, string? message = null) =>
        new ServiceResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };

    public static ServiceResponse<T> Fail(string message) =>
        new ServiceResponse<T>
        {
            Success = false,
            Message = message
        };
}