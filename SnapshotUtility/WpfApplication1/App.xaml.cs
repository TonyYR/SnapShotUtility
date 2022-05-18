using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SnapshotUtilityForIBI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private System.Threading.Mutex mutex = new System.Threading.Mutex(false, "Snapshot Utility");
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!mutex.WaitOne(0,false))
            {
                MessageBox.Show("This application is aready executed.", "Double Start Prevention",MessageBoxButton.OK);
                mutex.Close();
                mutex = null;
                this.Shutdown();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if(mutex != null)
            {
                mutex.ReleaseMutex();
                mutex.Close();
            }
        }
    }
}
