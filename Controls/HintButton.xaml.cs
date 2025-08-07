using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using wzd32.Resources;

namespace wzd32.Controls
{
    public partial class HintButton : UserControl
    {
        public HintButton()
        {
            InitializeComponent();
        }


        // 1) Set the button text
        public static readonly DependencyProperty ButtonTextProperty =
              DependencyProperty.Register(
                nameof(ButtonText),
                typeof(string),
                typeof(HintButton),
                new PropertyMetadata(""));

            public string ButtonText
            {
                get => (string)GetValue(ButtonTextProperty);
                set => SetValue(ButtonTextProperty, value);
            }

        // 2) Set binding to a command
        public static readonly DependencyProperty CommandProperty =
              DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(HintButton),
                new PropertyMetadata(null));

        public ICommand Command
        {
                get => (ICommand)GetValue(CommandProperty);
                set => SetValue(CommandProperty, value);
            }

        // 3) Set the hint text
        public static readonly DependencyProperty HintTextProperty =
              DependencyProperty.Register(
                nameof(HintText),
                typeof(string),
                typeof(HintButton),
                new PropertyMetadata(""));
     
        public string HintText
            {
                get => (string)GetValue(HintTextProperty);
                set => SetValue(HintTextProperty, value);
            }
        
    }
}