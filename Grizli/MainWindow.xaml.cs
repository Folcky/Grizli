using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Prism.Events;
using Infrastructure;
using System.Reflection;

namespace Grizli
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [Export]
    public partial class MainWindow : Window
    {
        private IModuleManager _moduleManager;
        private IRegionManager _regionManager;
        private IEventAggregator _eventaggregator;

        private Bookmarks _bookmarks = (new Bookmarks()).Load();

        [ImportingConstructor]
        public MainWindow([Import]IModuleManager moduleManager, [Import]IRegionManager regionManager, IEventAggregator eventaggregator)
        {
            try
            {
                InitializeComponent();
                _moduleManager = moduleManager;
                _regionManager = regionManager;
                _eventaggregator = eventaggregator;
                this.AddHandler(CloseableTabItem.CloseTabEvent, new RoutedEventHandler(this.CloseTab));
            }
            catch (Exception er) { MessageBox.Show(er.Message); }
        }
        bool isWiden = false;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Image child = (from l in FindChilds(cbAddress, typeof(Image))
                           where (l as Image).Name == "PART_EditableImage"
                           select l).FirstOrDefault() as Image;
            if (cbAddress.SelectedItem != null)
            {
                child.Source = (cbAddress.SelectedItem as Commander).ModuleImage;
            }
            else
            {
                ResourceDictionary resourceDictionary = new ResourceDictionary();
                resourceDictionary.Source = new Uri(
                "Grizli;component/BaseDictionary.xaml", UriKind.Relative);
                child.Source = (BitmapImage)resourceDictionary["ModuleIconDefault"];
            }

            try
            {
                //Подписываемся на создание закладок от модулей
                _eventaggregator.GetEvent<CreateBookmark>().Subscribe(EventFromModule);
                _eventaggregator.GetEvent<Command1Executed>().Subscribe(FillAddress);
                //Вычисляем максимальную высоту окна для корректной работы максимизации окна
                this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight-10;

                //Включаем событие для кнопки добавления модуля
                Button baddpanel = TabContainer.Template.FindName("bAddPanel", TabContainer) as Button;
                baddpanel.Click += new RoutedEventHandler(baddpanel_Click);
                //Добавляем меню к кнопке добавления модулей
                ContextMenu _addutilitymenu = (ContextMenu)this.TryFindResource("AddUtilityMenu");
                baddpanel.ContextMenu = _addutilitymenu;
                //Заполняем меню
                //TODO автозаполнение доступными модулями
                ((MenuItem)_addutilitymenu.Items[0]).Click += new RoutedEventHandler(baddpanel_Click);
                ((MenuItem)_addutilitymenu.Items[1]).Click += new RoutedEventHandler(OpenSSHConsole_Click);
                //DelegateAddContainer _addcontainer = AddContainer;
                //Application.Current.Dispatcher.BeginInvoke(_addcontainer, System.Windows.Threading.DispatcherPriority.Background, new_tab);
                SyncBookmarkPanel();
            }
            catch (Exception er) { MessageBox.Show(er.Message); }
        }

        private void SyncBookmarkPanel()
        {
            //Resource in this
            //style = Resources["ButtonCommon"] as Style
            //Resource globally
            //style = FindResource("ButtonCommon") as Style

            List<Bookmark> bms_loaded = new List<Bookmark>();
            foreach (UIElement uiobject in spBookmarks.Children.Cast<UIElement>())
            {
                if (uiobject.GetType() == typeof(Button) && (uiobject as Button).Tag != null && (uiobject as Button).Tag.GetType() == typeof(Bookmark))
                {
                    bms_loaded.Add((uiobject as Button).Tag as Bookmark);
                }
            }

            foreach(Bookmark bm in _bookmarks.ModuleBookmarks)
            {
                Bookmark bm_loaded = (from b in bms_loaded
                                      where b == bm
                                      select b).FirstOrDefault();
                if (bm_loaded == null)
                {
                    string content = "Bookmark";
                    foreach (string c in bm.ModuleCommand)
                    {
                        if ((c.Split('|')).ElementAt(2) != "")
                            content = (c.Split('|')).ElementAt(2);
                    }

                    Button bBM = new Button()
                                    {
                                        Height = 27,
                                        Margin = new Thickness() { Right = 1 },
                                        Padding = new Thickness() { Right = 4, Left = 4 },
                                        Content = content,
                                        Tag = bm,
                                        Style = FindResource("ButtonCommon") as Style
                                    };
                    bBM.Click += new RoutedEventHandler(bAddModulePanel_Click);
                    ContextMenu cm = new ContextMenu();
                    MenuItem mi = new MenuItem() { Tag = bBM, Header = "Удалить" };
                    mi.Click += new RoutedEventHandler(mi_Click);
                    cm.Items.Add(mi);
                    spBookmarks.Children.Add(bBM);
                    bBM.ContextMenu = cm;
                }
            }
        }

        private void mi_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _bookmarks.ModuleBookmarks.Remove(((sender as MenuItem).Tag as Button).Tag as Bookmark);
                _bookmarks.Save();
                spBookmarks.Children.Remove((sender as MenuItem).Tag as Button);
            }
            catch { }
        }
        
        private void bAddModulePanel_Click(object sender, RoutedEventArgs e)
        {
            Bookmark bm = (sender as Button).Tag as Bookmark;
            _moduleManager.LoadModule(bm.Module);
            _eventaggregator.GetEvent<ActivateModuleCopy>().Publish(new What2Do() { ModuleName = bm.Module, ModuleCommand = bm.ModuleCommand });
        }

        private void baddpanel_Click(object sender, RoutedEventArgs e)
        {
            //CloseableTabItem new_tab = new CloseableTabItem();
            //new_tab.Header = System.Environment.MachineName;
            //TabContainer.Items.Insert(TabContainer.Items.Count, new_tab);
            //((TabItem)TabContainer.Items[TabContainer.Items.Count - 1]).IsSelected = true;
            //DelegateAddContainer _addcontainer = AddContainer;
            //Application.Current.Dispatcher.BeginInvoke(_addcontainer, System.Windows.Threading.DispatcherPriority.Background, new_tab);

            //Загружаем модуль если он не загружен
            //TODO baddpanel_Click должен знать какой модуль загружать
            _moduleManager.LoadModule("ModuleLaExplorer");
            _eventaggregator.GetEvent<ActivateModuleCopy>().Publish(new What2Do() { ModuleName = "ModuleLaExplorer" });
            //_regionManager.Regions.Last().Views.First().GetType()
        }

        public void EventFromModule(What2Do obj)
        {
            Bookmark bm_saved = (from bm in _bookmarks.ModuleBookmarks
                                 where bm.Module == obj.ModuleName && bm.ModuleCommand.Except(obj.ModuleCommand).Count()==0
                                 select bm).FirstOrDefault();
            
            if (bm_saved == null)
            {
                _bookmarks.ModuleBookmarks.Add(new Bookmark() { Module = obj.ModuleName, ModuleCommand = obj.ModuleCommand });
                _bookmarks.Save();
                SyncBookmarkPanel();
            }
        }

        public void FillAddress(What2Do obj)
        {
            ObservableCollection<Commander> source = new ObservableCollection<Commander>();

            //Получить картинку по имепни модуля
            //_regionManager.Regions["TabRegion"].ActiveViews

            object header = (from mms in _regionManager.Regions["TabRegion"].ActiveViews
                             where (mms.GetType().GetProperty("My_module").GetValue(mms, null)).GetType().GetProperty("Name").GetValue((mms.GetType().GetProperty("My_module").GetValue(mms, null)), null) as string == obj.ModuleName
                             select mms.GetType().GetProperty("Header").GetValue(mms, null)).FirstOrDefault();


            BitmapImage modulelogo = header.GetType().GetProperty("HeaderLogo").GetValue(header, null) as BitmapImage;
            
            if (cbAddress.ItemsSource != null)
                source = new ObservableCollection<Commander>(cbAddress.ItemsSource as IEnumerable<Commander>);

            Commander command = (from l in source
                                where l.ModuleName == obj.ModuleName
                                && l.Command == obj.ModuleCommand.FirstOrDefault()
                                select l).FirstOrDefault();

            if (command == null)
            {
                command = new Commander()
                {
                    Command = obj.ModuleCommand.FirstOrDefault(),
                    ModuleName = obj.ModuleName,
                    ModuleImage = modulelogo
                };
                source.Add(command);
            }
            cbAddress.ItemsSource = source;
            cbAddress.SelectedItem = command;
        }

        private delegate void DelegateAddContainer(CloseableTabItem new_tab);
        private void AddContainer(CloseableTabItem new_tab)
        {
            Button b = new Button() { Name="askjdbaksdkasd", Content="Click me"};
            new_tab.Content = b;
        }

        private void CPanelChanged_handler(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //MessageBox.Show("The Property " + e.PropertyName + " changed");
            //CloseableTabItem i = new CloseableTabItem();
            //i = TabContainer.SelectedItem as CloseableTabItem;
            //Container k = (Container)i.Content;
            //lPanel.Content = k.PanelsInfo.CurrentPanelIndex;
            //tbAddress.Text = k.PanelsInfo.sPath;
            //if (k.PanelsInfo.sPath != null)
            //{
            //    if (k.PanelsInfo.sPath.IndexOf(@"\") != k.PanelsInfo.sPath.LastIndexOf(@"\"))
            //        i.Header = k.PanelsInfo.sPath.Substring(0, k.PanelsInfo.sPath.IndexOf(@"\") + 1) + "..." + k.PanelsInfo.sPath.Substring(k.PanelsInfo.sPath.LastIndexOf(@"\"));
            //    else
            //        i.Header = k.PanelsInfo.sPath;
            //}
            //else
            //    i.Header = System.Environment.MachineName;
        }

        private void CloseTab(object source, RoutedEventArgs args)
        {
            TabItem tabItem = args.Source as TabItem;
            if (tabItem != null)
            {
                TabControl tabControl = tabItem.Parent as TabControl;
                if (tabControl != null)
                    tabControl.Items.Remove(tabItem);
                GC.Collect();
            }
            if (TabContainer.Items.Count == 0)
                this.Close();
        }

        private void TabItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            
            //TabContainer.Items.Insert(TabContainer.Items.Count - 1, new_tab);
            //((TabItem)TabContainer.Items[TabContainer.Items.Count - 2]).IsSelected = true;
        }

        private void bHome_Click(object sender, RoutedEventArgs e)
        {
            List<string> commands = new List<string>();
            commands.Add("home");
            if (TabContainer.SelectedItem != null)
            {
                object module = TabContainer.SelectedItem.GetType().GetProperty("My_module").GetValue(TabContainer.SelectedItem, null);
                string ModuleName = module.GetType().GetProperty("Name").GetValue(module, null) as string;
                _eventaggregator.GetEvent<Command2Module>().Publish(new What2Do() { ModuleName = ModuleName, ModuleCommand = commands });
            }
        }

        private void window_initiateWiden(object sender, MouseButtonEventArgs e)
        {
            isWiden = true;
        }

        private void window_endWiden(object sender, MouseEventArgs e)
        {
            isWiden = false;
            // Make sure capture is released.
            Border rect = (Border)sender;
            rect.ReleaseMouseCapture();
        }
        private void window_Widen(object sender, MouseEventArgs e)
        {
            Border rect = (Border)sender;
            if (isWiden)
            {
                rect.CaptureMouse();
                double newWidth = e.GetPosition(this).X + 5;
                if (newWidth > 0) this.Width = newWidth;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void bClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void bMin_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void bRestore_Click(object sender, RoutedEventArgs e)
        {
            this.Height = SystemParameters.MaximizedPrimaryScreenHeight;
            this.Width = SystemParameters.MaximizedPrimaryScreenWidth;
            this.Top = 0;
            this.Left = 0;
        }

        private void tbAddress_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                List<string> commands = new List<string>();
                commands.Add(cbAddress.Text);
                if (TabContainer.SelectedItem != null)
                {
                    object module = TabContainer.SelectedItem.GetType().GetProperty("My_module").GetValue(TabContainer.SelectedItem, null);
                    string ModuleName = module.GetType().GetProperty("Name").GetValue(module, null) as string;
                    _eventaggregator.GetEvent<Command2Module>().Publish(new What2Do() { ModuleName = ModuleName, ModuleCommand = commands });
                }
            }
        }

        private void sshkitana_Click(object sender, RoutedEventArgs e)
        {
            //try
            //{
            //    CloseableTabItem i = new CloseableTabItem();
            //    i = TabContainer.SelectedItem as CloseableTabItem;
            //    i.Header = "KITANA";
            //    System.Windows.Forms.Integration.WindowsFormsHost s = new System.Windows.Forms.Integration.WindowsFormsHost();
            //    s = i.Content as System.Windows.Forms.Integration.WindowsFormsHost;
            //    WalburySoftware.TerminalControl k = (WalburySoftware.TerminalControl)s.Child;
            //    k.UserName = "sas";
            //    k.Password = "r7jE1$pY";
            //    k.Host = "kitana.samara.inside.mts.ru";
            //    //k.UserName = "builder";
            //    //k.Password = "johnik";
            //    //k.Host = "localhost";
            //    k.Method = WalburySoftware.ConnectionMethod.SSH2;

            //    k.Connect();

            //    k.SetPaneColors(System.Drawing.Color.LightGreen, System.Drawing.Color.Black);
            //}
            //catch (Exception r)
            //{
            //    MessageBox.Show(r.Message);
            //}
        }
        private void sshjade_Click(object sender, RoutedEventArgs e)
        {
            //try
            //{
            //    CloseableTabItem i = new CloseableTabItem();
            //    i = TabContainer.SelectedItem as CloseableTabItem;
            //    i.Header = "JADE";
            //    System.Windows.Forms.Integration.WindowsFormsHost s = new System.Windows.Forms.Integration.WindowsFormsHost();
            //    s = i.Content as System.Windows.Forms.Integration.WindowsFormsHost;
            //    WalburySoftware.TerminalControl k = (WalburySoftware.TerminalControl)s.Child;
            //    k.UserName = "sas";
            //    k.Password = "8L$kPd%Y";
            //    k.Host = "jade.samara.inside.mts.ru";
            //    //k.UserName = "builder";
            //    //k.Password = "johnik";
            //    //k.Host = "localhost";
            //    k.Method = WalburySoftware.ConnectionMethod.SSH2;

            //    k.Connect();

            //    k.SetPaneColors(System.Drawing.Color.LightGreen, System.Drawing.Color.Black);
            //}
            //catch (Exception r)
            //{
            //    MessageBox.Show(r.Message);
            //}
        }

        //private void bAddPanel_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        //{
        //    ContextMenu _firstmenu = (ContextMenu)this.TryFindResource("AddUtilityMenu");
        //    _firstmenu.IsOpen = true;
        //}

        private void OpenSSHConsole_Click(object sender, RoutedEventArgs e)
        {
            //CloseableTabItem new_tab = new CloseableTabItem();
            //new_tab.Header = System.Environment.MachineName;
            //TabContainer.Items.Insert(TabContainer.Items.Count, new_tab);
            //((TabItem)TabContainer.Items[TabContainer.Items.Count - 1]).IsSelected = true;
            //System.Windows.Forms.Integration.WindowsFormsHost s = new System.Windows.Forms.Integration.WindowsFormsHost();
            //WalburySoftware.TerminalControl newsshclient = new WalburySoftware.TerminalControl();
            ////newsshclient.Size = new System.Drawing.Size(622, 389);
            //newsshclient.Size = new System.Drawing.Size(Convert.ToInt16(this.RenderSize.Width), Convert.ToInt16(this.RenderSize.Height));
            //newsshclient.Name = "wall";
            //s.Child = newsshclient;
            //new_tab.Content = s;
        }

        private void bMax_Click(object sender, RoutedEventArgs e)
        {
            MaxWindow();
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void lWHeader_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MaxWindow();
        }

        private void MaxWindow()
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        public List<DependencyObject> FindChilds(DependencyObject reference, Type type)
        {
            List<DependencyObject> result = new List<DependencyObject>();
            if (reference != null)
            {
                int childrenCount = VisualTreeHelper.GetChildrenCount(reference);
                for (int i = 0; i < childrenCount; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(reference, i);
                    if (child.GetType() == type)
                        result.Add(child);
                    foreach (DependencyObject found in FindChilds(child, type))
                    {
                        if (found.GetType() == type)
                            result.Add(found);
                    }
                }
            }
            return result;
        }

        private void cbAddress_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            if (cbAddress.SelectedItem != null)
            {
                Image child = (from l in FindChilds(cbAddress, typeof(Image))
                               where (l as Image).Name == "PART_EditableImage"
                               select l).FirstOrDefault() as Image;
                child.Source = (cbAddress.SelectedItem as Commander).ModuleImage;
            }

            //if (childs.Count() != 0)
            //{
            //    foreach (DependencyObject child in childs)
            //    {
            //        if (((Explorer)child).PanelIndex == index)
            //            ((Explorer)child).GetItems(new Item { Type = FileTypes.FOLDER, sFullName = command == null || command == "home" ? null : command });
            //    }
            //}


            //Image comm = FindResource("ButtonCommon") as Style
        }
    }
}
