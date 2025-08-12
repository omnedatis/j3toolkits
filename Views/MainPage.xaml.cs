using CommunityToolkit.Mvvm.Messaging;
using System.Windows;
using System.Windows.Controls;
using wzd32.Controls;
using wzd32.Services;
using wzd32.ViewModels;
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
        var ret= dlg.ShowDialog();
        return dlg.Result;

    }
    // wrapper
    private void HandleOpenUserInfoDialog(object recipient, OpenUserInfoDialogMessage msg)
    {

        _ = ShowUserInfoDialog(msg.Path);
    }

    private void UserInfo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

    }

    private void HintButton_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {

    }

    private void HintButton_Loaded_1(object sender, RoutedEventArgs e)
    {

    }

    private void HintButton_Loaded_2(object sender, RoutedEventArgs e)
    {

    }
}
