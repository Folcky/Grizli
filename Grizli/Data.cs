using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Practices.Prism.Regions;
using System.Windows.Media.Imaging;

namespace Grizli
{
    public class Commander
    {
        public string Command { get; set; }
        public string ModuleName { get; set; }
        public BitmapImage ModuleImage { get; set; }
    }

    public class Bookmark
    {
        [XmlElement("Module")]
        public string Module;
        private List<string> _modulecommand = new List<string>();
        [XmlArray("ModuleCommands")]
        public List<string> ModuleCommand
        {
            get { return _modulecommand; }
            set 
            {
                if (value != null)
                    _modulecommand = value;
            }
        }
    }

    [XmlRoot("Bookmarks")]
    public class Bookmarks
    {
        private readonly string data_path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)+@"\Grizli";
        private string xml_file = "";

        public Bookmarks()
        {
            Directory.CreateDirectory(data_path);
            xml_file = data_path + @"\bookmarks.xml";
        }

        private List<Bookmark> _modulebookmarks = new List<Bookmark>();
        [XmlArray("ModuleBookmarks")]
        public List<Bookmark> ModuleBookmarks
        {
            get 
            {
                return _modulebookmarks;
            }
            set 
            {
                _modulebookmarks=value;
            }
        }

        public void Save()
        {
            XmlSerializer s = new XmlSerializer(typeof(Bookmarks));
            TextWriter w = new StreamWriter(xml_file);
            s.Serialize(w, this);
            w.Flush();
            w.Close();
        }
        public Bookmarks Load()
        {
            Bookmarks bookmarklist = new Bookmarks();
            if (File.Exists(xml_file))
            {
                try
                {
                    XmlSerializer s = new XmlSerializer(typeof(Bookmarks));
                    TextReader r = new StreamReader(xml_file);
                    bookmarklist = (Bookmarks)s.Deserialize(r);
                    r.Close();
                }
                catch { }
            }
            return bookmarklist;
        }
    }

    partial class BaseDictionary : ResourceDictionary
    { 
       public BaseDictionary()
       {
           InitializeComponent();
       }

       public void ChpokTab(object sender, RoutedEventArgs e)
       {
           ((sender as Button).Tag.GetType().GetProperty("My_region").GetValue((sender as Button).Tag, null) as IRegion).Remove((sender as Button).Tag);
           GC.Collect();
       }
    }
}
