using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//MY
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;
using System.Security.AccessControl;
using Win32IconExtractor.Win32;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Threading.Tasks;

namespace LaExplorer.Code
{
    class NetworkClient : IAccessor
    {
        private Protocols _protocol = Protocols.NETWORK;
        public Protocols Protocol()
        {
            return _protocol;
        }

        private Source _currentItems = new Source();
        public Source CurrentItems
        {
            get { return _currentItems; }
            set { _currentItems = value; }
        }

        private List<Ext_Icon> _knowedicons = new List<Ext_Icon>();
        public List<Ext_Icon> KnowedIcons
        {
            get { return _knowedicons; }
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

        public Source InitItems()
        {
            Source result = new Source();
            ParentItem parentitem = new ParentItem();
            parentitem.Protocol = Protocol();
            int[] i = new int[] { 1 };

            NetList netlist = new NetList();
            netlist = netlist.Load();
            ObservableCollection<Item> items = new ObservableCollection<Item>
                                            (from host in netlist.nethosts
                                             select new Item()
                                             {
                                                 sName = host._name,
                                                 sFullName = new Uri(host._path, true).AbsolutePath.Replace("/", "\\"),
                                                 Type = FileTypes.FOLDER,
                                                 sExt = "network",
                                                 Parent = new ParentItem
                                                 {
                                                     Protocol = Protocol(),
                                                     Host = new Uri(host._path, true).Host,
                                                     Domain = host._domain == null ? "" : host._domain,
                                                     User = host._user == null ? "" : host._user,
                                                     Password = host._password == null ? "" : host._password,
                                                     Path = new Uri(host._path, true).AbsolutePath.Replace("/", "\\"),
                                                     ConnectionName = host._name
                                                 },
                                                 iImage = GetPredefinedImage("NetworkDrive"),
                                                 bPBVisible = Visibility.Hidden,
                                                 RHeight = 0
                                             }
                                            );
            result.iSource = items;
            result.Connection_description = new ParentItem { Protocol = Protocols.LOCAL };
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
                using (Impersonation accesschecker = new Impersonation(diritem.Parent.Domain, diritem.Parent.User, diritem.Parent.Password))
                {
                    DirectoryInfo ndir = new DirectoryInfo(@"\\" + diritem.Parent.Host + @"\" + diritem.sFullName);
                    if (ndir.Exists)
                    {
                        List<Ext_Icon> _nicons = new List<Ext_Icon>();
                        parentitem.Protocol = Protocols.NETWORK;
                        parentitem.Host = diritem.Parent.Host;
                        parentitem.Path = diritem.sFullName;
                        parentitem.User = diritem.Parent.User;
                        parentitem.Domain = diritem.Parent.Domain;
                        parentitem.Password = diritem.Parent.Password;
                        parentitem.ConnectionName = diritem.Parent.ConnectionName;
                        ObservableCollection<Item> netitems = new ObservableCollection<Item>((from l in i
                                                                                              select new Item
                                                                                              {
                                                                                                  sName = ". . .",
                                                                                                  sFullName = ndir.Parent == null ? "" : new Uri(ndir.Parent.FullName, true).AbsolutePath.Replace("/", "\\"),
                                                                                                  iImage = GetPredefinedImage("UpFolderIcon"),
                                                                                                  Type = FileTypes.FOLDER,
                                                                                                  Parent = parentitem,
                                                                                                  bPBVisible = Visibility.Hidden,
                                                                                                  RHeight = 0
                                                                                              }).Union(
                                                   from l in ndir.GetDirectories()
                                                   select new Item
                                                   {
                                                       sName = l.Name,
                                                       sFullName = new Uri(l.FullName, true).AbsolutePath.Replace("/", "\\"),
                                                       Type = FileTypes.FOLDER,
                                                       dDate = l.LastWriteTime,
                                                       //iImage = CheckListAccess(currentUser, currentPrincipal, l) ? _foldericon : _lockedfoldericon, 
                                                       iImage = GetPredefinedImage("FolderIcon"),
                                                       Parent = parentitem,
                                                       bPBVisible = Visibility.Hidden,
                                                       RHeight = 0
                                                   }).Union(
                                                   from l in ndir.GetFiles()
                                                   select new Item
                                                   {
                                                       sName = l.Name,
                                                       sFullName = new Uri(l.FullName, true).AbsolutePath.Replace("/", "\\"),
                                                       Type = FileTypes.FILE,
                                                       sExt = l.Extension,
                                                       dDate = l.LastWriteTime,
                                                       lSize = l.Length,
                                                       Parent = parentitem,
                                                       //iImage = GetIcon(ref _icons, l),
                                                       bPBVisible = Visibility.Hidden,
                                                       RHeight = 0
                                                   }
                                                   ));
                        //result.Connection_description.sPath = diritem.sFullName;
                        result.iSource = netitems;
                        result.Connection_description = parentitem;
                    }
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

        public void DefineIcons(IEnumerable<Item> items, TaskScheduler scheduler)
        {
            foreach (var impersonator in items.Select(item => new {Domain=item.Parent.Domain, User=item.Parent.User, Password=item.Parent.Password}).Distinct())
            {
                using (Impersonation accesschecker = new Impersonation(impersonator.Domain, impersonator.User, impersonator.Password))
                {
                    foreach (Item item in items.Where(w => w.iImage==null && w.Parent.Domain == impersonator.Domain &&  w.Parent.User == impersonator.User && w.Parent.Password==impersonator.Password ))
                    {
                        BitmapImage result = null;
                        switch (item.Type)
                        {
                            case FileTypes.FILE:
                                FileInfo _file = new FileInfo(@"\\" + item.Parent.Host + @"\" + item.sFullName);
                                if (_file.Extension.ToLower() == ".exe" || _file.Extension.ToLower() == ".lnk" || _file.Extension.ToLower() == ".bmp" || _file.Extension.ToLower() == ".ico")
                                    //Дорогая операция
                                    result = GetIconOldSchool(_file.FullName).ToBitmap().ToBitmapImage();
                                result = (from k in KnowedIcons
                                          where k.sExt.Trim().ToLower() == _file.Extension.Trim().ToLower()
                                          select k.iImage).FirstOrDefault();
                                if (result == null)
                                {
                                    Task<BitmapImage> getBmp = Task<BitmapImage>.Factory.StartNew(() =>
                                        {
                                            BitmapImage image = GetIconOldSchool(_file.FullName).ToBitmap().ToBitmapImage();
                                            //Freeze image to ensure that it is accessible from any thread;
                                            image.Freeze();
                                            return image;
                                        });

                                    getBmp.ContinueWith((p) =>
                                    {
                                        result = p.Result;
                                        if (KnowedIcons.Where(w => w.sExt == _file.Extension.ToLower()).FirstOrDefault() == null)
                                            KnowedIcons.Add(new Ext_Icon
                                            {
                                                sExt = _file.Extension.ToLower(),
                                                iImage = result
                                            });
                                        if (item.iImage == null)
                                            item.iImage = result;
                                    }, scheduler);
                                    if (getBmp.Exception != null)
                                        MessageBox.Show(getBmp.Exception.Message);
                                }
                                else
                                    item.iImage = result;
                                break;
                            default:
                                result = null;
                                break;
                        }
                    }
                }
            }

        }

        public Stream GetReadStream(Item item)
        {
            return File.Open(@"\\" + item.Parent.Host + @"\" + item.sFullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public Stream GetWriteStream(ParentItem Destination, Item item)
        {
            return File.Open(@"\\" + item.Parent.Host + @"\" + item.sFullName, FileMode.Create);
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
            //switch (item_to_start.Parent.Protocol)
            //{
            //    case Protocols.LOCAL:
            //        try
            //        {
            //            FileInfo fileinfo_was = new FileInfo(item_to_start.sFullName);
            //            FileCreateAndModifiedTime file_was;
            //            file_was.CreationTime = fileinfo_was.CreationTime;
            //            file_was.LastWriteTime = fileinfo_was.LastWriteTime;
            //            Process process_editor = Process.Start(item_to_start.sFullName);
            //            if (process_editor != null)
            //            {
            //                process_editor.EnableRaisingEvents = true;
            //                process_editor.Exited += delegate { process_editor_Exited(item_to_start, file_was); };
            //            }
            //        }
            //        catch (Exception e)
            //        { MessageBox.Show(e.Message); }
            //        break;
            //    case Protocols.FTP:
            //        FileOperationHelper whattodo = new FileOperationHelper();
            //        var items = new ObservableCollection<Item>();
            //        items.Add(item_to_start);
            //        whattodo.FileList = items;
            //        whattodo.Destination = new ParentItem() { Path = System.IO.Path.GetTempPath(), Protocol = Protocols.LOCAL };
            //        BackgroundWorker copyworker = new BackgroundWorker();
            //        copyworker.WorkerReportsProgress = true;
            //        copyworker.DoWork += new DoWorkEventHandler(Copy_DoWork);
            //        copyworker.ProgressChanged += new ProgressChangedEventHandler(Copy_ReportProgress);
            //        copyworker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Copy4FtpEdit_Completed);
            //        copyworker.RunWorkerAsync(whattodo);
            //        break;
            //}
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public class Impersonation : IDisposable
        {
            private readonly SafeTokenHandle _handle;
            private readonly WindowsImpersonationContext _context;

            const int LOGON32_LOGON_NEW_CREDENTIALS = 9;

            public Impersonation(string domain, string username, string password)
            {
                var ok = LogonUser(username, domain, password,
                               LOGON32_LOGON_NEW_CREDENTIALS, 0, out this._handle);
                if (!ok)
                {
                    var errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode);
                    //throw new ApplicationException(string.Format("Could not impersonate the elevated user.  LogonUser returned error code {0}.", errorCode));
                }

                this._context = WindowsIdentity.Impersonate(this._handle.DangerousGetHandle());
            }

            public void Dispose()
            {
                this._context.Dispose();
                this._handle.Dispose();
            }

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            private static extern bool LogonUser(String lpszUsername, String lpszDomain, String lpszPassword, int dwLogonType, int dwLogonProvider, out SafeTokenHandle phToken);

            public sealed class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
            {
                private SafeTokenHandle()
                    : base(true) { }

                [DllImport("kernel32.dll")]
                [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
                [SuppressUnmanagedCodeSecurity]
                [return: MarshalAs(UnmanagedType.Bool)]
                private static extern bool CloseHandle(IntPtr handle);

                protected override bool ReleaseHandle()
                {
                    return CloseHandle(handle);
                }
            }
        }

        #region Old-School method
        [DllImport("shell32.dll")]
        static extern IntPtr ExtractAssociatedIcon(IntPtr hInst,
           StringBuilder lpIconPath, out ushort lpiIcon);

        public static Icon GetIconOldSchool(string fileName)
        {
            ushort uicon;
            StringBuilder strB = new StringBuilder(fileName);
            IntPtr handle = ExtractAssociatedIcon(IntPtr.Zero, strB, out uicon);
            Icon ico = Icon.FromHandle(handle);

            return ico;
        }
        #endregion
    }
}
