using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.IO;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Management;
using System.Windows;

namespace LaExplorer.Code
{
    class USBWatcher
    {
        public USBWatcher()
        {
            ManagementEventWatcher w = null;
            WqlEventQuery q;
            ManagementOperationObserver observer = new
            ManagementOperationObserver();
            // Bind to local machine
            ManagementScope scope = new ManagementScope("root\\CIMV2");
            scope.Options.EnablePrivileges = true; //sets required privilege
            try
            {
                q = new WqlEventQuery();
                q.EventClassName = "__InstanceOperationEvent";
                q.WithinInterval = new TimeSpan(0, 0, 3);
                q.Condition = @"TargetInstance ISA 'Win32_DiskDrive' ";
                //Console.WriteLine(q.QueryString);
                w = new ManagementEventWatcher(q);
                w.EventArrived += new EventArrivedEventHandler(OnChanged);
                w.Start();
            }
            catch //(Exception e)
            {
                //Console.WriteLine(e.Message);
                //MessageBox.Show("Catch");
            }
            finally
            {
                //w.Stop();
                //MessageBox.Show("Final");
            }
        }
        // A delegate type for hooking up change notifications.
        public delegate void ChangedEventHandler(object sender, EventArgs e);

        // An event that clients can use to be notified whenever the
        // elements of the list change.
        public event ChangedEventHandler Changed;

        [DefaultValue(false)]
        public bool EnableRaisingEvents { get; set; }

        // Invoke the Changed event; called whenever list changes
        public void OnChanged(object sender, EventArrivedEventArgs e)
        {
            if (Changed != null)
            {
                if (EnableRaisingEvents)
                {
                    EventArgs s = new EventArgs();
                    Changed(this, s);
                }
            }
        }

        public void Close()
        {
            //int i = 1;
        }
            
    }
}

