using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.IO;

namespace Grizli
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            if (Directory.Exists(@".\Utilities")==false)
            { Directory.CreateDirectory(@".\Utilities"); }
            Bootstrapper boot = new Bootstrapper();
            boot.Run();
        }
    }
}
