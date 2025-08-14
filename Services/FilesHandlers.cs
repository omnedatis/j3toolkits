using CommunityToolkit.Mvvm.Messaging.Messages;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;

namespace wzd32.Services;
public class FileWriteMessage : RequestMessage<FileWriteResult>
{
    public string FilePath { get; }
    public string Content { get; }

    public FileWriteMessage(string filePath, string content)
    {
        FilePath = filePath;
        Content = content;
    }
}

public class FileReadMessage : RequestMessage<FileReadResult>
{
    public string FilePath { get; }

    public FileReadMessage(string filePath)
    {
        FilePath = filePath;
    }
}


public sealed record FileWriteResult(bool IsSuccess, string? ErrorMessage = null);
public sealed record FileReadResult(bool IsSuccess, string? Content = null, string? ErrorMessage = null);


public interface IFileFormatter<TData>
{
    /// object (check) => .json
    string Extension { get; }

    /// object => string
    string Format(TData data);

    public TData Parse(string content)
    {
        return JsonConvert.DeserializeObject<TData>(content);
    }

}

public class JsonFormatter<T> : IFileFormatter<T>
{
    public string Extension => ".json";

    public string Format(T data)
        => JsonConvert.SerializeObject(data, Formatting.Indented);

    public T Parse(string path)
    {

        return JsonConvert.DeserializeObject<T>(path)!;
    }

}

public class CsvFormatter<T> : IFileFormatter<IEnumerable<T>>
{
    public string Extension => ".csv";

    public string Format(IEnumerable<T> data)
    {
        var sb = new StringBuilder();
        var props = typeof(T).GetProperties();
        // 標頭
        sb.AppendLine(string.Join(",", props.Select(p => p.Name)));
        // 內容
        foreach (var item in data)
            sb.AppendLine(string.Join(",", props.Select(p => p.GetValue(item))));
        return sb.ToString();
    }

    public IEnumerable<T> Parse(string content)
    {
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
            yield break;

        var props = typeof(T).GetProperties();
        var headers = lines[0].Split(',');

        var propMap = new Dictionary<int, PropertyInfo>();
        for (int i = 0; i < headers.Length; i++)
        {
            var prop = props.FirstOrDefault(p => p.Name.Equals(headers[i], StringComparison.OrdinalIgnoreCase));
            if (prop != null)
                propMap[i] = prop;
        }

        for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            var values = lines[lineIndex].Split(',');

            var obj = Activator.CreateInstance<T>();
            foreach (var kvp in propMap)
            {
                int colIndex = kvp.Key;
                var prop = kvp.Value;

                if (colIndex < values.Length)
                {
                    var valStr = values[colIndex];
                    object? val = null;

                    try
                    {
                        if (prop.PropertyType == typeof(string))
                        {
                            val = valStr;
                        }
                        else if (prop.PropertyType.IsEnum)
                        {
                            val = Enum.Parse(prop.PropertyType, valStr);
                        }
                        else if (Nullable.GetUnderlyingType(prop.PropertyType) is Type underlying)
                        {
                            val = Convert.ChangeType(valStr, underlying);
                        }
                        else
                        {
                            val = Convert.ChangeType(valStr, prop.PropertyType);
                        }
                    }
                    catch
                    {
                        val = null;
                    }

                    prop.SetValue(obj, val);
                }
            }
            yield return obj;
        }
    }

}

