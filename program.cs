using CommunityToolkit.Mvvm.Messaging;
using System.IO;
using System.Text;
using System.Windows;
using wzd32.Services;


namespace wzd32;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 註冊 FileWriteMessage 的 handler
        WeakReferenceMessenger.Default.Register<FileWriteMessage>(
            this,
            (recipient, message) =>
            {
                try
                {
                    File.WriteAllText(message.FilePath, message.Content, Encoding.UTF8);
                    message.Reply(new FileWriteResult(true));
                }
                catch (Exception ex)
                {
                    // 可集中記錄 log 或顯示錯誤
                    message.Reply(new FileWriteResult(false, ex.Message));
                }
            });
        WeakReferenceMessenger.Default.Register<FileReadMessage>(
            this,
            (recipient, message) =>
            {
                try
                {
                    string content = File.ReadAllText(message.FilePath, Encoding.UTF8);
                    message.Reply(new FileReadResult(true, content));
                }
                catch (Exception ex)
                {
                    message.Reply(new FileReadResult(false, ex.Message));
                }
            });
    }
}

class Program
{
    [STAThread]
    static void Main()
    {
        var app = new App();
        app.Run(new Views.ShellWindow());
    }
}
