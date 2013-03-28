using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition;
using System.Text;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;

namespace ModuleA
{
    [ModuleExport(typeof(ModuleA)), Module(ModuleName = "ModuleA", OnDemand = true)]
    public class ModuleA : IModule
    {
        private IRegionManager regionManager;

        [ImportingConstructor]
        public ModuleA(IRegionManager regionManager)
        {
            this.regionManager = regionManager;
        }

        public void Initialize()
        {
            regionManager.AddToRegion("TabRegion", new Views.MainWindow());
        }
    }
}
