using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.MefExtensions;
using System.Windows;
using System.ComponentModel.Composition.Hosting;
using Microsoft.Practices.Prism.Modularity;

namespace Grizli
{
    class Bootstrapper : MefBootstrapper
    {
        protected override System.Windows.DependencyObject CreateShell()
        {
            return Container.GetExportedValue<MainWindow>();
        }
        protected override void InitializeShell()
        {
            base.InitializeShell();
            App.Current.MainWindow = (Window)Shell;
            App.Current.MainWindow.Show();
        }

        protected override Microsoft.Practices.Prism.Modularity.IModuleCatalog CreateModuleCatalog()
        {
            //MessageBox.Show(System.Windows.Forms.Application.ExecutablePath);
            try
            {
                return new DirectoryModuleCatalog() { ModulePath = @".\Utilities" };
            }
            catch (Exception er) { return null; }
        }

        protected override void ConfigureAggregateCatalog()
        {
            base.ConfigureAggregateCatalog();
            AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(Bootstrapper).Assembly));
        }
    }
}
