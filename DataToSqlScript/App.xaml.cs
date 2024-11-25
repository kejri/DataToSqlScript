using DataToSqlScript.Main;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace DataToSqlScript
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Action EmptyDelegate = delegate () { };

        public App()
          : base()
        {
            this.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);

            //CultureInfo? culture = null;
            //if (culture != null)
            //{
            //    Thread.CurrentThread.CurrentCulture = culture;
            //    Thread.CurrentThread.CurrentUICulture = culture;
            //}

            var model = new MainPM(new MainView());
            MainWindow = model.View as Window;
            if (MainWindow != null)
            {
                MainWindow.Closed += MainWindow_Closed;
                MainWindow.Show();
            }
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {

        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // If the app is running outside of the debugger then report the exception using
            // the browser's exception mechanism. On IE this will display it a yellow alert 
            // icon in the status bar and Firefox will display a script error.
            Exception exception = e.Exception.GetBaseException();
            if (exception is Rk.Common.Exceptions.IRkException)
            {
                e.Handled = true;
                MessageBox.Show(exception.Message, "Chyba aplikace", MessageBoxButton.OK);
            }

            if (!e.Handled /*&& !System.Diagnostics.Debugger.IsAttached*/)
            {

                // NOTE: This will allow the application to continue running after an exception has been thrown
                // but not handled. 
                // For production applications this error handling should be replaced with something that will 
                // report the error to the website and stop the application.
                MessageBox.Show(exception.Message, "Chyba", MessageBoxButton.OK);
                e.Handled = true;
                //Deployment.Current.Dispatcher.BeginInvoke(delegate { ReportErrorToDOM(e); });
            }
        }
    }
}
