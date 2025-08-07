using CommunityToolkit.Mvvm.Messaging.Messages;
using wzd32.ViewModels;
namespace wzd32.Services;
public class RequestUserInfoDialogMessage : RequestMessage<UserInfo>
{
    public string Path { get; }

    public RequestUserInfoDialogMessage(string path)
    {
        Path = path;
    }
}
public class OpenUserInfoDialogMessage : ValueChangedMessage<string>
{
    public OpenUserInfoDialogMessage() : base(String.Empty) { }

    public OpenUserInfoDialogMessage(string path) : base(path)
    {
    }

    public string Path => Value;
}