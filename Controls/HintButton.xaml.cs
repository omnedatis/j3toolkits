using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}