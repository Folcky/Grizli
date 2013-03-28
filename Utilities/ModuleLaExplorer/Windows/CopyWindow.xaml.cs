using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LaExplorer.Code;
using System.IO;

namespace LaExplorer.Views
{
    /// <summary>
    /// Interaction logic for CopyWindow.xaml
    /// </summary>
    public partial class CopyWindow : Window
    {
        public CopyWindow()
        {
            InitializeComponent();
            this.Loaded +=new RoutedEventHandler(CopyWindow_Loaded);
        }

        private ObservableCollection<string> _paths;
        public ObservableCollection<string> Paths { get { return _paths; } set { _paths = value; } }

        private ObservableCollection<ParentItem> _destinations;
        public ObservableCollection<ParentItem> Destinations { get { return _destinations; } set { _destinations = value; } }

        private bool _ok;
        public bool Ok { get { return _ok; } set { _ok = value; } }
        private string _destination_old;
        public string Destination_old { get { return _destination_old; } set { _destination_old = value; } }

        private ParentItem _destination;
        public ParentItem Destination { get { return _destination; } set { _destination = value; } }

        private void CopyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            cbPaths.Items.Clear();
            ObservableCollection<string> destinations = new ObservableCollection<string>(
                ((from l in Destinations
                where l.Protocol==Protocols.LOCAL
                    select l.Path).Union(
                    from l in Destinations
                    where l.Protocol == Protocols.FTP
                    select l.GetConnectionPath()
                    )
                    .Union(
                    from l in Destinations
                    where l.Protocol == Protocols.NETWORK
                    select l.GetConnectionPath()
                    )).Reverse());
            cbPaths.ItemsSource = destinations;
            cbPaths.SelectedIndex = 0;
        }

        private void bOK_Click(object sender, RoutedEventArgs e)
        {
            Ok = true;
            if (Directory.Exists(cbPaths.Text))
                Destination = new ParentItem() { Path = cbPaths.Text, Protocol = Protocols.LOCAL };
            else
            {
                ParentItem destiny = (from l in Destinations
                                      where l.ConnectionName==cbPaths.Text.Substring(0,cbPaths.Text.IndexOf(":"))
                                      //cbPaths.Text.Contains(l.sConnectionName + ":" + (l.Path.IndexOf("/") != -1 ? l.Path.Substring(l.Path.IndexOf("/")) : "/"))
                                      select l).FirstOrDefault();

                switch (destiny.Protocol)
                { 
                    case Protocols.FTP:
                        destiny.Path = destiny.Path.Substring(0, destiny.Path.IndexOf("/")) + (cbPaths.Text.IndexOf("/") != -1 ? cbPaths.Text.Substring(cbPaths.Text.IndexOf("/")) : "/");
                        break;
                    case Protocols.NETWORK:
                        destiny.Path = destiny.Path.Substring(0, destiny.Path.IndexOf(@"\")) + (cbPaths.Text.IndexOf(@"\") != -1 ? cbPaths.Text.Substring(cbPaths.Text.IndexOf(@"\")) : @"\");
                        break;
                }

                //if (destiny.Path.IndexOf("/") != -1)
                //{
                //    destiny.Path = destiny.Path.Substring(0, destiny.Path.IndexOf("/")) + (cbPaths.Text.IndexOf("/") != -1 ? cbPaths.Text.Substring(cbPaths.Text.IndexOf("/")) : "/");
                //}
                //else
                //    destiny.Path = destiny.Path + (cbPaths.Text.IndexOf("/") != -1 ? cbPaths.Text.Substring(cbPaths.Text.IndexOf("/")) : "/");
                //destiny.Path = destiny.Path + (cbPaths.Text.IndexOf("/") != -1 ? cbPaths.Text.Substring(cbPaths.Text.IndexOf("/")) : "/");
                Destination = destiny;
            }
            this.Close();
        }

        private void bCancel_Click(object sender, RoutedEventArgs e)
        {
            Ok = false;
            this.Close();
        }

        private void bClose_Click(object sender, RoutedEventArgs e)
        {
            Ok = false;
            this.Close();
        }

    }
}
