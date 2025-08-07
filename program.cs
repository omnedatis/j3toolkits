using System;
using System.Windows;

namespace wzd32
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            var app = new Application();
            app.Run(new Views.ShellWindow());
        }
    }
}
