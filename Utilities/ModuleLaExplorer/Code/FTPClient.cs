using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//MY
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LaExplorer.Code
{
    public enum FTPStyle
    {
        WINDOWS = 0,
        UNIX = 1
    } 

    public class FTPClient : IAccessor
    {
        private Protocols _protocol = Protocols.FTP;
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
        private List<PredefinedImage> _predefined_images = new List<PredefinedImage>();
        private List<PredefinedImage> Predefined_images
        {
            get { return _predefined_images; }
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

            FTPList ftplist = new FTPList();
            ftplist = ftplist.Load();

            ObservableCollection<Item> items = new ObservableCollection<Item>
                                            (from host in ftplist.ftphosts
                                             select new Item()
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
                                             });
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
            if (diritem.Type == FileTypes.FOLDER || diritem.Type == FileTypes.FTPLINK)
            {
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
            }
            return result;
        }

        private StreamReader GetFTPStream(bool IsPassive, string url, ICredentials login_info)
        {
            FtpWebRequest reqFTP;
            if (url.IndexOf("ftp://") != 0)
                url = "ftp://" + url;
            reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(url));
            reqFTP.UseBinary = true;
            reqFTP.UsePassive = IsPassive;// (bool)diritem.Parent.PassiveMode;
            reqFTP.Credentials = login_info;// new NetworkCredential(diritem.Parent.User, diritem.Parent.Password);
            reqFTP.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            reqFTP.Proxy = null;
            WebResponse response = reqFTP.GetResponse();
            return new StreamReader(response.GetResponseStream());
        }

        public ObservableCollection<Item> GetFTPFileList(Item diritem)
        {
            if (File.Exists("ftp_errors.txt"))
                File.Create("ftp_errors.txt").Close();
            string host = diritem.Parent.Host.IndexOf("ftp://") != 1 ? "ftp://" + diritem.Parent.Host + "//%2f" : diritem.Parent.Host + "//%2f";
            string dir = host + diritem.sFullName;
            StringBuilder result = new StringBuilder();

            ObservableCollection<Item> fresult = new ObservableCollection<Item>();
            try
            {
                StreamReader reader = GetFTPStream((bool)diritem.Parent.PassiveMode, dir, new NetworkCredential(diritem.Parent.User, diritem.Parent.Password));
                string line = reader.ReadLine();

                while (line != null)
                {
                    result.Append(line);
                    result.Append("\n");
                    Item fresult_line;
                    if (GetFTPStyle(line) == FTPStyle.UNIX)
                    {
                        fresult_line = ParseUnixFTPStyleString(line);
                    }
                    else
                        fresult_line = ParseWindowsFTPStyleString(line);
                    if (fresult_line != null)
                        fresult.Add(fresult_line);
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
                //response.Close();
                return new ObservableCollection<Item>(fresult.OrderBy(item => item.Type));
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
                return new ObservableCollection<Item>();
            }
        }

        private Item ParseUnixFTPStyleString(string line)
        {
            //b Block special file.
            //c Character special file.
            //d Directory.
            //l Symbolic link.
            //s Socket link.
            //p FIFO.
            //- Regular file.

            Item fresult_line = null;

            Regex regex3 = new Regex(@"^(?<dir>[\-ld])(?<permission>([\-r][\-w][\-xs]){3})\+?\s+(?<filecode>\d+)\s+(?<owner>(\w+))\s+(?<group>(\w+))\s+(?<size>\d+)\s+(?<timestamp>\w+\s+\d+\s+\d{2}.?\d{2})\s+(?<name>.+)",
                               RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            Regex regex_MMM_yyyy = new Regex(@"(?<month>\w{3})\s+(?<day>\d{1,2})\s+(?<year>\d{4})");
            Regex regex_MMM_dd_hhmi = new Regex(@"(?<month>\w{3})\s+(?<day>\d{1,2})\s+(?<hour>\d{1,2}):(?<minute>\d{1,2})");
            if (line != null && (regex3.IsMatch(line)))
            {
                fresult_line = new Item();
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
            }
            return fresult_line;
        }

        private Item ParseWindowsFTPStyleString(string line)
        {
            //Windows IIS FTP server
            //07-29-13  10:40PM       <DIR>          Apps
            //05-25-13  03:02AM       <DIR>          aspnet_client
            //07-28-13  04:33PM       <DIR>          Books
            //03-20-13  08:10PM       <DIR>          Education
            //07-27-13  11:14PM       <DIR>          Films
            //03-14-12  11:45PM       <DIR>          Games
            //02-20-12  10:32PM       <DIR>          Iphone
            //07-15-13  10:35AM       <DIR>          Music
            //03-20-13  08:06PM       <DIR>          Sport
            //07-22-13  01:59PM       <DIR>          TV Shows
            //07-30-13  10:34PM       <DIR>          _torrents
            Item fresult_line = new Item();

            // The segments is like "12-13-10",  "", "12:41PM", "", "","", "", 
            // "", "", "<DIR>", "", "", "", "", "", "", "", "", "", "Folder", "A". 
            string[] segments = line.Split(' ');

            int index = 0;

            // The date segment is like "12-13-10" instead of "12-13-2010" if Four-digit years 
            // is not checked in IIS. 
            string dateSegment = segments[index];
            string[] dateSegments = dateSegment.Split(new char[] { '-' },
                StringSplitOptions.RemoveEmptyEntries);

            int month = int.Parse(dateSegments[0]);
            int day = int.Parse(dateSegments[1]);
            int year = int.Parse(dateSegments[2]);

            // If year >=50 and year <100, then  it means the year 19** 
            if (year >= 50 && year < 100)
            {
                year += 1900;
            }

            // If year <50, then it means the year 20** 
            else if (year < 50)
            {
                year += 2000;
            }

            // Skip the empty segments. 
            while (segments[++index] == string.Empty) { }

            // The time segment. 
            string timesegment = segments[index];

            fresult_line.dDate = DateTime.Parse(string.Format("{0}-{1}-{2} {3}",
                year, month, day, timesegment));

            // Skip the empty segments. 
            while (segments[++index] == string.Empty) { }

            // The size or directory segment. 
            // If this segment is "<DIR>", then it means a directory, else it means the 
            // file size. 
            string sizeOrDirSegment = segments[index];
            fresult_line.Type = sizeOrDirSegment.Equals("<DIR>",StringComparison.OrdinalIgnoreCase) ? FileTypes.FOLDER : FileTypes.FILE;

            // If this fileSystem is a file, then the size is larger than 0.  
            if (fresult_line.Type != FileTypes.FOLDER)
            {
                fresult_line.lSize = long.Parse(sizeOrDirSegment);
            }

            // Skip the empty segments. 
            while (segments[++index] == string.Empty) { }

            // Calculate the index of the file name part in the original string. 
            int filenameIndex = 0;

            for (int i = 0; i < index; i++)
            {
                // "" represents ' ' in the original string. 
                if (segments[i] == string.Empty)
                {
                    filenameIndex += 1;
                }
                else
                {
                    filenameIndex += segments[i].Length + 1;
                }
            }
            // The file name may include many segments because the name can contain ' '.           
            fresult_line.sName = line.Substring(filenameIndex).Trim();

            return fresult_line;
        } 

        private static FTPStyle GetFTPStyle(string recordString)
        {
            Regex regex = new System.Text.RegularExpressions.Regex(@"^[d-]([r-][w-][x-]){3}$");
            string header = recordString.Substring(0, 10);
            // If the style is UNIX, then the header is like "drwxrwxrwx". 
            if (regex.IsMatch(header))
            {
                return FTPStyle.UNIX;
            }
            else
            {
                return FTPStyle.WINDOWS;
            }
        }

        public void DefineIcons(IEnumerable<Item> items, TaskScheduler scheduler)
        {
        }

        public Stream GetReadStream(Item item)
        {
            return null;
        }

        public Stream GetWriteStream(ParentItem Destination, Item item)
        {
            return null;
        }

        public string GetItemsStatus()
        { return ""; }
    }
}
