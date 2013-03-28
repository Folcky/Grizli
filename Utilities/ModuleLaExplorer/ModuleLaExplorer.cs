using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition;
using System.Text;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using System.Windows.Controls;
using Microsoft.Practices.Prism.Events;
using Infrastructure;

namespace LaExplorer
{
    [ModuleExport(typeof(ModuleLaExplorer)), Module(ModuleName = "ModuleLaExplorer", OnDemand = true)]
    public class ModuleLaExplorer : IModule
    {
        private string _name = "ModuleLaExplorer";
        public string Name { get { return _name; } }

        private IRegionManager regionManager;
        private IEventAggregator eventaggregator;

        [ImportingConstructor]
        public ModuleLaExplorer(IRegionManager regionManager, IEventAggregator eventaggregator)
        {
            this.regionManager = regionManager;
            this.eventaggregator = eventaggregator;
        }

        public void Initialize()
        {
            //CreateModuleCopy(null);
            eventaggregator.GetEvent<ActivateModuleCopy>().Subscribe(CreateModuleCopy);
            eventaggregator.GetEvent<Command2Module>().Subscribe(CommandReciever);
            
            //eventaggregator.GetEvent<CreateBookmark>().Publish(new What2Do() { ModuleName = "ModuleLaExplorer1" });
        }

        public void CreateModuleCopy(What2Do obj)
        {
            if (obj != null && obj.ModuleName == "ModuleLaExplorer")
            {
                int panel_count = 2;
                if (obj.ModuleCommand != null && obj.ModuleCommand.Count != 0)
                    panel_count = obj.ModuleCommand.Count();
                var view = new Views.Container(Orientation.Horizontal, panel_count, obj.ModuleCommand);
                view.My_module = this;
                view.My_region = regionManager.Regions["TabRegion"];
                regionManager.AddToRegion("TabRegion", view);
                regionManager.Regions["TabRegion"].Activate(view);
            }
        }

        public void CommandReciever(What2Do obj)
        {
            if (obj != null && obj.ModuleName == "ModuleLaExplorer"
                && obj.ModuleCommand != null && obj.ModuleCommand.Count()>0)
            {
                int i = (regionManager.Regions["TabRegion"].ActiveViews.FirstOrDefault() as LaExplorer.Views.Container).PanelsInfo.CurrentPanelIndex;
                string command = obj.ModuleCommand.FirstOrDefault();
                (regionManager.Regions["TabRegion"].ActiveViews.FirstOrDefault() as LaExplorer.Views.Container).PanelsInfo.sSources[i - 1].ExecuteCommand(command);
            }
        }

        public void CreateBookmark(What2Do obj)
        {
            if (obj != null)
            {
                eventaggregator.GetEvent<CreateBookmark>().Publish(obj);
            }
        }

        public void Command1Publicator(What2Do obj)
        {
            if (obj != null)
            {
                eventaggregator.GetEvent<Command1Executed>().Publish(obj);
            }
        }
    }
}
