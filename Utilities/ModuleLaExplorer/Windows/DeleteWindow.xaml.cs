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

namespace LaExplorer.Views
{
    /// <summary>
    /// Interaction logic for DeleteWindow.xaml
    /// </summary>
    public partial class DeleteWindow : Window
    {
        public DeleteWindow()
        {
            InitializeComponent();
            this.Loaded +=new RoutedEventHandler(DeleteWindow_Loaded);
        }
        
        private ObservableCollection<Item> _objects;
        public ObservableCollection<Item> Objects { get { return _objects; } set { _objects = value; } }

        private bool _ok;
        public bool Ok { get { return _ok; } set { _ok = value; } }

        private void DeleteWindow_Loaded(object sender, RoutedEventArgs e)
        {
            lbObjects.Items.Clear();
            lbObjects.ItemsSource = Objects;
        }

        private void bOK_Click(object sender, RoutedEventArgs e)
        {
            Ok = true;
            this.Close();
        }

        private void bCancel_Click(object sender, RoutedEventArgs e)
        {
            Ok = false;
            this.Close();
        }

        private void bClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
