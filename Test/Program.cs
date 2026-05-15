using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;


namespace Ephemera.IconicSelector.Test
{
    class Program
    {
        [STAThread]
        static void Main(string[] _)
        {
            // Handle unexpected esceptions.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) => { HandleException(e.Exception, "UI Thread Exception"); };
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => { HandleException((Exception)e.ExceptionObject, "Background Thread Exception"); };

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                var host = new TestHost();
                Application.Run(host);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "!!!");
                Environment.Exit(1);
            }
        }

        static void HandleException(Exception ex, string type)
        {
            MessageBox.Show(ex.ToString(), type);
            Environment.Exit(1);
        }
    }
}
