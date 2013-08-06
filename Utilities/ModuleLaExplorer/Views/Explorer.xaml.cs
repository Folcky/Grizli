using System;
using System.Collections;
using System.Collections.ObjectModel;
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
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Security.Principal;
using System.Management;
using System.Net;
using System.Threading;
using Microsoft.VisualBasic.FileIO;
using LaExplorer.Code;
using LaExplorer.Windows;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

namespace LaExplorer.Views
{
    /// <summary>
    /// Interaction logic for Explorer.xaml
    /// </summary>
    public partial class Explorer : UserControl, IDisposable
    {
        TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();

        //Accessors to Local, FTP, Network paths
        //Filled in "static Explorer()"
        ObservableCollection<IAccessor> accessors = new ObservableCollection<IAccessor>();
        
        Container Parent_container { get; set; }

        double verticaloffset=0;
        //double horizontaloffset=0;
        public struct FileCreateAndModifiedTime
        {
            public DateTime CreationTime;
            public DateTime LastWriteTime;
        }

        public Explorer(Container _container, string command)
        {
            InitializeComponent();

            accessors.Add(new LocalClient());
            accessors.Add(new NetworkClient());
            accessors.Add(new FTPClient());

            Protocols protocol_type;
            try
            {
                protocol_type=(Protocols)Enum.Parse(typeof(Protocols), command != "" ? command.Split('|').ElementAt(0) : null, true);
            }
            catch { protocol_type = Protocols.LOCAL; }
            //Protocols protocol_type = Protocols.LOCAL;
            string connection_name = command != "" ? command.Split('|').ElementAt(1) : null;
            string path = command != "" ? command.Split('|').ElementAt(2) : null;

            appdata_watcher.Created += new FileSystemEventHandler(OnAppDataUpdated);
            appdata_watcher.Deleted += new FileSystemEventHandler(OnAppDataUpdated);
            appdata_watcher.Changed += new FileSystemEventHandler(OnAppDataUpdated);
            appdata_watcher.Renamed += new RenamedEventHandler(OnAppDataUpdated);
            //usb_watcher.Changed += new USBWatcher.ChangedEventHandler(OnUSBUpdated);
            //usb_watcher.EnableRaisingEvents = true;

            if (protocol_type == Protocols.LOCAL)
            {
                FileAttributes attr;
                try
                {
                    attr = File.GetAttributes(path);
                }
                catch { attr = FileAttributes.Directory; }
                if (path==null || path.Trim() == "" || (attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    InitItems();
                }
                else
                    GetItems(new Item { Type = FileTypes.FILE, sFullName = path, Parent = new ParentItem() { Protocol = protocol_type } });

            }
            else
                GetItems(new Item { Type = FileTypes.FOLDER });
            Parent_container = _container;
        }

        protected int eventCounter = 0;
        private Accessor accessor = new Accessor();
        FileSystemWatcher appdata_watcher = new FileSystemWatcher(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Grizli");
        //USBWatcher usb_watcher = new USBWatcher();

        static Explorer()
        {
            _ListItems = DependencyProperty.Register("ListItems", typeof(Source),
                typeof(Explorer),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(OnItemsChanged)));
            _PanelIndex = DependencyProperty.Register("PanelIndex", typeof(int),
                typeof(Explorer));
        }

        public event PropertyChangedEventHandler LItemsChanged;
        public event PropertyChangedEventHandler CPanelChanged;

        public static DependencyProperty _ListItems;
        public static DependencyProperty _PanelIndex;

        public int PanelIndex
        {
            get
            {
                return (int)GetValue(_PanelIndex);
            }
            set
            {
                SetValue(_PanelIndex, value);
                OnCPanelChanged(new PropertyChangedEventArgs("PanelIndex"));
            }
        }
        public Source ListItems
        {
            get
            {
                return (Source)GetValue(_ListItems);
            }
            set
            {
                SetValue(_ListItems, value);
                OnLItemsChanged(new PropertyChangedEventArgs("ListItems"));
            }
        }

        public void OnCPanelChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            //Говорим что возникло событие
            if (CPanelChanged != null)
                CPanelChanged(this, e);
        }

        private static void OnItemsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            //int i = 1;
            //По идее здесь определим LItems извне
        }
        public void OnLItemsChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            //Говорим что возникло событие
            if (LItemsChanged != null)
                LItemsChanged(this, e);
            if (CPanelChanged != null)
                CPanelChanged(this, e);
        }

        public void ExecuteCommand(string command)
        {
            if (command == null || command == "home" || command.Trim() == "")
            {
                InitItems();
                this.Parent_container.PublishPath(this, "");
            }
            Regex local_path = new Regex(@"^\D{1,1}:(\\{1,1}\D{0,})?");
            if (local_path.IsMatch(command.TrimStart()) && Directory.Exists(command.TrimStart()))
            {
                GetItems(new Item { Type = FileTypes.FOLDER, sFullName = command.TrimStart(), Parent = new ParentItem { Protocol=Protocols.LOCAL } });
            }
        }

        public void InitItems()
        {
            try 
            {
                Source old_source = new Source();
                ParentItem parentitem = new ParentItem();
                parentitem.Protocol = Protocols.LOCAL;
                old_source.Connection_description = parentitem;
                old_source.iSource = new ObservableCollection<Item>();
                foreach (IAccessor ac in accessors)
                {
                    foreach (Item item in ac.InitItems().iSource)
                    {
                        old_source.iSource.Add(item);
                    }
                }
                if (old_source.iSource != null && old_source.iSource.Count()>0)
                {
                    lvList.ItemsSource = old_source.iSource;
                    this.ListItems = old_source;
                    if (sbPanelInfo.HasItems == true)
                        sbPanelInfo.Items[0] = "";
                    else
                        sbPanelInfo.Items.Add(ListItems.iSource.Count());

                    DelegateGetImagesForItems _updateimages = GetImagesForItems;
                    Application.Current.Dispatcher.BeginInvoke(_updateimages, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                }
            }
            catch { }

        }

        public void GetItems(Item senderitem)
        {
            try
            {
                Source old_source=null;
                if (senderitem.sFullName == "")
                {
                    InitItems();
                    return;
                }
                else
                {
                    IAccessor ac = AccessorFactory.GetAccessor(accessors, senderitem.Parent.Protocol);
                    old_source = ac.GetItems(senderitem);
                }
                
                if (old_source.iSource != null)
                {
                    lvList.ItemsSource = old_source.iSource;
                    this.ListItems = old_source;
                    //if (sbPanelInfo.HasItems == true)
                    //    sbPanelInfo.Items[0] = ac.GetItemsStatus();
                    //else
                    //    sbPanelInfo.Items.Add(ListItems.iSource.Count());
                    GetImagesForItems();
                    //DelegateGetImagesForItems _updateimages = GetImagesForItems;
                    //Application.Current.Dispatcher.BeginInvoke(_updateimages, System.Windows.Threading.DispatcherPriority.Background);
                }


                //    if (senderitem.Type == FileTypes.FOLDER || senderitem.Type == FileTypes.FTPLINK)
                //    {
                //        IAccessor ac = AccessorFactory.GetAccessor(accessors, senderitem.Parent.Protocol);
                //        Source old_source = ac.GetItems(senderitem);
                //        if (old_source.iSource != null)
                //        {
                //            s = old_source;
                //            DelegateGetImagesForItems _updateimages = GetImagesForItems;
                //            Application.Current.Dispatcher.BeginInvoke(_updateimages, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

                //            if (s.iSource != null)
                //            {
                //                iSource = s.iSource;
                //                lvList.ItemsSource = iSource;
                //                if (s.Connection_description.Path != null && s.Connection_description.Path != "" && s.Connection_description.Protocol == Protocols.LOCAL)
                //                {
                //                    appdata_watcher.EnableRaisingEvents = false;
                //                    //usb_watcher.EnableRaisingEvents = false;
                //                }
                //                else
                //                {
                //                    appdata_watcher.EnableRaisingEvents = true;
                //                    //usb_watcher.EnableRaisingEvents = true;
                //                }
                                // this.ListItems = s;
                //                if (sbPanelInfo.HasItems == true)
                //                    sbPanelInfo.Items[0] = ac.GetItemsStatus();
                //                else
                //                    sbPanelInfo.Items.Add(iSource.Count());
                //            }
                //        }
                //    }
                //    else
                //    {
                //        try
                //        {
                //            DelegateStartProccess _startproccess = StartProccess;
                //            Application.Current.Dispatcher.BeginInvoke(_startproccess, System.Windows.Threading.DispatcherPriority.Background, senderitem);
                //        }
                //        catch
                //        { }
                //    }
                //}
                //catch (Exception error)
                //{
                //    MessageBox.Show(error.Message);
                //}
            }
            catch { }
        }

        private delegate void DelegateStartProccess(Item args);
        private void StartProccess(Item item_to_start)
        {
            switch (item_to_start.Parent.Protocol)
            {
                case Protocols.LOCAL:
                    try
                    {
                        FileInfo fileinfo_was = new FileInfo(item_to_start.sFullName);
                        FileCreateAndModifiedTime file_was;
                        file_was.CreationTime = fileinfo_was.CreationTime;
                        file_was.LastWriteTime = fileinfo_was.LastWriteTime;
                        Process process_editor = Process.Start(item_to_start.sFullName);
                        if (process_editor != null)
                        {
                            process_editor.EnableRaisingEvents = true;
                            process_editor.Exited += delegate { process_editor_Exited(item_to_start, file_was); };
                        }
                    }
                    catch (Exception e)
                    { MessageBox.Show(e.Message); }
                    break;
                case Protocols.FTP:
                    FileOperationHelper whattodo = new FileOperationHelper();
                    var items = new ObservableCollection<Item>();
                    items.Add(item_to_start);
                    whattodo.FileList = items;
                    whattodo.Destination = new ParentItem(){ Path=System.IO.Path.GetTempPath(), Protocol=Protocols.LOCAL};
                    BackgroundWorker copyworker = new BackgroundWorker();
                    copyworker.WorkerReportsProgress = true;
                    copyworker.DoWork += new DoWorkEventHandler(Copy_DoWork);
                    copyworker.ProgressChanged += new ProgressChangedEventHandler(Copy_ReportProgress);
                    copyworker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Copy4FtpEdit_Completed);
                    copyworker.RunWorkerAsync(whattodo);
                    break;
            }
        }

        public void process_editor_Exited(Item item, FileCreateAndModifiedTime file_was)
        {
            //MessageBox.Show("Exited!");
            //TODO:Upload ftp file back to the host, if it was edited
            FileOperationHelper whattodo;
            switch (item.Parent.Protocol)
            {
                case Protocols.LOCAL:
                    break;
                case Protocols.FTP:
                    string file_name = String.Format(@"{0}\{1}", System.IO.Path.GetTempPath(), item.sName);
                    FileInfo fileinfo_now = new FileInfo(file_name);
                    if (fileinfo_now.CreationTime == file_was.CreationTime && fileinfo_now.LastWriteTime == file_was.LastWriteTime)
                        MessageBox.Show("Not edited");
                    else
                    {
                        whattodo = new FileOperationHelper();
                        whattodo.FileList.Add(new Item()
                        {
                            Type = FileTypes.FILE,
                            sName = item.sName,
                            sFullName = String.Format(@"{0}\{1}", System.IO.Path.GetTempPath(), item.sName),
                            Parent = new ParentItem() { Protocol = Protocols.LOCAL },
                            lSize = fileinfo_now.Length
                        });
                        whattodo.Destination = item.Parent;
                        whattodo.Destination.Path = item.sFullName.Substring(0, item.sFullName.LastIndexOf("/"));

                        BackgroundWorker copyworker = new BackgroundWorker();
                        //copyworker.WorkerReportsProgress = true;
                        copyworker.DoWork += new DoWorkEventHandler(Copy_DoWork);
                        //copyworker.ProgressChanged += new ProgressChangedEventHandler(Copy_ReportProgress);
                        copyworker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Copy_Completed);
                        copyworker.RunWorkerAsync(whattodo);
                    }
                    break;
            }
        }


        private delegate void DelegateAppDirUpdater(FileSystemEventArgs args);
        private void OnAppDataUpdated(object sender, FileSystemEventArgs e)
        {
            DelegateAppDirUpdater _updatedir = RefreshDrives;
            Application.Current.Dispatcher.BeginInvoke(_updatedir, System.Windows.Threading.DispatcherPriority.Background, e);
        }

        private delegate void DelegateDriveUpdater(EventArgs e);
        private void OnUSBUpdated(object sender, EventArgs e)
        {
            DelegateDriveUpdater _updatedir = RefreshDrives;
            Application.Current.Dispatcher.BeginInvoke(_updatedir, System.Windows.Threading.DispatcherPriority.Background, e);
        }

        private void RefreshDrives(EventArgs args)
        {
            GetItems(new Item { Type = FileTypes.FOLDER, sFullName = null });
        }

        private delegate void DelegateGetImagesForItems();
        private void GetImagesForItems()
        {
            IEnumerable<Item> to_proccess = from l in lvList.Items.Cast<Item>()
                                            where lvList.ItemContainerGenerator.ContainerFromItem(l) as ListViewItem != null
                                            && l.iImage == null
                                            select l;

            foreach (Protocols protocol in to_proccess.Select(s=>s.Parent.Protocol).Distinct())
            {
                IAccessor ac = AccessorFactory.GetAccessor(accessors, protocol);
                if (ac != null)
                {
                    IEnumerable<Item> to_proccess_with_protocol = to_proccess.Where(w => w.Parent.Protocol == protocol);
                    //Async method
                    ac.DefineIcons(to_proccess_with_protocol, scheduler);
                }
            }

            //IEnumerable<Item> ftps = from l in to_proccess
            //                             where l.Parent.Protocol == Protocols.FTP
            //                             select l;
            //foreach (Item _lvitem in ftps)
            //{
            //    if (_lvitem.iImage == null)
            //        _lvitem.iImage = accessor.GetIcon(new FileInfo(_lvitem.sName));
            //}
        }

        public void Move(ref ListViewItem sender)
        {
            DragDrop.DoDragDrop(sender, sender.Content, DragDropEffects.Copy);
        }

        protected void lvListItem_LeftDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender.GetType() == typeof(ListViewItem) && e.LeftButton == MouseButtonState.Pressed)
                {
                    ListViewItem listviewitem = sender as ListViewItem;
                    if (listviewitem.Content != null && listviewitem.Content.GetType() == typeof(Item))
                    {
                        Item choosed_item = listviewitem.Content as Item;
                        GetItems(choosed_item);
                    }
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            GridView _lgrid = (GridView)lvList.View;
            _lgrid.Columns[0].Width = (int)200;
        }

        protected void lwLItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1 && e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Released)
            {
                //RGrid_FileOp.Visibility = Visibility.Visible;
                ListViewItem lbl = (ListViewItem)sender;
                //DragDrop.DoDragDrop(listBox1, listBox1.Items, DragDropEffects.All);
            }
        }

        private void lvList_GotFocus(object sender, RoutedEventArgs e)
        {
            if (CPanelChanged != null)
            {
                PropertyChangedEventArgs s = new PropertyChangedEventArgs("CPanel");
                CPanelChanged(this, s);
            }
        }

        public virtual void Dispose()
        {
            //int i = 1;
        }

        private void lvList_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F4:
                    //_parent_container.Number_of_panels = 3;
                    break;
                case Key.Delete:
                    ObservableCollection<Item> choosed = new ObservableCollection<Item>(
                    from l in ((ListView)sender).SelectedItems.Cast<Item>()
                    //Чтол это за хрень
                    //where l.bFolder != 3
                    orderby l.sName
                    select l);

                    FileOperationHelper whattodo = new FileOperationHelper();
                    whattodo.FileList = new ObservableCollection<Item>(choosed);
                    DeleteWindow s = new DeleteWindow();
                    s.Objects = choosed;
                    s.ShowDialog();
                    if (s.Ok)
                    {
                        BackgroundWorker deleteworker = new BackgroundWorker();
                        deleteworker.WorkerReportsProgress = true;
                        deleteworker.DoWork += new DoWorkEventHandler(Delete_DoWork);
                        deleteworker.ProgressChanged += new ProgressChangedEventHandler(Delete_ReportProgress);
                        deleteworker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Delete_Completed);
                        deleteworker.RunWorkerAsync(whattodo);
                    }
                    break;
                case Key.F5:
                    ObservableCollection<Item> selected_items = new ObservableCollection<Item>(((ListView)sender).SelectedItems.Cast<Item>());
                    if (ListItems.Connection_description.Path != "" && selected_items.Count()>0)
                    {
                        ObservableCollection<string> paths = new ObservableCollection<string>(from l in Parent_container.PanelsInfo.sSources
                                                                                              where l.ListItems.Connection_description.Path != null
                                                                                              //&& l.PanelIndex != this.PanelIndex
                                                                                              select l.ListItems.Connection_description.Path);
                        ObservableCollection<ParentItem> destinations = new ObservableCollection<ParentItem>(from l in Parent_container.PanelsInfo.sSources
                                                                                                             select l.ListItems.Connection_description);
                        CopyWindow copywindow = new CopyWindow();
                        copywindow.Paths = paths;
                        copywindow.Destinations = destinations;
                        copywindow.ShowDialog();
                        if (copywindow.Ok)
                        {
                            whattodo = new FileOperationHelper();
                            whattodo.FileList = selected_items;
                            //  Z:\*.*  ??
                            whattodo.ToMask = (string)copywindow.cbPaths.SelectedValue;
                            //Регулярные выражения или какие стандартные маски файлов бывают
                            whattodo.Filter = (string)copywindow.cbFilter.SelectedValue;
                            whattodo.Destination = copywindow.Destination;
                            BackgroundWorker copyworker = new BackgroundWorker();
                            copyworker.WorkerReportsProgress = true;
                            copyworker.DoWork += new DoWorkEventHandler(Copy_DoWork);
                            copyworker.ProgressChanged += new ProgressChangedEventHandler(Copy_ReportProgress);
                            copyworker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Copy_Completed);
                            copyworker.RunWorkerAsync(whattodo);
                        }
                    }
                    break;
            }
        }

        private void Delete_DoWork(object sender, DoWorkEventArgs e)
        {
            foreach (Item item in ((FileOperationHelper)e.Argument).FileList)
            {
                try
                {
                    switch (item.Parent.Protocol)
                    { 
                        case Protocols.FTP:
                            if (accessor.DeleteFtpFile(item))
                            {
                                (sender as BackgroundWorker).ReportProgress(100, item);
                            }
                            break;
                        case Protocols.LOCAL:
                            if (File.Exists(item.sFullName))
                            {
                                FileSystem.DeleteFile(item.sFullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                            }
                            else
                                if (Directory.Exists(item.sFullName))
                                {
                                    //не работает, на непустых директориях c read-only файлами
                                    //Directory.Delete(item.sFullName, true);
                                    accessor.DeleteRecursiveFolder(item.sFullName);
                                }
                            break;
                        case Protocols.NETWORK:
                            using (new Accessor.Impersonation(item.Parent.Domain, item.Parent.User, item.Parent.Password))
                            {
                                if (File.Exists(@"\\" + item.Parent.Host + @"\" + item.sFullName))
                                {
                                    FileSystem.DeleteFile(@"\\" + item.Parent.Host + @"\" + item.sFullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                                    (sender as BackgroundWorker).ReportProgress(100, item);
                                }
                                else
                                    if (Directory.Exists(@"\\" + item.Parent.Host + @"\" + item.sFullName))
                                    {
                                        //не работает, на непустых директориях c read-only файлами
                                        //Directory.Delete(item.sFullName, true);
                                        accessor.DeleteRecursiveFolder(@"\\" + item.Parent.Host + @"\" + item.sFullName);
                                        (sender as BackgroundWorker).ReportProgress(100, item);
                                    }
                            }
                            break;
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }

        private void Delete_ReportProgress(object sender, ProgressChangedEventArgs e)
        {
            Item item = (Item)e.UserState;

            ObservableCollection<Item> spisok = (ObservableCollection<Item>)(lvList.ItemsSource);
            lvList.ItemsSource = new ObservableCollection<Item>(
                from m in spisok
                where m != item
                select m
                );
        }
        private void Delete_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
        }

        private Stream GetFTPStream(Item source, ParentItem destination, string method)
        {
            string dir = "";
            string user = "";
            string password = "";
            bool passivemode = false;
            switch (source.Parent.Protocol)
            {
                case (Protocols.LOCAL):
                    dir = destination.Host + "//%2f" + destination.Path + @"/" + source.sName;
                    user = destination.User;
                    password = destination.Password;
                    passivemode = (bool)destination.PassiveMode;
                    break;
                case (Protocols.FTP):
                    dir = source.Parent.Host + "//%2f" + source.sFullName;
                    user = source.Parent.User;
                    password = source.Parent.Password;
                    passivemode = (bool)source.Parent.PassiveMode;
                    break;
            }
            if (dir.IndexOf("ftp://") != 1)
                dir = "ftp://" + dir;
            FtpWebRequest reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(dir));
            reqFTP.UseBinary = true;
            reqFTP.Credentials = new NetworkCredential(user, password);
            reqFTP.Method = method;
            reqFTP.UsePassive = (bool)passivemode;
            reqFTP.Proxy = null;
            switch (source.Parent.Protocol)
            {
                case (Protocols.LOCAL):
                    //UPLOAD
                    return reqFTP.GetRequestStream();
                case (Protocols.FTP):
                    //DOWNLOAD
                    WebResponse response = reqFTP.GetResponse();
                    return response.GetResponseStream();
                default:
                    return null;
            }
        }

        private Stream GetNETWORKStream(Item item, FileAccess method)
        {
            using (new Accessor.Impersonation(item.Parent.Domain, item.Parent.User, item.Parent.Password))
            {
                switch (method)
                {
                    case FileAccess.Read:
                        return File.Open(@"\\" + item.Parent.Host + @"\" + item.sFullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    case FileAccess.Write:
                        return File.Open(@"\\" + item.Parent.Host + @"\" + item.sFullName, FileMode.Create);
                    default:
                        return null;
                }
            }
        }

        private void FileCopy_proccessor(BackgroundWorker whom2report, Stream read_stream, Stream write_stream, Item item)
        {
            int Length = 2048;
            Byte[] buffer = new Byte[Length];
            int bytesRead = read_stream.Read(buffer, 0, Length);
            UInt64 totalBytes = 0;
            int i = 0;
            int k = 0;
            while (bytesRead > 0)
            {
                totalBytes += (UInt64)bytesRead;
                write_stream.Write(buffer, 0, bytesRead);
                bytesRead = read_stream.Read(buffer, 0, Length);
                if (whom2report!=null && whom2report.WorkerReportsProgress)
                {
                    k = Convert.ToInt32(Convert.ToDecimal(totalBytes) / Convert.ToDecimal(item.lSize) * 100);
                    if (i != k)
                    {
                        i = k;
                        whom2report.ReportProgress(i, item);
                    }
                }
            }
            read_stream.Close();
            write_stream.Close();
        }

        private void Copy_DoWork(object sender, DoWorkEventArgs e)
        {
            FileOperationHelper whattodo = (FileOperationHelper)e.Argument;
            //Container.SetRotation(sender, 2);
            foreach (Item item in whattodo.FileList)
            {
                IAccessor ac_reader = AccessorFactory.GetAccessor(accessors, item.Parent.Protocol);
                IAccessor ac_writer = AccessorFactory.GetAccessor(accessors, whattodo.Destination.Protocol);
                Stream read_stream = ac_reader.GetReadStream(item);
                Stream write_stream = ac_writer.GetWriteStream(whattodo.Destination, item);

                //UPLOAD FTP
                if (item.Parent.Protocol == Protocols.LOCAL && item.Type == FileTypes.FILE && whattodo.Destination.Protocol == Protocols.FTP)
                {
                    write_stream = GetFTPStream(item, whattodo.Destination, WebRequestMethods.Ftp.UploadFile);
                    read_stream = File.OpenRead(item.sFullName);
                    FileCopy_proccessor(sender as BackgroundWorker, read_stream, write_stream, item);
                }

                //DOWNLOAD FTP
                if (item.Parent.Protocol == Protocols.FTP && item.Type == FileTypes.FILE && whattodo.Destination.Protocol == Protocols.LOCAL)
                {
                    read_stream = GetFTPStream(item, whattodo.Destination, WebRequestMethods.Ftp.DownloadFile);
                    write_stream = new FileStream(whattodo.Destination.Path + @"\" + item.sName, FileMode.Create);
                    FileCopy_proccessor(sender as BackgroundWorker, read_stream, write_stream, item);
                }

                //UPLOAD NETWORK FILE
                if (item.Parent.Protocol == Protocols.LOCAL && item.Type == FileTypes.FILE && whattodo.Destination.Protocol == Protocols.NETWORK)
                {
                    read_stream = File.Open(item.sFullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    write_stream = GetNETWORKStream(new Item() { Parent = whattodo.Destination, sFullName = whattodo.Destination.Path + @"\" + item.sName }, FileAccess.Write);
                    FileCopy_proccessor(sender as BackgroundWorker, read_stream, write_stream, item);
                }

                //DOWNLOAD NETWORK FILE
                if (item.Parent.Protocol == Protocols.NETWORK && item.Type == FileTypes.FILE && whattodo.Destination.Protocol == Protocols.LOCAL)
                {
                    read_stream = GetNETWORKStream(item, FileAccess.Read);
                    write_stream = new FileStream(whattodo.Destination.Path + @"\" + item.sName, FileMode.Create);
                    FileCopy_proccessor(sender as BackgroundWorker, read_stream, write_stream, item);
                }

                //COPY FILE LOCALLY
                if (item.Parent.Protocol == Protocols.LOCAL && item.Type == FileTypes.FILE && whattodo.Destination.Protocol == Protocols.LOCAL)
                {
                    read_stream = File.Open(item.sFullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    write_stream = File.Open(whattodo.Destination.Path + @"\" + item.sName, FileMode.Create, FileAccess.Write, FileShare.None);
                    FileCopy_proccessor(sender as BackgroundWorker, read_stream, write_stream, item);
                }

                //COPY FOLDER LOCALLY
                if (item.Parent.Protocol == Protocols.LOCAL && item.Type == FileTypes.FOLDER && whattodo.Destination.Protocol == Protocols.LOCAL)
                {
                    (sender as BackgroundWorker).ReportProgress(0, item);

                    string[] dirPaths = Directory.GetDirectories(item.sFullName, "*", System.IO.SearchOption.AllDirectories);
                    string[] filePaths = Directory.GetFiles(item.sFullName, "*", System.IO.SearchOption.AllDirectories);

                    int _operations_number = filePaths.Count() + dirPaths.Count();
                    int _finished_operations_number = 0;
                    int _progress = 0;
                    int _fprogress = 0;

                    foreach (string dirPath in dirPaths)
                    {
                        if (!Directory.Exists(dirPath.Replace(item.sFullName, whattodo.Destination + @"\" + item.sName)))
                            Directory.CreateDirectory(dirPath.Replace(item.sFullName, whattodo.Destination + @"\" + item.sName));
                        _finished_operations_number++;
                        _progress = Convert.ToInt32(Convert.ToDecimal(_finished_operations_number) / Convert.ToDecimal(_operations_number) * 100);
                        if (_progress != _fprogress)
                        {
                            _fprogress = _progress;
                            (sender as BackgroundWorker).ReportProgress(_progress, item);
                            //_parent_container.Number_of_panels = k;
                        }
                    }

                    foreach (string filePath in filePaths)
                    {
                        if (!File.Exists(filePath.Replace(item.sFullName, whattodo.Destination + @"\" + item.sName)))
                            File.Copy(filePath, filePath.Replace(item.sFullName, whattodo.Destination + @"\" + item.sName));
                        _finished_operations_number++;
                        _progress = Convert.ToInt32(Convert.ToDecimal(_finished_operations_number) / Convert.ToDecimal(_operations_number) * 100);
                        if (_progress != _fprogress)
                        {
                            _fprogress = _progress;
                            (sender as BackgroundWorker).ReportProgress(_progress, item);
                            //_parent_container.Number_of_panels = k;
                        }
                    }
                }
            }
            e.Result = e.Argument;
        }

        private void Copy_ReportProgress(object sender, ProgressChangedEventArgs e)
        {
            Item item = (Item)e.UserState;
            if (item.RHeight != 5)
                item.RHeight = 5;
            item.bPBVisible = Visibility.Visible;
            item.iProgress = e.ProgressPercentage;
        }
        private void Copy_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            foreach (Item item in ((FileOperationHelper)e.Result).FileList)
            {
                item.RHeight = 0;
                item.bPBVisible = Visibility.Hidden;
                item.iProgress = 0;
            }
        }

        private void Copy4FtpEdit_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            foreach (Item item in ((FileOperationHelper)e.Result).FileList)
            {
                item.RHeight = 0;
                item.bPBVisible = Visibility.Hidden;
                item.iProgress = 0;

                string file_name = String.Format(@"{0}\{1}", System.IO.Path.GetTempPath(), item.sName);
                FileInfo fileinfo_was = new FileInfo(file_name);
                FileCreateAndModifiedTime file_was;
                file_was.CreationTime = fileinfo_was.CreationTime;
                file_was.LastWriteTime = fileinfo_was.LastWriteTime;
                Process process_editor = Process.Start(file_name);
                if (process_editor != null)
                {
                    process_editor.EnableRaisingEvents = true;
                    process_editor.Exited += delegate { process_editor_Exited(item, file_was); };
                }
            }
        }

        private void lvList_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset != verticaloffset)
            {
                DelegateGetImagesForItems _updateimages = GetImagesForItems;
                Application.Current.Dispatcher.BeginInvoke(_updateimages, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                verticaloffset=e.VerticalOffset;
            }
        }

        private void NameAsc_Click(object sender, RoutedEventArgs e)
        {
            ListItems.iSource = new ObservableCollection<Item>(
                (from m in ListItems.iSource.OrderBy(p => p.sName)
                 where m.Type == FileTypes.FOLDER
                 select m).Union(
                from m in ListItems.iSource.OrderBy(p => p.sName)
                where m.Type == FileTypes.FTPLINK
                select m
                ).Union(
                from m in ListItems.iSource.OrderBy(p => p.sName)
                where m.Type == FileTypes.FILE
                select m
                )
                );
            lvList.ItemsSource = ListItems.iSource;
        }

        private void AddFTP_Click(object sender, RoutedEventArgs e)
        {
            ManageConnections s = new ManageConnections();
            s.ShowDialog();
            //TODO перегрузить списки соединений
            //так нельзя перегружать, перегрузиться только в текущем окне
            //GetItems(new Item { Type = FileTypes.FOLDER });
        }
        
        private void CreateBookmark_Click(object sender, RoutedEventArgs e)
        {
            this.Parent_container.PublishBookmark(this, ((sender as MenuItem).Tag as Item).sFullName);
        }

        private void NameDesc_Click(object sender, RoutedEventArgs e)
        {
            ListItems.iSource = new ObservableCollection<Item>((from m in ListItems.iSource.OrderByDescending(p => p.sName)
                                                      where m.Type == FileTypes.FOLDER
                                                      select m).Union(
                from m in ListItems.iSource.OrderByDescending(p => p.sName)
                where m.Type == FileTypes.FTPLINK
                select m
                ).Union(
                from m in ListItems.iSource.OrderByDescending(p => p.sName)
                where m.Type == FileTypes.FILE
                select m
                ));
            lvList.ItemsSource = ListItems.iSource;
        }

        private void lvList_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            //MessageBox.Show("menu");
            if (ListItems.Connection_description.Path == null || ListItems.Connection_description.Path == "")
            {
                ContextMenu _firstmenu = (ContextMenu)this.TryFindResource("FirstMenu");
                _firstmenu.IsOpen = true;
            }
        }

        private void lvListItem_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            //MessageBox.Show("menu");
            if (sender!=null && sender.GetType() == typeof(ListViewItem) && ((sender as ListViewItem).DataContext as Item).sFullName !=null)
            {
                ContextMenu _filemenu = (ContextMenu)this.TryFindResource("FileMenu");
                foreach (object menuitem in _filemenu.Items)
                { 
                    if(menuitem.GetType()==typeof(MenuItem) && (menuitem as MenuItem).Name=="miCreateBookmark")
                        (menuitem as MenuItem).Tag = ((sender as ListViewItem).DataContext as Item);
                }
                _filemenu.IsOpen = true;
            }
        }

        private void lvList_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            DelegateGetImagesForItems _updateimages = GetImagesForItems;
            Application.Current.Dispatcher.BeginInvoke(_updateimages, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
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

        private void UserControl_GotFocus(object sender, RoutedEventArgs e)
        {
            this.Parent_container.PublishPath(this, ListItems.Connection_description.Path);
        }

        private void lvList_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    if (((ListView)sender).SelectedItems.Count != 0)
                        GetItems((Item)((ListView)sender).SelectedItems[0]);
                    break;
            }
        }
    }
}
