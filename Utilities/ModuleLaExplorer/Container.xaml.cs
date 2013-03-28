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
using System.ComponentModel;

namespace laExplorer
{
    /// <summary>
    /// Interaction logic for Container.xaml
    /// </summary>
    public partial class Container : UserControl
    {
        public Container()
        {
            InitializeComponent();
        }

        public Container(Orientation _orient, int number_of_panels)
        {
            InitializeComponent();
            int _explorer_index = 1;
            GridLengthConverter myGridLengthConverter = new GridLengthConverter();
            for (int i = 1; i < number_of_panels; i++)
            {
                if (number_of_panels > 1)
                    if (Orientation.Horizontal == _orient)
                    {
                        if (i == 1)
                            GridContainer.ColumnDefinitions.Add(new ColumnDefinition { });
                        GridContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = (GridLength)myGridLengthConverter.ConvertFrom(3), Name = "csplit" + i });
                        GridContainer.ColumnDefinitions.Add(new ColumnDefinition { });
                    }
                if (Orientation.Vertical == _orient)
                {
                    if (i == 1)
                        GridContainer.RowDefinitions.Add(new RowDefinition { });
                    GridContainer.RowDefinitions.Add(new RowDefinition { Height = (GridLength)myGridLengthConverter.ConvertFrom(3), Name = "rsplit" + i });
                    GridContainer.RowDefinitions.Add(new RowDefinition { });
                }
            }
            if (Orientation.Horizontal == _orient)
                for (int i = 0; i < GridContainer.ColumnDefinitions.Count; i++)
                {
                    if (GridContainer.ColumnDefinitions[i].Name.Contains("split"))
                    {
                        GridSplitter n1 = new GridSplitter();
                        n1.SetValue(Grid.ColumnProperty, i);
                        n1.VerticalAlignment = VerticalAlignment.Stretch;
                        n1.HorizontalAlignment = HorizontalAlignment.Stretch;
                        GridContainer.Children.Add(n1);
                    }
                    if (GridContainer.ColumnDefinitions[i].Name == "")
                    {
                        Explorer e1 = new Explorer(this);
                        e1.SetValue(Grid.ColumnProperty, i);
                        GridContainer.Children.Add(e1);
                        e1.VerticalAlignment = VerticalAlignment.Stretch;
                        e1.HorizontalAlignment = HorizontalAlignment.Stretch;
                        //Привязка уведомителя к методу изменения свойства
                        e1.CPanelChanged += new System.ComponentModel.PropertyChangedEventHandler(this.OnPanelsChanged);
                        e1.PanelIndex = _explorer_index;
                        _explorer_index++;
                        //Заполняем ссылками на Explorers
                        panels.sSources.Add(e1);
                    }
                }
            if (Orientation.Vertical == _orient)
                for (int i = 0; i < GridContainer.RowDefinitions.Count; i++)
                {
                    if (GridContainer.RowDefinitions[i].Name.Contains("split"))
                    {
                        GridSplitter n1 = new GridSplitter();
                        n1.SetValue(Grid.RowProperty, i);
                        n1.HorizontalAlignment = HorizontalAlignment.Stretch;
                        n1.VerticalAlignment = VerticalAlignment.Stretch;
                        GridContainer.Children.Add(n1);
                    }
                    if (GridContainer.RowDefinitions[i].Name == "")
                    {
                        Explorer e1 = new Explorer(this);
                        e1.SetValue(Grid.RowProperty, i);
                        e1.VerticalAlignment = VerticalAlignment.Stretch;
                        e1.HorizontalAlignment = HorizontalAlignment.Stretch;
                        GridContainer.Children.Add(e1);
                        e1.CPanelChanged += new System.ComponentModel.PropertyChangedEventHandler(this.OnPanelsChanged);
                        e1.PanelIndex = _explorer_index;
                        _explorer_index++;
                    }
                }
            this.PanelsInfo = panels;
        }

        static Container()
        {
            _Orientation = DependencyProperty.Register("Orientation", typeof(Orientation), typeof(Container));
            _PanelsInfo = DependencyProperty.Register("PanelsInfo", typeof(PanelsDescription), typeof(Container));
        }
        
        PanelsDescription panels = new PanelsDescription();

        public static DependencyProperty NPanels;
        public static DependencyProperty PanelInfo;
        //public event System.ComponentModel.PropertyChangedEventHandler CPanelChanged;
        
        //_Orientation
        public static DependencyProperty _Orientation;
        public Orientation Orientation
        {
            get
            {
                return (Orientation)GetValue(_Orientation);
            }
            set
            {
                SetValue(_Orientation, value);
            }
        }

        //_PanelsInfo
        public static DependencyProperty _PanelsInfo;
        public event System.ComponentModel.PropertyChangedEventHandler CPanelsChanged;
        public PanelsDescription PanelsInfo
        {
            get
            {
                return (PanelsDescription)GetValue(_PanelsInfo);
            }
            set
            {
                SetValue(_PanelsInfo, value);
                //Включить уведомление, что свойство изменилось
                OnCPanelsChanged(new PropertyChangedEventArgs("PanelsInfo"));
            }
        }
        //Уведомляем потребителей, что свойство изменилось
        public void OnCPanelsChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (CPanelsChanged != null)
                CPanelsChanged(this, e);
        }
        //Использовать для привязки с дочерним usercontrol
        //Метод изменения свойства
        private void OnPanelsChanged(object sender, PropertyChangedEventArgs e)
        {
            //TODO?
            if (this.PanelsInfo != null)
            {
                this.PanelsInfo.CurrentPanelIndex = ((Explorer)sender).PanelIndex;
                this.PanelsInfo.sPath = ((Explorer)sender).ListItems.sPath;
                //Уведомляем потребителей, что свойство изменилось
                if (CPanelsChanged != null)
                    CPanelsChanged(this, e);
            }
        }


        //Attached Property, FYI just for fun
        public static readonly DependencyProperty RotationProperty =
            DependencyProperty.RegisterAttached("Rotation", typeof(double), typeof(Container));

        public static void SetRotation(UIElement element, double value)
        {
            element.SetValue(RotationProperty, value);
        }
        public static double GetRotation(UIElement element)
        {
            return (double)element.GetValue(RotationProperty);
        }


        public void GetItems(int index, string command)
        {
            List<DependencyObject> childs = FindChilds(this, typeof(Explorer));
            if (childs.Count() != 0)
            {
                foreach (DependencyObject child in childs)
                {
                    if (((Explorer)child).PanelIndex == index)
                        ((Explorer)child).GetItems(new Item { bFolder = 1, sFullName=command==null || command=="home" ? null : command });
                }
            }
        }

        public List<DependencyObject> FindChilds(DependencyObject reference, Type type)
        {
            List<DependencyObject> result=new List<DependencyObject>();
            if (reference != null)
            {
                int childrenCount = VisualTreeHelper.GetChildrenCount(reference);
                for (int i = 0; i < childrenCount; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(reference, i);
                    if (child.GetType()==type)
                    result.Add(child);
                    foreach (DependencyObject found in FindChilds(child, type))
                    {
                        if (found.GetType() == type)
                        result.Add(found);
                    }
                }
            }
            return result;
        }
    }

    public static class UIChildFinder
    {
        public static DependencyObject FindChild(this DependencyObject reference, string childName, Type childType)
        {
            DependencyObject foundChild = null;
            if (reference != null)
            {
                int childrenCount = VisualTreeHelper.GetChildrenCount(reference);
                for (int i = 0; i < childrenCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(reference, i);
                    // If the child is not of the request child type child
                    if (child.GetType() != childType)
                    {
                        // recursively drill down the tree
                        foundChild = FindChild(child, childName, childType);
                    }
                    else if (!string.IsNullOrEmpty(childName))
                    {
                        var frameworkElement = child as FrameworkElement;
                        // If the child's name is set for search
                        if (frameworkElement != null && frameworkElement.Name == childName)
                        {
                            // if the child's name is of the request name
                            foundChild = child;
                            break;
                        }
                    }
                    else
                    {
                        // child element found.
                        foundChild = child;
                        break;
                    }
                }
            }
            return foundChild;
        }

        public static void Close()
        {
        }
    }


}
