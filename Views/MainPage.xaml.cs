using CommunityToolkit.Mvvm.Messaging;
using Shared.Contracts;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using wzd32.Controls;
using wzd32.Services;
using wzd32.ViewModels;
using System.IO;
namespace wzd32.Views;


/// <summary>
/// Interaction logic for Page1.xaml
/// </summary>
public partial class MainPage : Page
{
    public MainPage()
    {
        // Fix for CS0103: Ensure InitializeComponent is defined.
        InitializeComponent();
        DataContext = new MainViewModel();
        this.Loaded += OnPageLoaded;
        this.Unloaded += OnPageUnloaded;

    }


    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Register<OpenUserInfoDialogMessage>(
            this, HandleOpenUserInfoDialog);
        WeakReferenceMessenger.Default.Register<RequestUserInfoDialogMessage>(
                this,
                (recipient, message) =>
                {
                    var info = ShowUserInfoDialog(message.Path);
                    message.Reply(info);
                });

    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<OpenUserInfoDialogMessage>(this);
        WeakReferenceMessenger.Default.Unregister<RequestUserInfoDialogMessage>(this);
    }

    private UserInfo ShowUserInfoDialog(string path)
    {
        var owner = Window.GetWindow(this) ?? Application.Current.MainWindow;
        var dlg = new UserInfoDialog(path)
        {
            Owner = owner,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Width = 400,
            Height = 200
        };
        dlg.Left = owner.Left + 50;
        dlg.Top = owner.Top + 30;
        var ret = dlg.ShowDialog();
        return dlg.Result;

    }
    // wrapper
    private void HandleOpenUserInfoDialog(object recipient, OpenUserInfoDialogMessage msg)
    {

        _ = ShowUserInfoDialog(msg.Path);
    }

    private static readonly Regex DigitsOnlyRegex = new(@"^\d*$", RegexOptions.Compiled);

    public static void OnPreviewTextInputDigitsOnly(object sender, TextCompositionEventArgs e)
    {
        // 允許空與純數字
        e.Handled = !DigitsOnlyRegex.IsMatch(e.Text);
    }

    public static void OnPasteDigitsOnly(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        var text = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        if (!DigitsOnlyRegex.IsMatch(text))
        {
            e.CancelCommand();
        }
    }
    private void UserInfo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

    }

    private void HintButton_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        var hub = await HubClient.CreateAsync("127.0.0.1", 54001, token: "dev-token");
        _hubClient = hub; // Store the HubClient instance for later use 
        hub.Telemetry += t => Console.WriteLine($"CPU:{t.Cpu} MEM:{t.MemMB}");
        hub.Log += l => Console.WriteLine($"[{l.Level}] {l.Line}");
        hub.Exited += x => Console.WriteLine($"EXIT {x.PidKey} code={x.ExitCode}");
        hub.Error += ex => Console.Error.WriteLine(ex);
        hub.Disconnected += why => Console.WriteLine($"Disconnected: {why}");

        var exe = @"C:\Program Files (x86)\jxsj3(yu)\JXSJ3Launcher.exe";
        var dir = Path.GetDirectoryName(exe)!;
        await hub.StartAsync(new Start(
            PidKey: "000001",
            FileName:exe,
            Args: "",
            WorkDir: dir)
        );

        // …必要時送命令
        await hub.SendAsync(new Command("000001", "stdin"));

        // 收尾
    }

    private void HintButton_Loaded_1(object sender, RoutedEventArgs e)
    {

    }

    private void HintButton_Loaded_2(object sender, RoutedEventArgs e)
    {

    }

    // Replace the following method with the corrected version

    private HubClient? _hubClient; // Add this field to store the HubClient instance

    private async void Button_Click_1(object sender, RoutedEventArgs e)
    {
        // Ensure _hubClient is initialized before using it
        if (_hubClient == null)
        {
            _hubClient = await HubClient.CreateAsync("127.0.0.1", 54001, token: "dev-token");
        }
        await _hubClient.SendAsync(new Command(PidKey: "000001", Name: "input.snap", Args: new { width=1280, height=720, margin=12 }));
        await _hubClient.SendAsync(new Command(PidKey: "000001", Name: "input.login", Args: new { user = "你的帳號", pass = "你的密碼" }));
    }
}
