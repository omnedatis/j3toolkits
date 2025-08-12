using Microsoft.Extensions.Logging;

namespace wzd32.Services;

// Error hander signature
public interface IErrorHandler
{
    void HandleError(ErrorInfo info);
}

public class ErrorHandler : IErrorHandler
{
    public void HandleError(ErrorInfo info)
    {
        // Here you can implement the logic to handle the error, such as logging it or displaying a message to the user.
        switch (info.Type)
        {
            case ErrorType.warning:
                Console.WriteLine($"Warning: {info.Message}");
                break;
            case ErrorType.error:
                Console.WriteLine($"Error: {info.Message}");
                if (info.Exception != null)
                {
                    Console.WriteLine($"Exception: {info.Exception.Message}");
                }
                break;
            case ErrorType.critical:
                Console.WriteLine($"Critical Error: {info.Message}");
                if (info.Exception != null)
                {
                    Console.WriteLine($"Exception: {info.Exception.Message}");
                }
                // You might want to terminate the application or perform some other critical action here.
                break;
        }
    }
}

public enum ErrorType
{
    warning,
    error,
    critical, s
}
public class ErrorInfo
{
    public ErrorType Type { get; set; }
    public string Message { get; set; }
    public Exception? Exception { get; set; }
    public ErrorInfo(ErrorType type, string message, Exception? exception = null)
    {
        Type = type;
        Message = message;
        Exception = exception;
    }
}

internal class LoggerProvidrer
{
    private readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder =>
       {
           builder
               .SetMinimumLevel(LogLevel.Debug)
               .AddDebug();

       });
    public ILogger<T> CreateLogger<T>()
    {
        return _loggerFactory.CreateLogger<T>();
    }
}