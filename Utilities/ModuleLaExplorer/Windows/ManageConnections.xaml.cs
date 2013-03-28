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
using System.Windows.Shapes;
using LaExplorer.Code;

namespace LaExplorer.Windows
{
    /// <summary>
    /// Interaction logic for ManageConnections.xaml
    /// </summary>
    public partial class ManageConnections : Window
    {
        public ManageConnections()
        {
            InitializeComponent();
            Spisok = new List<object>();
        }

        List<object> Spisok;

        private void bFTP_Click(object sender, RoutedEventArgs e)
        {
            object ftp = (from l in Spisok
                         where l.GetType() == typeof(FTPConnection)
                         select l).FirstOrDefault();
            if (ftp != null && PageTransition1.CurrentPage!=ftp)
                PageTransition1.ShowPage(ftp as UserControl);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FTPConnection ftp = new FTPConnection();
            NetConnection net = new NetConnection();
            net.bCancel.Click += new RoutedEventHandler(bClose_Click);
            ftp.bCancel.Click += new RoutedEventHandler(bClose_Click);
            Spisok.Add(ftp);
            Spisok.Add(net);

        }

        private void bClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void bNetwork_Click(object sender, RoutedEventArgs e)
        {
            object net = (from l in Spisok
                          where l.GetType() == typeof(NetConnection)
                          select l).FirstOrDefault();
            if (net != null && PageTransition1.CurrentPage != net)
                PageTransition1.ShowPage(net as UserControl);
        }
    }
}
