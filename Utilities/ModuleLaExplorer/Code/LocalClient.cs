using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//MY
using System.Security;
using System.Security.Principal;
using System.IO;
using System.Windows;
using System.Collections.ObjectModel;
using System.Security.AccessControl;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LaExplorer.Code
{
    public class LocalClient : IAccessor
    {
        public struct FileCreateAndModifiedTime
        {
            public DateTime CreationTime;
            public DateTime LastWriteTime;
        }

        FileSystemWatcher file_watcher = new FileSystemWatcher();

        public LocalClient()
        {
            try
            {
                this.User = System.Security.Principal.WindowsIdentity.GetCurrent();
            }
            catch { }

            file_watcher.Created += new FileSystemEventHandler(ItemsUpdated);
            file_watcher.Deleted += new FileSystemEventHandler(ItemsUpdated);
            file_watcher.Changed += new FileSystemEventHandler(ItemsUpdated);
            file_watcher.Renamed += new RenamedEventHandler(ItemsUpdated);
            file_watcher.EnableRaisingEvents = false;
        }

        private List<Ext_Icon> _knowedicons = new List<Ext_Icon>();
        public List<Ext_Icon> KnowedIcons
        {
            get { return _knowedicons; }
        }

        private Source _currentItems = new Source();
        public Source CurrentItems
        {
            get { return _currentItems; }
            set { _currentItems = value; }
        }

        private Protocols _protocol = Protocols.LOCAL;
        public Protocols Protocol()
        {
            return _protocol;
        }

        private WindowsPrincipal _principal;
        public WindowsPrincipal Principal
        {
            get { return _principal; }
            set
            {
                if (value != null && value.GetType() == typeof(WindowsPrincipal))
                    _principal = value;
            }
        }

        private WindowsIdentity _user;
        public WindowsIdentity User
        {
            get { return _user; }
            set
            {
                if (value != null && value.GetType() == typeof(WindowsIdentity))
                {
                    _user = value;
                    this.Principal = new WindowsPrincipal(value);
                }
            }
        }

        private List<PredefinedImage> _predefined_images = new List<PredefinedImage>();
        private List<PredefinedImage> Predefined_images
        {
            get { return _predefined_images; }
        }
        public BitmapImage GetPredefinedImage(string name)
        {
            try
            {
                BitmapImage result = (from l in Predefined_images
                                      where l.Image_name == name
                                      select l.Image).FirstOrDefault();
                if (result == null)
                {
                    ResourceDictionary resourceDictionary = new ResourceDictionary();
                    resourceDictionary.Source = new Uri("ModuleLaExplorer;component/Resources/Dictionary.xaml", UriKind.Relative);
                    PredefinedImage to_add = new PredefinedImage() { Image_name = name, Image = (BitmapImage)resourceDictionary[name] };
                    Predefined_images.Add(to_add);
                    result = to_add.Image;
                }
                return result;
            }
            catch { return new BitmapImage(); }
        }

        public bool CheckListAccess(WindowsIdentity user, WindowsPrincipal principal, DirectoryInfo directory)
        {
            // These are set to true if either the allow read or deny read access rights are set
            bool allowList = false;
            bool denyList = false;

            try
            {
                // Get the collection of authorization rules that apply to the current directory
                AuthorizationRuleCollection acl = directory.GetAccessControl().GetAccessRules(true, true, typeof(SecurityIdentifier));

                for (int x = 0; x < acl.Count; x++)
                {
                    FileSystemAccessRule currentRule = (FileSystemAccessRule)acl[x];
                    // If the current rule applies to the current user
                    if (user.User.Equals(currentRule.IdentityReference) || principal.IsInRole((SecurityIdentifier)currentRule.IdentityReference))
                    {
                        if
                        (currentRule.AccessControlType.Equals(AccessControlType.Deny))
                        {
                            if ((currentRule.FileSystemRights & FileSystemRights.ListDirectory) == FileSystemRights.ListDirectory)
                            {
                                denyList = true;
                            }
                        }
                        else if
                        (currentRule.AccessControlType.Equals(AccessControlType.Allow))
                        {
                            if ((currentRule.FileSystemRights & FileSystemRights.ListDirectory) == FileSystemRights.ListDirectory)
                            {
                                allowList = true;
                            }
                        }
                    }
                }
            }
            catch { return false; }

            if (allowList & !denyList)
                return true;
            else
                return false;
        }

        public Source InitItems()
        {
            Source result = new Source();
            ParentItem parentitem = new ParentItem();
            parentitem.Protocol = Protocol();
            int[] i = new int[] { 1 };
            ObservableCollection<Item> items = new ObservableCollection<Item>
                (from l in DriveInfo.GetDrives()
                 select new Item
                 {
                     sName = l.Name,
                     sFullName = l.Name,
                     Type = FileTypes.FOLDER,
                     Parent = new ParentItem { Protocol = Protocol() },
                     sExt = l.DriveType.ToString(),
                     iImage = GetPredefinedImage(l.DriveType.ToString() + "Drive"),
                     lSize = l.DriveType.ToString() == "Fixed" || l.DriveType.ToString() == "Removable" ? l.TotalSize : 0,
                     dDate = l.DriveType.ToString() == "Fixed" ? l.RootDirectory.LastWriteTime : DateTime.Today,
                     bPBVisible = Visibility.Hidden,
                     RHeight = 0
                 });
            result.iSource = items;
            result.Connection_description = parentitem;
            CurrentItems = result;
            return result;
        }
        public Source GetItems(Item diritem)
        {
            Source result = new Source();
            ParentItem parentitem = new ParentItem();
            parentitem.Protocol = Protocol();
            parentitem.Path = diritem.sFullName;
            int[] i = new int[] { 1 };
            if (diritem.Type == FileTypes.FOLDER)
            {
                List<Ext_Icon> _licons = new List<Ext_Icon>();
                DirectoryInfo ldir = new DirectoryInfo(diritem.sFullName);
                if (CheckListAccess(this.User, this.Principal, ldir))
                {
                    ObservableCollection<Item> items = new ObservableCollection<Item>((from l in i
                                                                                       select new Item
                                                                                       {
                                                                                           sName = ". . .",
                                                                                           sFullName = ldir.Parent == null ? "" : ldir.Parent.FullName,
                                                                                           iImage = GetPredefinedImage("UpFolderIcon"),
                                                                                           Type = FileTypes.FOLDER,
                                                                                           Parent = parentitem,
                                                                                           bPBVisible = Visibility.Hidden,
                                                                                           RHeight = 0
                                                                                       }).Union(
                                               from l in ldir.GetDirectories()
                                               select new Item
                                               {
                                                   sName = l.Name,
                                                   sFullName = l.FullName,
                                                   Type = FileTypes.FOLDER,
                                                   dDate = l.LastWriteTime,
                                                   Parent = parentitem,
                                                   bPBVisible = Visibility.Hidden,
                                                   RHeight = 0
                                               }).Union(
                                               from l in ldir.GetFiles()
                                               select new Item
                                               {
                                                   sName = l.Name,
                                                   sFullName = l.FullName,
                                                   Type = FileTypes.FILE,
                                                   sExt = l.Extension,
                                                   dDate = l.LastWriteTime,
                                                   lSize = l.Length,
                                                   Parent = parentitem,
                                                   bPBVisible = Visibility.Hidden,
                                                   RHeight = 0
                                               }
                                               ));
                    result.iSource = items;
                    result.Connection_description = parentitem;
                    file_watcher.Path = result.Connection_description.Path;
                    file_watcher.Filter = "*.*";
                    file_watcher.IncludeSubdirectories = false;
                    file_watcher.EnableRaisingEvents = true;

                }
                CurrentItems = result;
                return result;
            }
            else if (diritem.Type == FileTypes.FILE)
            {
                try
                {
                    DelegateStartProccess _startproccess = StartProccess;
                    Application.Current.Dispatcher.BeginInvoke(_startproccess, System.Windows.Threading.DispatcherPriority.Background, diritem);
                }
                catch
                { }
            }
            return null;
        }

        private delegate void DelegateDirUpdater(FileSystemEventArgs args);
        private void ItemsUpdated(object sender, FileSystemEventArgs e)
        {
            DelegateDirUpdater _updatedir = RefreshItems;
            Application.Current.Dispatcher.BeginInvoke(_updatedir, System.Windows.Threading.DispatcherPriority.Background, e);
        }

        private void RefreshItems(FileSystemEventArgs args)
        {
            switch (args.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    try
                    {
                        Item created = GetItem(args.FullPath);
                        if (created != null)
                        {
                            FileInfo created_file = new FileInfo(created.sFullName);
                            if (created != null && created_file.DirectoryName == CurrentItems.Connection_description.Path)
                                CurrentItems.iSource.Add(created);
                        }
                    }
                    catch { }
                    break;
                case WatcherChangeTypes.Renamed:
                    Item record = (from r in CurrentItems.iSource
                                   where r.sFullName == ((RenamedEventArgs)args).OldFullPath
                                   select r).FirstOrDefault();
                    record.sName = record.Type == FileTypes.FOLDER ? (new DirectoryInfo(((RenamedEventArgs)args).FullPath)).Name : (new FileInfo(((RenamedEventArgs)args).FullPath)).Name;
                    record.sFullName = ((RenamedEventArgs)args).FullPath;
                    break;
                case WatcherChangeTypes.Deleted:
                    Item to_delete = (from r in CurrentItems.iSource
                                      where r.sFullName == args.FullPath
                                      select r).FirstOrDefault();
                    CurrentItems.iSource.Remove(to_delete);
                    break;
                //default:
                //TODO
                //break;
            }
        }

        public Item GetItem(string fullpath)
        {
            int[] i = new int[] { 1 };
            DirectoryInfo dir = new DirectoryInfo(fullpath);
            FileInfo file = new FileInfo(fullpath);
            if (dir.Exists)
            {
                Item item = new Item
                {
                    sName = dir.Name,
                    sFullName = dir.FullName,
                    Type = FileTypes.FOLDER,
                    dDate = dir.LastWriteTime,
                    Parent = new ParentItem { Protocol = Protocols.LOCAL },
                    bPBVisible = Visibility.Hidden,
                    RHeight = 0
                };
                IEnumerable<Item> list = from l in i
                                         select item;
                //DefineIcons(list);
                return item;
            }
            if (file.Exists)
            {
                Item item = new Item
                {
                    sName = file.Name,
                    sFullName = file.FullName,
                    Type = FileTypes.FILE,
                    sExt = file.Extension,
                    dDate = file.LastWriteTime,
                    lSize = file.Length,
                    Parent = new ParentItem { Protocol = Protocol() },
                    bPBVisible = Visibility.Hidden,
                    RHeight = 0
                };
                IEnumerable<Item> list = from l in i
                                         select item;
                //DefineIcons(list);
                return item;
            }
            else
                return null;
        }

        public void DefineIcons(IEnumerable<Item> items, TaskScheduler scheduler)
        {
            foreach (Item item in items)
            {
                BitmapImage result = null;
                switch (item.Type)
                {
                        
                    case FileTypes.FOLDER:
                        result = CheckListAccess(User, Principal, new DirectoryInfo(item.sFullName)) ? GetPredefinedImage("FolderIcon") : GetPredefinedImage("LockedFolderIcon");
                        item.iImage = result;
                        break;
                    case FileTypes.FILE:
                        result = null;
                        FileInfo _file = new FileInfo(item.sFullName);
                        if (_file.Extension.ToLower() == ".exe" || _file.Extension.ToLower() == ".lnk" || _file.Extension.ToLower() == ".bmp" || _file.Extension.ToLower() == ".ico")
                            //Дорогая операция
                            result = Icon.ExtractAssociatedIcon(_file.FullName).ToBitmap().ToBitmapImage();
                        result = (from k in KnowedIcons
                                              where k.sExt == _file.Extension
                                              select k.iImage).FirstOrDefault();
                        if (result == null)
                        {
                            //Дорогая операция
                            result = Icon.ExtractAssociatedIcon(_file.FullName).ToBitmap().ToBitmapImage();
                            KnowedIcons.Add(new Ext_Icon
                            {
                                sExt = _file.Extension.ToLower(),
                                iImage = result
                            });
                        }
                        item.iImage=result;
                        break;
                    default:
                        result = null;
                        break;
                }
            }
            
        }

        public string GetItemsStatus()
        {
            return (from l in CurrentItems.iSource
                    where
                    l.Type == FileTypes.FILE
                    select l).Count() + " files " +
                    (from l in CurrentItems.iSource
                     where
                     (l.Type == FileTypes.FOLDER || l.Type == FileTypes.FTPLINK)
                     && l.sName != ". . ."
                     select l).Count() + " directories";
        }

        private delegate void DelegateStartProccess(Item args);
        private void StartProccess(Item item_to_start)
        {
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
        }
        public void process_editor_Exited(Item item, FileCreateAndModifiedTime file_was)
        {
        }

        public Stream GetReadStream(Item item)
        {
            return File.Open(item.sFullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public Stream GetWriteStream(ParentItem Destination, Item item)
        {
            return File.Open(Destination.Path + @"\" + item.sName, FileMode.Create, FileAccess.Write, FileShare.None);
        }
    }

    
}
