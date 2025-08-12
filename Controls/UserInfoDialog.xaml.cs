using System.Windows;
using System.Windows.Controls;
using wzd32.ViewModels;
namespace wzd32.Controls;
/// <summary>
/// Interaction logic for Window1.xaml
/// </summary>
public interface IUserInfoDialog
{
    bool ShowCustomDialog<TVeiwModel>(TVeiwModel vm);


}
public partial class UserInfoDialog : Window
{
    public UserInfo Result { get; private set; }

    private string DefaultName;
    public UserInfoDialog(string ret)
    {
        InitializeComponent();
        this.DefaultName = ret;
        NameTextBox.Text = ret;

    }



    private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {

    }

    private void AccountTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {

    }

    private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        Result = new UserInfo(NameTextBox.Text, AccountTextBox.Text, PasswordTextBox.Text, this.DefaultName);
        DialogResult = true;

    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Result = new UserInfo(this.DefaultName, "", "", this.DefaultName);
        DialogResult = false;
    }
}
