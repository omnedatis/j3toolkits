
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text;
using System.Windows;
using wzd32.Services;


namespace wzd32.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    private const string ConfigPath = "config.json";

    private static readonly UserConfig Config;

    static MainViewModel()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigPath);


        string RawText;
        try
        {
            RawText = File.ReadAllText(path, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            throw new FileNotFoundException("Configuration file not found at config.json", ex);
        }
        UserConfig userConfig;
        try
        {
            userConfig = JsonConvert.DeserializeObject<UserConfig>(RawText)!;
        }
        catch (Exception ex)
        {
            throw new FileFormatException("Invalid configuration file format", ex);
        }
        try
        {
            List<string> sources = userConfig.Sources;
            bool bsources = IsNotNullOrEmpty(sources);
            string name = userConfig.Name;
            bool bname = IsNotNullOrEmpty(name);
            if ((bname && bsources) != true)
            {
                throw new InvalidDataException("Invalid configuration in file.");
            }
        }
        catch (Exception ex)
        {
            throw new FileFormatException("Invalid configuration file format", ex);
        }
        Config = userConfig!;

    }

    public MainViewModel()
    {
        bool isInDesignMode =
             (bool)DesignerProperties
               .GetIsInDesignMode(new DependencyObject());

        if (isInDesignMode)
            ReadDesignData();
        else
            ReadData();
        // for messenger
        IsActive = true;

    }

    [ObservableProperty]
    private DateTime? lastModifyDT = null;

    [ObservableProperty]
    private ObservableCollection<UserInfo> userInfoList = new();

    [RelayCommand]
    private void ReadData()
    {
        //D
        string fullpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user.json");
        if (!File.Exists(fullpath))
            return;
        FileReadResult resp = WeakReferenceMessenger.Default.Send(new FileReadMessage(fullpath));
        var formatter = new JsonFormatter<List<UserInfoMap>>();
        List<UserInfoMap> data = formatter.Parse(resp.Content);
        //D
        Application.Current.Dispatcher.Invoke(() =>
        {
            UserInfoList.Clear();
            foreach (UserInfoMap item in data)
            {
                UserInfoList.Add(item.ToRecord());
            }
        });

    }
    private void ReadDesignData()
    {

        // mock data at design time
        UserInfoList.Add(new UserInfo("DesignAlice", "alice@fake.com", "abcd", "path/abbc"));
        UserInfoList.Add(new UserInfo("DesignBob", "bob@fake.com", "1234", "path/cdf"));
        OnPropertyChanged(nameof(UserInfoList));
    }

    [RelayCommand]
    public void Register()
    {
        LastModifyDT = DateTime.Now;
        var result = MessageBox.Show("是否使用預設值？", "Hint", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
            ParseUserInfo(true);
        else
            ParseUserInfo(false);

    }

    [RelayCommand]
    public void Test()
    {
        MessageBox.Show("this is a test", "hint", MessageBoxButton.OKCancel, 0);
    }

    public void ParseUserInfo(bool IsDefault = false)
    {

        List<UserInfoMap> data = new List<UserInfoMap>();
        Application.Current.Dispatcher.Invoke(() =>
        {
            UserInfoList.Clear();
        });
        foreach (string src in Config.Sources)
        {
            foreach (var dir in Directory.GetDirectories(src)
                .Where(x => (IsNotNullOrEmpty(x) && x.Contains(Config.Name))))
            {

                var req = new RequestUserInfoDialogMessage(dir);

                UserInfo userInfo;

                if (IsDefault == true)
                    userInfo = new UserInfo(dir, "", "", dir);

                else

                    userInfo = WeakReferenceMessenger.Default.Send(req);
                userInfo = userInfo ?? new UserInfo(dir, "", "", dir);


                Application.Current.Dispatcher.Invoke(() =>
                {
                    UserInfoList.Add(userInfo);
                });

                data.Add(userInfo.ToDict());
            }
            ;
            JsonFormatter<List<UserInfoMap>> formatter = new JsonFormatter<List<UserInfoMap>>();
            WriteData<List<UserInfoMap>>(formatter, data, "user");

        }
        ;
    }
    private void WriteData<TData>(IFileFormatter<TData> formatter, TData data, string name)
    {
        var fullpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{name}{formatter.Extension}");
        var content = formatter.Format(data);

        FileWriteResult result = WeakReferenceMessenger.Default
                     .Send(new FileWriteMessage(fullpath, content));

    }
    [RelayCommand]
    private void WriteData()
    {
        var result = MessageBox.Show("確認後將複寫歷史註冊檔案，確定儲存?", "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (result != MessageBoxResult.OK)
            return;
        List<UserInfoMap> data = [];
        foreach (UserInfo item in UserInfoList)
        {
            data.Add(item.ToDict());
        }
        var fullpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user.json");
        JsonFormatter<List<UserInfoMap>> formatter = new JsonFormatter<List<UserInfoMap>>();
        var content = formatter.Format(data);
        WeakReferenceMessenger.Default.Send(new FileWriteMessage(fullpath, content));
    }




    private static bool IsNotNullOrEmpty<T>(IEnumerable<T> target)
    {
        return target != null && target.Any();
    }
}
internal record UserConfig(string Name, List<string> Sources);

public partial class UserInfo : ObservableObject
{
    // 使用 CommunityToolkit 的 source-generator 產生屬性與 PropertyChanged
    // 產生的屬性為: Name, Account, Password, PID, Server, Role
    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private string account;

    [ObservableProperty]
    private string password;

    [ObservableProperty]
    private string pid;

    [ObservableProperty]
    private int? server;

    [ObservableProperty]
    private int? role;

    // 保留與原本相容的建構子（原先呼叫 new UserInfo("Name", "acc", "pwd", "pid")）
    public UserInfo(string name, string account, string password, string pid, int? server = null, int? role = null)
    {
        // 直接設定自動產生的屬性（會自動觸發 OnPropertyChanged）
        Name = name;
        Account = account;
        Password = password;
        Pid = pid;
        Server = server;
        Role = role;
    }

    // 空的預設建構子（保持靈活性 / JSON 反序列化）
    public UserInfo() { }
    public UserInfoMap ToDict()
    {

        var innerDict = new Dictionary<string, string>
        {
            [nameof(Name)] = Name,
            [nameof(Account)] = Account,
            [nameof(Password)] = Password,
            [nameof(Server)] = Server?.ToString() ?? string.Empty,
            [nameof(Role)] = Role?.ToString() ?? string.Empty
        };

        return new UserInfoMap(Name, Account, Password, Pid, Server, Role)
        {
            [Pid] = innerDict,
        };
    }

}

public class UserInfoMap : Dictionary<string, Dictionary<string, string>>
{

    public UserInfoMap() : base()
    {
    }
    public UserInfoMap(
        string Name,
        string Account,
        string Password,
        string PID,
        int? Server = null,
        int? Role = null)
    {
        var innerdict = new Dictionary<string, string>
        {
            [nameof(Name)] = Name,
            [nameof(Account)] = Account,
            [nameof(Password)] = Password,
            [nameof(Server)] = Server?.ToString() ?? string.Empty,
            [nameof(Role)] = Role?.ToString() ?? string.Empty
        };

        // 因為這個類別繼承 Dictionary，所以可以直接用 this 當作外層字典
        this[PID] = innerdict;
    }

    public UserInfo ToRecord()
    {

        var kvp = this.First(); // kvp.Key = PID, kvp.Value = inner dictionary
        var inner = kvp.Value;

        return new UserInfo(
            name: inner[nameof(UserInfo.Name)],
            account: inner[nameof(UserInfo.Account)],
            password: inner[nameof(UserInfo.Password)],
            pid: kvp.Key,
            server: string.IsNullOrEmpty(inner[nameof(UserInfo.Server)]) ? null : int.Parse(inner[nameof(UserInfo.Server)]),
            role: string.IsNullOrEmpty(inner[nameof(UserInfo.Role)]) ? null : int.Parse(inner[nameof(UserInfo.Role)])
        );
    }
}
