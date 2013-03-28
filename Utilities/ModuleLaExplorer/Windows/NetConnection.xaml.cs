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
    public partial class NetConnection : UserControl
    {
        public NetConnection()
        {
            InitializeComponent();
        }

        NetList netlist = new NetList();
        private void FTPConnection_Loaded(object sender, RoutedEventArgs e)
        {
            netlist = netlist.Load();
            foreach (NetHost host in netlist.nethosts)
            {
                cbNetnames.Items.Add(host._name);
                lbNetConnects.Items.Add(host._name);
            }
            foreach (NetHost host in netlist.nethosts)
            {
                cbPath.Items.Add(host._path);
            }
            this.Loaded -= new RoutedEventHandler(FTPConnection_Loaded);
        }

        private void lbNetConnects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbNetConnects.SelectedIndex != -1)
            {
                NetHost host = (from l in netlist.nethosts
                                where l._name == lbNetConnects.SelectedValue.ToString()
                                select l).FirstOrDefault();

                if (host != null)
                {
                    cbNetnames.Text = host._name;
                    cbPath.Text = host._path;
                    tbDomain.Text = host._domain;
                    tbUser.Text = host._user;
                    tbPass.Text = host._password;
                    cbUseCurrentUser.IsChecked = host._usecurrentuser;
                }
            }

        }
        private void bDelete_Click(object sender, RoutedEventArgs e)
        {
            if (lbNetConnects.SelectedIndex != -1)
            {
                NetHost host = (from l in netlist.nethosts
                                where l._name == lbNetConnects.SelectedValue.ToString()
                                select l).FirstOrDefault();
                if (host != null)
                {
                    netlist.nethosts.Remove(host);
                    lbNetConnects.Items.Remove(host._name);

                    cbNetnames.Text = "";
                    cbPath.Text = "";
                    tbDomain.Text = host._domain;
                    tbUser.Text = "";
                    tbPass.Text = "";
                    cbUseCurrentUser.IsChecked = false;

                    netlist.Save();
                }
            }
        }

        private void bOK_Click(object sender, RoutedEventArgs e)
        {
            NetHost host = (from l in netlist.nethosts
                            where l._name == cbNetnames.Text
                            select l).FirstOrDefault();
            if (cbPath.Text != "" && cbNetnames.Text != "")
                if (host == null)
                {
                    netlist.nethosts.Add(new NetHost
                    {
                        _path = cbPath.Text,
                        _name = cbNetnames.Text,
                        _domain = tbDomain.Text,
                        _user = tbUser.Text,
                        _password = tbPass.Text,
                        _usecurrentuser = (bool)cbUseCurrentUser.IsChecked
                    });
                    netlist.Save();
                }
                else
                {
                    host._usecurrentuser = (bool)cbUseCurrentUser.IsChecked;
                    host._path = cbPath.Text;
                    host._name = cbNetnames.Text;
                    host._domain = tbDomain.Text;
                    host._user = tbUser.Text;
                    host._password = tbPass.Text;
                    netlist.Save();
                }
        }
    }
}
