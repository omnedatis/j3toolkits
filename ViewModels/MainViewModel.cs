
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Resources;
using wzd32.Services;


namespace wzd32.ViewModels;

public partial class MainViewModel : ObservableRecipient
{

    private const string ConfigPath = "config.json";


    private static readonly UserConfig Config;

    static MainViewModel()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigPath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Configuration file not found at config.json");

        }
        string RawText = File.ReadAllText(path, Encoding.UTF8);
        UserConfig userConfig;
        try
        {
            userConfig = JsonConvert.DeserializeObject<UserConfig>(RawText)!;
        }
        catch (Exception ex)
        {
            throw new FileFormatException("Invalid configuration file format", ex);
        }
        List<string> sources = userConfig.Sources;
        bool bsources = IsNotNullOrEmpty(sources);
        string name = userConfig.Name;
        bool bname = IsNotNullOrEmpty(name);
        if ((bname && bsources) != true)
        {
            throw new InvalidCastException("Invalid configuration in file.");
        }
        Config = userConfig!;

    }

    public MainViewModel()
    {
        bool isInDesignMode =
             (bool)DesignerProperties
               .GetIsInDesignMode(new DependencyObject());

        if (isInDesignMode)
            LoadDesignData();
        else
            LoadRealData();
        // for messenger
        IsActive = true;

    }

    [ObservableProperty]
    private DateTime? lastModifyTime = null;

    
    [ObservableProperty]
    private DateTime? lastModifyDT = null;

    [ObservableProperty]
    private ObservableCollection<UserInfo> userInfoList = new();


    private void LoadRealData()
    {

        // ... 實際可改成非同步呼叫 Service
    }

    private void LoadDesignData()
    {

        // mock data for design time
        UserInfoList.Add(new UserInfo("DesignAlice", "alice@fake.com", "abcd"));
        UserInfoList.Add(new UserInfo("DesignBob", "bob@fake.com", "1234"));
        OnPropertyChanged(nameof(UserInfoList));
    }

    [RelayCommand]
    public void Register()
    {
        LastModifyDT = DateTime.Now;
        var result = MessageBox.Show("是否使用預設值？", "Hint", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
            ApplyUserInfo(true);
        else
            ApplyUserInfo(false);
        



    }
    [RelayCommand]  
    public void Test()
    {
        MessageBox.Show("this is a test", "hint", MessageBoxButton.OKCancel, 0);
    }

    public void ApplyUserInfo(bool IsDefault=false)
    {


        UserInfoList.Clear();
        foreach (string src in Config.Sources)
        {
            foreach (var dir in Directory.GetDirectories(src)
                .Where(x => (IsNotNullOrEmpty(x) && x.Contains(Config.Name))))
            {
                
                var req = new RequestUserInfoDialogMessage(dir);

                UserInfo userInfo;
                // 2. Send<TReq, TResp> 同步呼叫，直接拿到 UserInfo
                if (IsDefault == true)
                {
                    userInfo = new UserInfo(dir, "", "");
                } else {
                    userInfo = WeakReferenceMessenger.Default.Send(req);
                }

                    
                Debug.WriteLine(userInfo);
                // 3. 加到暫存清單
                UserInfoList.Add(userInfo);

            }
            ;
        };
    }
    private static bool IsNotNullOrEmpty<T>(IEnumerable<T> target)
    {
        return target != null && target.Any();
    }
}
internal record UserConfig(string Name, List<string> Sources);

public record UserInfo(string Name, string Account, string Password, string? GID=null, int? Server=null, int? Role=null);
