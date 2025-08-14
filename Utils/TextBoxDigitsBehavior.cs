using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace wzd32.Utils;

public static class TextBoxDigitsBehavior
{
    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached(
            "Enabled",
            typeof(bool),
            typeof(TextBoxDigitsBehavior),
            new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetEnabled(DependencyObject obj) => (bool)obj.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject obj, bool value) => obj.SetValue(EnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox tb)
        {
            if ((bool)e.NewValue)
            {
                tb.PreviewTextInput += OnlyDigitsHandler;
                DataObject.AddPastingHandler(tb, OnPaste);
            }
            else
            {
                tb.PreviewTextInput -= OnlyDigitsHandler;
                DataObject.RemovePastingHandler(tb, OnPaste);
            }
        }
    }

    private static void OnlyDigitsHandler(object sender, TextCompositionEventArgs e)
        => e.Handled = !e.Text.All(char.IsDigit);

    private static void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(DataFormats.Text))
        {
            var text = (string)e.DataObject.GetData(DataFormats.Text);
            if (!text.All(char.IsDigit))
                e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }
}

