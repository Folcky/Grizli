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
using LaExplorer.Code;

namespace LaExplorer.Windows
{
    /// <summary>
    /// Interaction logic for Connection.xaml
    /// </summary>
    public partial class FTPConnection : UserControl
    {
        public FTPConnection()
        {
            InitializeComponent();
        }

        FTPList ftplist = new FTPList();
        private void FTPConnection_Loaded(object sender, RoutedEventArgs e)
        {
            ftplist = ftplist.Load();
            foreach (FTPHost host in ftplist.ftphosts)
            {
                cbFTPnames.Items.Add(host._sname);
                lbFTPConnects.Items.Add(host._sname);
            }
            foreach (FTPHost host in ftplist.ftphosts)
            {
                cbFTPhost.Items.Add(host._shost);
            }
            this.Loaded -= new RoutedEventHandler(FTPConnection_Loaded);
        }

        private void lbFTPConnects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbFTPConnects.SelectedIndex != -1)
            {
                FTPHost host = (from l in ftplist.ftphosts
                                where l._sname == lbFTPConnects.SelectedValue.ToString()
                                select l).FirstOrDefault();

                if (host != null)
                {
                    cbFTPnames.Text = host._sname;
                    cbFTPhost.Text = host._shost;
                    tbUser.Text = host._suser;
                    tbPass.Text = host._spassword;
                    tbCWD.Text = host._cwd;
                    cbFtpPassiveMode.IsChecked = host._passivemode;
                }
            }

        }
        private void bDelete_Click(object sender, RoutedEventArgs e)
        {
            if (lbFTPConnects.SelectedIndex != -1)
            {
                FTPHost host = (from l in ftplist.ftphosts
                                where l._sname == lbFTPConnects.SelectedValue.ToString()
                                select l).FirstOrDefault();
                if (host != null)
                {
                    ftplist.ftphosts.Remove(host);
                    lbFTPConnects.Items.Remove(host._sname);

                    cbFTPnames.Text = "";
                    cbFTPhost.Text = "";
                    tbUser.Text = "";
                    tbPass.Text = "";
                    cbFtpPassiveMode.IsChecked = false;

                    ftplist.Save();
                }
            }
        }

        private void bOK_Click(object sender, RoutedEventArgs e)
        {
            FTPHost host = (from l in ftplist.ftphosts
                            where l._sname == cbFTPnames.Text
                            select l).FirstOrDefault();
            if (cbFTPhost.Text != "" && cbFTPnames.Text != "")
                if (host == null)
                {
                    ftplist.ftphosts.Add(new FTPHost
                    {
                        _shost = cbFTPhost.Text,
                        _sname = cbFTPnames.Text,
                        _suser = tbUser.Text,
                        _spassword = tbPass.Text,
                        _cwd = tbCWD.Text,
                        _passivemode = (bool)cbFtpPassiveMode.IsChecked
                    });
                    ftplist.Save();
                }
                else
                {
                    host._passivemode = (bool)cbFtpPassiveMode.IsChecked;
                    host._shost = cbFTPhost.Text;
                    host._sname = cbFTPnames.Text;
                    host._suser = tbUser.Text;
                    host._spassword = tbPass.Text;
                    host._cwd = tbCWD.Text;
                    ftplist.Save();
                }
        }
    }
}
