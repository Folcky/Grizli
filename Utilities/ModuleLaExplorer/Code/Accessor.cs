using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Win32IconExtractor.Win32;
using LaExplorer.Views;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;

namespace LaExplorer.Code
{
    public interface IAccessor
    {
        Protocols Protocol();
        Source GetItems(Item diritem);
        void DefineIcons(IEnumerable<Item> item, TaskScheduler scheduler);
        string GetItemsStatus();
        Source InitItems();
        Stream GetReadStream(Item item);
        Stream GetWriteStream(ParentItem destination, Item item);
    }

    public abstract class AccessorFactory
    {
        public static IAccessor GetAccessor(ObservableCollection<IAccessor> accessors, Protocols protocol)
        {
            return (from ac in accessors
                   where ac.Protocol() == protocol
                   select ac).FirstOrDefault();
        }
    }

    public class Accessor
    {
        public Accessor()
        {
            try
            {
                this.User = System.Security.Principal.WindowsIdentity.GetCurrent();
            }
            catch { }

            try
            {
                ResourceDictionary resourceDictionary = new ResourceDictionary();
                resourceDictionary.Source = new Uri(
                "ModuleLaExplorer;component/Resources/Dictionary.xaml", UriKind.Relative);
                this.Predefined_images.Add(new PredefinedImage() { Image_name = "FolderIcon", Image = (BitmapImage)resourceDictionary["FolderIcon"] });
                this.Predefined_images.Add(new PredefinedImage() { Image_name = "LockedFolderIcon", Image = (BitmapImage)resourceDictionary["LockedFolderIcon"] });
                this.Predefined_images.Add(new PredefinedImage() { Image_name = "UnknownIcon", Image = (BitmapImage)resourceDictionary["UnknownIcon"] });
                this.Predefined_images.Add(new PredefinedImage() { Image_name = "UpFolderIcon", Image = (BitmapImage)resourceDictionary["UpFolderIcon"] });
                this.Predefined_images.Add(new PredefinedImage() { Image_name = "FtpLinkIcon", Image = (BitmapImage)resourceDictionary["FtpLinkIcon"] });

                this.Predefined_images.Add(new PredefinedImage() { Image_name = "FixedDrive", Image = (BitmapImage)resourceDictionary["FixedDrive"] });
                this.Predefined_images.Add(new PredefinedImage() { Image_name = "RemovableDrive", Image = (BitmapImage)resourceDictionary["RemovableDrive"] });
                this.Predefined_images.Add(new PredefinedImage() { Image_name = "CDRomDrive", Image = (BitmapImage)resourceDictionary["CDRomDrive"] });
                this.Predefined_images.Add(new PredefinedImage() { Image_name = "UnknownDrive", Image = (BitmapImage)resourceDictionary["UnknownDrive"] });
                this.Predefined_images.Add(new PredefinedImage() { Image_name = "NetworkDrive", Image = (BitmapImage)resourceDictionary["NetworkDrive"] });
                this.Predefined_images.Add(new PredefinedImage() { Image_name = "NetDrive", Image = (BitmapImage)resourceDictionary["NoRootDirectoryDrive"] });
                this.Predefined_images.Add(new PredefinedImage() { Image_name = "FtpDrive", Image = (BitmapImage)resourceDictionary["FtpDrive"] });
            }
            catch { }

        }

        private List<PredefinedImage> _predefined_images = new List<PredefinedImage>();
        private List<PredefinedImage> Predefined_images
        {
            get { return _predefined_images; }
            set 
            {
                if (value != null && value.GetType() == typeof(List<PredefinedImage>))
                    _predefined_images = value;
            }
        }
        public BitmapImage GetPredefinedImage(string name)
        {
            return (from l in Predefined_images
                    where l.Image_name == name
                    select l.Image).FirstOrDefault();
        }

        public int MMM2MM(string MMM)
        {
            switch (MMM.ToLower())
            {
                default:
                    return 1;
                case ("jan"):
                    return 1;
                case ("feb"):
                    return 2;
                case ("mar"):
                    return 3;
                case ("apr"):
                    return 4;
                case ("may"):
                    return 5;
                case ("jun"):
                    return 6;
                case ("jul"):
                    return 7;
                case ("aug"):
                    return 8;
                case ("sep"):
                    return 9;
                case ("oct"):
                    return 10;
                case ("nov"):
                    return 11;
                case ("dec"):
                    return 12;
            }
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

        public BitmapImage GetIcon(FileInfo _file)
        {
            //TODO оптимизировать
            try
            {
                string temp_file = System.IO.Path.GetTempPath() + _file.Name;
                //Нельзя запускать в несколько потоков на Write protected дисках
                if (_file.Exists == false)
                {
                    Icon result=null;
                    
                    if (_file.Extension == "")
                    {
                        File.Create(temp_file).Close();
                        if (File.Exists(temp_file))
                        {
                            result = Icon.ExtractAssociatedIcon(temp_file);
                            File.Delete(temp_file);
                            return result.ToBitmap().ToBitmapImage();
                        }
                    }
                    else
                    {
                        result=Win32IconExtractor.Win32.Icons.IconFromExtension(_file.Extension, Win32IconExtractor.Win32.Icons.SystemIconSize.Large);
                        if (result == null)
                        {
                            File.Create(temp_file).Close();
                            if (File.Exists(temp_file))
                            {
                                result = Icon.ExtractAssociatedIcon(temp_file);

                                File.Delete(temp_file);
                                return result.ToBitmap().ToBitmapImage();
                            }
                        }
                        return result.ToBitmap().ToBitmapImage();
                    }
                }
                Uri uriAddress2 = new Uri(_file.FullName);
                if (!uriAddress2.IsUnc)
                    return Icon.ExtractAssociatedIcon(_file.FullName).ToBitmap().ToBitmapImage();
                else
                    return GetIconOldSchool(_file.FullName).ToBitmap().ToBitmapImage();
            }
            catch//(Exception e) 
            {
                try 
                {
                    if (_file.Exists == true)
                    {
                        return GetIconOldSchool(_file.FullName).ToBitmap().ToBitmapImage();
                    }
                }
                catch { return null; }
                return null;
            }
        }

        public BitmapImage GetIcon(ref List<Ext_Icon> icon_source, FileInfo _file)
        {
            if (_file.Extension.ToLower() == ".exe" || _file.Extension.ToLower() == ".lnk" || _file.Extension.ToLower() == ".bmp" || _file.Extension.ToLower() == ".ico")
                //Дорогая операция
                return Icon.ExtractAssociatedIcon(_file.FullName).ToBitmap().ToBitmapImage();
            BitmapImage result = (from k in icon_source
                                  where k.sExt == _file.Extension
                                  select k.iImage).FirstOrDefault();
            if (result == null)
            {
                //Дорогая операция
                result = Icon.ExtractAssociatedIcon(_file.FullName).ToBitmap().ToBitmapImage();
                icon_source.Add(new Ext_Icon
                {
                    sExt = _file.Extension.ToLower(),
                    iImage = result
                });
            }
            return result; 
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

        public Item GetItem(string fullpath)
        {
            List<Ext_Icon> _icons = new List<Ext_Icon>();
            DirectoryInfo dir = new DirectoryInfo(fullpath);
            FileInfo file = new FileInfo(fullpath);
            if (dir.Exists)
                return new Item
                {
                    sName = dir.Name,
                    sFullName = dir.FullName,
                    Type = FileTypes.FOLDER,
                    dDate = dir.LastWriteTime,
                    iImage = CheckListAccess(this.User, this.Principal, dir) ? GetPredefinedImage("FolderIcon") : GetPredefinedImage("LockedFolderIcon"),
                    Parent = new ParentItem { Protocol = Protocols.LOCAL },
                    bPBVisible = Visibility.Hidden,
                    RHeight = 0
                };
            if (file.Exists)
                return new Item
                {
                    sName = file.Name,
                    sFullName = file.FullName,
                    Type = FileTypes.FILE,
                    sExt = file.Extension,
                    dDate = file.LastWriteTime,
                    lSize = file.Length,
                    Parent = new ParentItem { Protocol = Protocols.LOCAL },
                    iImage = GetIcon(ref _icons, file),
                    bPBVisible = Visibility.Hidden,
                    RHeight = 0
                };
            else
                return null;
        }

        public bool DeleteFtpFile(Item item2delete)
        {
            try
            {
                string item2delete_name = item2delete.Parent.Host + "//%2f" + item2delete.sFullName;
                FtpWebRequest reqFTP;
                if (item2delete_name.IndexOf("ftp://") != 1)
                    item2delete_name = "ftp://" + item2delete_name;
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(item2delete_name));
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(item2delete.Parent.User, item2delete.Parent.Password);
                reqFTP.Proxy = null;
                reqFTP.UsePassive = (bool)item2delete.Parent.PassiveMode;
                reqFTP.Method = WebRequestMethods.Ftp.DeleteFile;
                WebResponse response = reqFTP.GetResponse();
                return true;
            }
            catch
            { return false; }
        }

        public void DeleteRecursiveFolder(string item2delete)
        {
            foreach (string Folder in Directory.GetDirectories(item2delete))
            {
                this.DeleteRecursiveFolder(Folder);
            }

            foreach (string file in Directory.GetFiles(item2delete))
            {
                FileInfo fi = new FileInfo(file);
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            Directory.Delete(item2delete);
        }

        public ObservableCollection<Item> GetFTPFileList(Item diritem)
        {
            if (File.Exists("ftp_errors.txt"))
                File.Create("ftp_errors.txt").Close();
            string host = diritem.Parent.Host.IndexOf("ftp://") != 1 ? "ftp://" + diritem.Parent.Host + "//%2f" : diritem.Parent.Host + "//%2f";
            string dir = host+diritem.sFullName;
            StringBuilder result = new StringBuilder();
            FtpWebRequest reqFTP;
            ObservableCollection<Item> fresult= new ObservableCollection<Item>();
            try
            {
                //reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri("ftp://" + "ftp.corbina.ru" + "/" + dir));
                if (dir.IndexOf("ftp://") != 0)
                    dir = "ftp://" + dir;
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(dir));
                reqFTP.UseBinary = true;
                reqFTP.UsePassive = (bool)diritem.Parent.PassiveMode;
                reqFTP.Credentials = new NetworkCredential(diritem.Parent.User, diritem.Parent.Password);
                reqFTP.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                reqFTP.Proxy = null;
                WebResponse response = reqFTP.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());
                Regex regex3 = new Regex(@"^(?<dir>[\-ld])(?<permission>([\-r][\-w][\-xs]){3})\+?\s+(?<filecode>\d+)\s+(?<owner>(\w+))\s+(?<group>(\w+))\s+(?<size>\d+)\s+(?<timestamp>\w+\s+\d+\s+\d{2}.?\d{2})\s+(?<name>.+)",
                                         //@"^(?<dir>[\-ld])(?<permission>([\-r][\-w][\-xs]){3})\s+(?<filecode>\d+)\s+(?<owner>\w+)\s+(?<group>\w+)\s+(?<size>\d+)\s+(?<timestamp>((?<month>\w{3})\s+(?<day>\d{2})\s+(?<hour>\d{1,2}):(?<minute>\d{2}))|((?<month>\w{3})\s+(?<day>\d{2})\s+(?<year>\d{4})))\s+(?<name>.+)$",
                                    RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
                Regex regex_MMM_yyyy = new Regex(@"(?<month>\w{3})\s+(?<day>\d{1,2})\s+(?<year>\d{4})");
                Regex regex_MMM_dd_hhmi = new Regex(@"(?<month>\w{3})\s+(?<day>\d{1,2})\s+(?<hour>\d{1,2}):(?<minute>\d{1,2})");
                string line = reader.ReadLine();

                 //b Block special file.
                 //c Character special file.
                 //d Directory.
                 //l Symbolic link.
                 //s Socket link.
                 //p FIFO.
                 //- Regular file.

                while (line != null)
                {
                    Item fresult_line = new Item();
                    result.Append(line);
                    result.Append("\n");
                    if (line != null && (
                        regex3.IsMatch(line)
                        ))
                    {
                        if (line.Contains("s34964_su-130212-091701.log"))
                        {
                            int i = 1;
                        }
                        if (line.Contains("s34964_su-121227-080001.log"))
                        {
                            int i = 1;
                        }
                        
                        if (regex3.Split(line)[regex3.GroupNumberFromName("dir")] == "d")
                        {
                            fresult_line.Type = FileTypes.FOLDER;
                        }
                        else if (regex3.Split(line)[regex3.GroupNumberFromName("dir")] == "l")
                        {
                            fresult_line.Type = FileTypes.FTPLINK;
                        }
                        else
                            fresult_line.Type = FileTypes.FILE;
                        fresult_line.sName = regex3.Split(line)[regex3.GroupNumberFromName("name")];
                        try
                        {
                            fresult_line.lSize = Convert.ToInt64(regex3.Split(line)[regex3.GroupNumberFromName("size")]);
                        }
                        catch
                        {
                            fresult_line.lSize = 0;
                        }

                        string timestamp = regex3.Split(line)[regex3.GroupNumberFromName("timestamp")];
                        //Define Date
                        if (regex_MMM_yyyy.IsMatch(timestamp))
                        {
                            int year = Convert.ToInt16(regex_MMM_yyyy.Split(timestamp)[regex_MMM_yyyy.GroupNumberFromName("year")].Trim());
                            int month = MMM2MM(regex_MMM_yyyy.Split(timestamp)[regex_MMM_yyyy.GroupNumberFromName("month")].Trim());
                            int day = Convert.ToInt16(regex_MMM_yyyy.Split(timestamp)[regex_MMM_yyyy.GroupNumberFromName("day")].Trim());
                            fresult_line.dDate = new DateTime(year, 01, day);
                        }

                        if (regex_MMM_dd_hhmi.IsMatch(timestamp))
                        {
                            int month = MMM2MM(regex_MMM_dd_hhmi.Split(timestamp)[regex_MMM_dd_hhmi.GroupNumberFromName("month")].Trim());
                            int year = month > DateTime.Now.Month ? DateTime.Now.Year - 1 : DateTime.Now.Year;
                            int day = Convert.ToInt16(regex_MMM_dd_hhmi.Split(timestamp)[regex_MMM_dd_hhmi.GroupNumberFromName("day")].Trim());
                            int hour = Convert.ToInt16(regex_MMM_dd_hhmi.Split(timestamp)[regex_MMM_dd_hhmi.GroupNumberFromName("hour")].Trim());
                            int minute = Convert.ToInt16(regex_MMM_dd_hhmi.Split(timestamp)[regex_MMM_dd_hhmi.GroupNumberFromName("minute")].Trim());
                            fresult_line.dDate = new DateTime(year, month, day, hour, minute, 0);
                        }

                        fresult.Add(fresult_line);
                    }
                    else
                    {
                        if (line != null)
                        {
                            FileInfo s = new FileInfo("ftp_errors.txt");
                            StreamWriter f = s.AppendText();
                            f.WriteLine(line);
                            f.Close();
                        }
                    }
                    line = reader.ReadLine();
                }
                reader.Close();
                response.Close();
                return new ObservableCollection<Item>(fresult.OrderBy(item => item.Type));
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
                return new ObservableCollection<Item>();
            }
        }

        public Source GetItems(Item diritem)
        {
            Source result = new Source();
            ParentItem parentitem = new ParentItem();
            int[] i = new int[] { 1 };
            if (diritem.sFullName != null && diritem.sFullName != "")
            {
                switch (diritem.Parent.Protocol)
                {
                    case Protocols.FTP:
                        ObservableCollection<Item> filenames = GetFTPFileList(diritem);
                        parentitem.User = diritem.Parent.User;
                        parentitem.Password = diritem.Parent.Password;
                        parentitem.Protocol = Protocols.FTP;
                        parentitem.ConnectionName = diritem.Parent.ConnectionName;
                        parentitem.PassiveMode = diritem.Parent.PassiveMode;
                        parentitem.Path = diritem.sFullName;
                        parentitem.Host = diritem.Parent.Host;
                        ObservableCollection<Item> ftpitems = new ObservableCollection<Item>(
                            (from l in i
                             select new Item
                             {
                                 sName = ". . .",
                                 sFullName = diritem.sFullName.LastIndexOf("/") == -1 ? "" : diritem.sFullName.Substring(0, diritem.sFullName.LastIndexOf("/")),
                                 iImage = GetPredefinedImage("UpFolderIcon"),
                                 Type = FileTypes.FOLDER,
                                 Parent = parentitem,
                                 bPBVisible = Visibility.Hidden,
                                 RHeight = 0
                             }).Union(from l in filenames
                                      select new Item
                                                {
                                                    sName = l.sName,
                                                    sFullName = diritem.sFullName + "/" + (l.Type == FileTypes.FOLDER || l.Type == FileTypes.FILE ? l.sName : l.sName.Substring(0, l.sName.IndexOf("->") - 1)),
                                                    Type = l.Type == FileTypes.FOLDER ? FileTypes.FOLDER : l.Type == FileTypes.FTPLINK ? FileTypes.FTPLINK : FileTypes.FILE,
                                                    lSize = l.lSize,
                                                    iImage = l.Type == FileTypes.FOLDER ? GetPredefinedImage("FolderIcon") : l.Type == FileTypes.FTPLINK ? GetPredefinedImage("FtpLinkIcon") : null,
                                                    Parent = parentitem,
                                                    dDate = l.dDate,
                                                    bPBVisible = Visibility.Hidden,
                                                    RHeight = 0
                                                }));
                        result.iSource = ftpitems;
                        result.Connection_description = parentitem;
                        return result;
                    case Protocols.LOCAL:
                        List<Ext_Icon> _licons = new List<Ext_Icon>();
                        parentitem.Protocol = Protocols.LOCAL;
                        parentitem.Path = diritem.sFullName;
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
                                                           //iImage = CheckListAccess(currentUser, currentPrincipal, l) ? _foldericon : _lockedfoldericon, 
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
                                                           //iImage = GetIcon(ref _icons, l),
                                                           bPBVisible = Visibility.Hidden,
                                                           RHeight = 0
                                                       }
                                                       ));
                            //result.Connection_description.sPath = diritem.sFullName;
                            result.iSource = items;
                            result.Connection_description = parentitem;
                        }
                        return result;
                    case Protocols.NETWORK:
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
                        return result;
                    default:
                        return null;
                }
            }
            else
            {
                //ObservableCollection<DriveInfo> drives = DriveInfo.GetDrives();
                ObservableCollection<Item> items = new ObservableCollection<Item>
                (from l in DriveInfo.GetDrives()
                                          select new Item
                                          {
                                              sName = l.Name,
                                              sFullName = l.Name,
                                              Type = FileTypes.FOLDER,
                                              Parent = new ParentItem { Protocol = Protocols.LOCAL },
                                              sExt = l.DriveType.ToString(),
                                              iImage = GetPredefinedImage(l.DriveType.ToString()+"Drive"),
                                              lSize = l.DriveType.ToString() == "Fixed" || l.DriveType.ToString() == "Removable" ? l.TotalSize : 0,
                                              dDate = l.DriveType.ToString() == "Fixed" ? l.RootDirectory.LastWriteTime : DateTime.Today,
                                              bPBVisible = Visibility.Hidden,
                                              RHeight = 0
                                          });

                FTPList ftplist = new FTPList();
                ftplist = ftplist.Load();
                foreach (FTPHost host in ftplist.ftphosts)
                {
                    Item ftp = new Item
                    {
                        sName = host._sname,
                        sFullName = host._cwd,
                        Type = FileTypes.FOLDER,
                        Parent = new ParentItem
                        {
                            Protocol = Protocols.FTP,
                            User = host._suser == null ? "" : host._suser,
                            Password = host._spassword == null ? "" : host._spassword,
                            PassiveMode = host._passivemode,
                            Path = host._cwd,
                            Host = host._shost,
                            ConnectionName = host._sname
                        },
                        iImage = GetPredefinedImage("FtpDrive"),
                        bPBVisible = Visibility.Hidden,
                        RHeight = 0
                    };
                    items.Add(ftp);
                }
                
                NetList netlist = new NetList();
                netlist = netlist.Load();
                foreach (NetHost host in netlist.nethosts)
                {
                    string netpath = new Uri(host._path, true).AbsolutePath.Replace("/", "\\");
                    Item net = new Item
                    {
                        sName = host._name,
                        sFullName = netpath,
                        Type = FileTypes.FOLDER,
                        Parent = new ParentItem
                        {
                            Protocol = Protocols.NETWORK,
                            Host = new Uri(host._path, true).Host,
                            Domain = host._domain == null ? "" : host._domain,
                            User = host._user == null ? "" : host._user,
                            Password = host._password == null ? "" : host._password,
                            Path = netpath,
                            ConnectionName = host._name
                        },
                        iImage = GetPredefinedImage("NetDrive"),
                        bPBVisible = Visibility.Hidden,
                        RHeight = 0
                    };
                    items.Add(net);
                }
                result.iSource = items;
                result.Connection_description = new ParentItem { Protocol = Protocols.LOCAL };
                return result;
            }
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

    }
    
    public class ImpersonateUser
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool LogonUser(
        String lpszUsername,
        String lpszDomain,
        String lpszPassword,
        int dwLogonType,
        int dwLogonProvider,
        ref IntPtr phToken);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public extern static bool CloseHandle(IntPtr handle);
        private static IntPtr tokenHandle = new IntPtr(0);
        private static WindowsImpersonationContext impersonatedUser;
        // If you incorporate this code into a DLL, be sure to demand that it
        // runs with FullTrust.
        [PermissionSetAttribute(SecurityAction.Demand, Name = "FullTrust")]
        public WindowsIdentity Impersonate(string domainName, string userName, string password)
        {
            //try
            {
                tokenHandle = IntPtr.Zero;
                // ---- Step - 1
                // Call LogonUser to obtain a handle to an access token.
                bool returnValue = LogonUser(
                userName,
                domainName,
                password,
                (int)LogonType.LOGON32_LOGON_NEW_CREDENTIALS,
                (int)LogonProvider.LOGON32_PROVIDER_DEFAULT,
                ref tokenHandle); // tokenHandle - new security token
                if (false == returnValue)
                {
                    int ret = Marshal.GetLastWin32Error();                    
                    throw new System.ComponentModel.Win32Exception(ret);
                }
                // ---- Step - 2
                WindowsIdentity newId = new WindowsIdentity(tokenHandle);
                // ---- Step - 3
                impersonatedUser = newId.Impersonate();
                return newId;
            }
        }
        // Stops impersonation
        public void Undo()
        {
            impersonatedUser.Undo();
            // Free the tokens.
            if (tokenHandle != IntPtr.Zero)
                CloseHandle(tokenHandle);
        }
    }
}


