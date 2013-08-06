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
    public class NetHost
    {
        // здесь вместо XmlElement можно XmlAttribute. Формат сохранения будет другой. Проверьте.
        [XmlElement("name")]
        public string _name;
        [XmlElement("path")]
        public string _path;
        [XmlElement("domain")]
        public string _domain;
        [XmlElement("user")]
        public string _user;
        [XmlElement("password")]
        public string _password;
        [XmlElement("usecurrentuser")]
        public bool _usecurrentuser;
    }

    [XmlRoot("NetList")]
    public class NetList
    {
        private readonly string data_path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Grizli";
        private string xml_file = "";

        [XmlArray("nethosts")]
        public List<NetHost> nethosts;
        [XmlElement("id-counter")]
        public int id_counter = 1;

        public NetList()
        {
            nethosts = new List<NetHost>();
            Directory.CreateDirectory(data_path);
            xml_file = data_path + @"\net_hosts.xml";
        }

        public void Save()
        {
            XmlSerializer s = new XmlSerializer(typeof(NetList));
            TextWriter w = new StreamWriter(xml_file);
            s.Serialize(w, this);
            w.Flush();
            w.Close();
        }

        public NetList Load()
        {
            NetList netlist = new NetList();
            if (File.Exists(xml_file))
            {
                XmlSerializer s = new XmlSerializer(typeof(NetList));
                TextReader r = new StreamReader(xml_file);
                netlist = (NetList)s.Deserialize(r);
                r.Close();
            }
            return netlist;
        }
    }

    public class FTPHost
    {
        // здесь вместо XmlElement можно XmlAttribute. Формат сохранения будет другой. Проверьте.
        [XmlElement("name")]
        public string _sname;
        [XmlElement("host")]
        public string _shost;
        [XmlElement("user")]
        public string _suser;
        [XmlElement("password")]
        public string _spassword;
        [XmlElement("cwd")]
        public string _cwd;
        [XmlElement("passivemode")]
        public bool _passivemode;
    }

    [XmlRoot("FTPList")]
    public class FTPList
    {
        private readonly string data_path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Grizli";
        private string xml_file="";

        [XmlArray("ftphosts")]
        public List<FTPHost> ftphosts;
        [XmlElement("id-counter")]
        public int id_counter = 1;

        public FTPList()
        {
            ftphosts = new List<FTPHost>();
            Directory.CreateDirectory(data_path);
            xml_file = data_path + @"\ftp_hosts.xml";
        }

        public void Save()
        {
            XmlSerializer s = new XmlSerializer(typeof(FTPList));
            TextWriter w = new StreamWriter(xml_file);
            s.Serialize(w, this);
            w.Flush();
            w.Close();
        }

        public FTPList Load()
        {
            FTPList ftplist = new FTPList();
            if (File.Exists(xml_file))
            {
                XmlSerializer s = new XmlSerializer(typeof(FTPList));
                TextReader r = new StreamReader(xml_file);
                ftplist = (FTPList)s.Deserialize(r);
                r.Close();
            }
            return ftplist;
        }
    }

    public static class BitmapExtensions
    {
        public static BitmapImage ToBitmapImage(this Bitmap bitmap)
        {
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);

            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();
            return bi;
        }
    }

    public class FileOperationHelper
    {
        private string _tomask;
        private string _filter;
        private ParentItem _destination;
        public string ToMask { get { return _tomask; } set { _tomask = value; } }
        public string Filter { get { return _filter; } set { _filter = value; } }
        public ParentItem Destination { get { return _destination; } set { _destination = value; } }
        private ObservableCollection<Item> _filelist;
        public ObservableCollection<Item> FileList { get { return _filelist; } set { _filelist = value; } }
        public FileOperationHelper()
        {
            _filelist = new ObservableCollection<Item>();
        }
    }

    public class PanelsDescription
    {
        private int _current_panel_index;
        //private string _path;
        private ParentItem _connection_description;
        public int CurrentPanelIndex { get { return _current_panel_index; } set { _current_panel_index = value; } }
        //public string sPath { get { return _path; } set { _path = value; } }
        public ParentItem Connection_description { get { return _connection_description; } set { _connection_description = value; } }
        public ObservableCollection<Views.Explorer> sSources { get; set; }
        public PanelsDescription()
        {
            _current_panel_index = 1;//?
            _connection_description = new ParentItem { Path = "" };
            sSources = new ObservableCollection<Explorer>();
        }
    }

    public class Source : INotifyPropertyChanged
    {
        private ObservableCollection<Item> _isource;
        public event PropertyChangedEventHandler PropertyChanged;
        private ParentItem _connection_description;
        public ParentItem Connection_description { get { return _connection_description; } set { _connection_description = value; } }
        public ObservableCollection<Item> iSource
        {
            get { return _isource; }
            set
            {
                _isource = value;
                NotifyPropertyChanged("iSource");
            }
        }
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }

    public class Ext_Icon
    {
        public string sExt { get; set; }
        public BitmapImage iImage { get; set; }
    }

    public enum Protocols
    { FTP, LOCAL, NETWORK, UNKNOWN }

    public enum FileTypes
    { FOLDER, FILE, LINK, FTPLINK }

    public class ParentItem
    {
        private string _host = "";
        public string Host { get { return _host; } set { _host = value; } }
        private string _path = "";
        public string Path { get { return _path; } set { _path = value; } }
        private string _domain = "";
        public string Domain { get { return _domain; } set { _domain = value; } }
        private string _user = "";
        public string User { get { return _user; } set { _user = value; } }
        private string _password = "";
        public string Password { get { return _password; } set { _password = value; } }
        private Protocols _protocol = Protocols.UNKNOWN;
        public Protocols Protocol { get { return _protocol; } set { _protocol = value; } }
        private string _connectionname = "";
        public string ConnectionName { get { return _connectionname; } set { _connectionname = value; } }
        private bool? _passivemode = null;
        public bool? PassiveMode { get { return _passivemode; } set { _passivemode = value; } }

        public string GetConnectionPath()
        {
            switch (this.Protocol)
            {
                case Protocols.FTP:
                    return this.ConnectionName + ":" + (this.Path.IndexOf("/") != -1 ? this.Path.Substring(this.Path.IndexOf("/")) : "/");
                case Protocols.NETWORK:
                    return this.ConnectionName + ":" + this.Path;
                default:
                    return "";
            }
        }
    }

    public class Item : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        private int _progress;
        private int _rheight;
        private Visibility _bpbvisible;
        private string _sfullname;
        private string _sname;
        private BitmapImage _iimage;
        public ParentItem Parent;
        public string sName
        {
            get { return _sname; }
            set
            {
                _sname = value;
                NotifyPropertyChanged("sName");
            }
        }
        public string sFullName
        {
            get { return _sfullname; }
            set
            {
                _sfullname = value;
                NotifyPropertyChanged("sFullName");
            }
        }
        private FileTypes _filetypes;
        public FileTypes Type { get { return _filetypes; } set { _filetypes = value; } }
        public string sExt { get; set; }
        public DateTime dDate { get; set; }
        public string sSize { get; set; }
        public long lSize { get; set; }
        public BitmapImage iImage
        {
            get { return _iimage; }
            set
            {
                _iimage = value;
                NotifyPropertyChanged("iImage");
            }
        }
        //public string sProtocolType { get; set; }
        public Visibility bPBVisible
        {
            get { return _bpbvisible; }
            set
            {
                _bpbvisible = value;
                NotifyPropertyChanged("bPBVisible");
            }
        }
        public int RHeight
        {
            get { return _rheight; }
            set
            {
                _rheight = value;
                NotifyPropertyChanged("RHeight");
            }
        }
        public int iProgress
        {
            get { return _progress; }
            set
            {
                _progress = value;
                NotifyPropertyChanged("iProgress");
            }
        }

        //public string sUser { get; set; }
        //public string sPassword { get; set; }
    }

    public class PredefinedImage
    {
        private string image_name = "";
        public string Image_name { get { return image_name; } set { image_name = value; } }
        private BitmapImage image;
        public BitmapImage Image { get { return image; } set { image = value; } }
    }

    public enum LogonType
    {
        /// <summary>
        /// This logon type is intended for users who will be interactively using the computer, such as a user being logged on  
        /// by a terminal server, remote shell, or similar process.
        /// This logon type has the additional expense of caching logon information for disconnected operations; 
        /// therefore, it is inappropriate for some client/server applications,
        /// such as a mail server.
        /// </summary>
        LOGON32_LOGON_INTERACTIVE = 2,

        /// <summary>
        /// This logon type is intended for high performance servers to authenticate plaintext passwords.

        /// The LogonUser function does not cache credentials for this logon type.
        /// </summary>
        LOGON32_LOGON_NETWORK = 3,

        /// <summary>
        /// This logon type is intended for batch servers, where processes may be executing on behalf of a user without 
        /// their direct intervention. This type is also for higher performance servers that process many plaintext
        /// authentication attempts at a time, such as mail or Web servers. 
        /// The LogonUser function does not cache credentials for this logon type.
        /// </summary>
        LOGON32_LOGON_BATCH = 4,

        /// <summary>
        /// Indicates a service-type logon. The account provided must have the service privilege enabled. 
        /// </summary>
        LOGON32_LOGON_SERVICE = 5,

        /// <summary>
        /// This logon type is for GINA DLLs that log on users who will be interactively using the computer. 
        /// This logon type can generate a unique audit record that shows when the workstation was unlocked. 
        /// </summary>
        LOGON32_LOGON_UNLOCK = 7,

        /// <summary>
        /// This logon type preserves the name and password in the authentication package, which allows the server to make 
        /// connections to other network servers while impersonating the client. A server can accept plaintext credentials 
        /// from a client, call LogonUser, verify that the user can access the system across the network, and still 
        /// communicate with other servers.
        /// NOTE: Windows NT:  This value is not supported. 
        /// </summary>
        LOGON32_LOGON_NETWORK_CLEARTEXT = 8,

        /// <summary>
        /// This logon type allows the caller to clone its current token and specify new credentials for outbound connections.
        /// The new logon session has the same local identifier but uses different credentials for other network connections. 
        /// NOTE: This logon type is supported only by the LOGON32_PROVIDER_WINNT50 logon provider.
        /// NOTE: Windows NT:  This value is not supported. 
        /// </summary>
        LOGON32_LOGON_NEW_CREDENTIALS = 9,
    }

    public enum LogonProvider
    {
        /// <summary>
        /// Use the standard logon provider for the system. 
        /// The default security provider is negotiate, unless you pass NULL for the domain name and the user name 
        /// is not in UPN format. In this case, the default provider is NTLM. 
        /// NOTE: Windows 2000/NT:   The default security provider is NTLM.
        /// </summary>
        LOGON32_PROVIDER_DEFAULT = 0,
        LOGON32_PROVIDER_WINNT35 = 1,
        LOGON32_PROVIDER_WINNT40 = 2,
        LOGON32_PROVIDER_WINNT50 = 3
    }

    
}
