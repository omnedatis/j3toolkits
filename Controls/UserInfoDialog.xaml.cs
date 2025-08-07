using Prism.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
        Result = new UserInfo(NameTextBox.Text, AccountTextBox.Text, PasswordTextBox.Text);
        DialogResult = true;

    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Result = new UserInfo(DefaultName, "", "");
        DialogResult = false;
    }
}
