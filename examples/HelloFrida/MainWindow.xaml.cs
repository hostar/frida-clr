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
using System.Collections.ObjectModel;

namespace HelloFrida
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Frida.DeviceManager deviceManager;

        public ObservableCollection<Frida.Device> Devices { get; private set; }
        public ObservableCollection<Frida.Process> Processes { get; private set; }
        private Frida.Session session;
        private Frida.Script script;
        private bool scriptLoaded;

        public MainWindow()
        {
            InitializeComponent();
            Devices = new ObservableCollection<Frida.Device>();
            Processes = new ObservableCollection<Frida.Process>();
            DataContext = this;
            Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }

        private void RefreshAllowedActions()
        {
            deviceList.Dispatcher.BeginInvoke(new Action(() =>
            {
                deviceList.IsEnabled = session == null;
                refreshButton.IsEnabled = session == null && deviceList.SelectedItem != null;

                processList.IsEnabled = session == null;
                spawnButton.IsEnabled = processList.SelectedItem != null;
                resumeButton.IsEnabled = processList.SelectedItem != null;
                attachButton.IsEnabled = processList.SelectedItem != null && session == null;
                detachButton.IsEnabled = session != null;

                scriptSource.IsEnabled = session != null && script == null;
                createScriptButton.IsEnabled = session != null && script == null;
                loadScriptButton.IsEnabled = script != null && !scriptLoaded;
                unloadScriptButton.IsEnabled = script != null;
                postToScriptButton.IsEnabled = script != null && scriptLoaded;
            }));
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            deviceManager = new Frida.DeviceManager();
            deviceManager.Changed += new EventHandler(deviceManager_Changed);
            RefreshDeviceList();
            RefreshAllowedActions();
        }

        private void RefreshDeviceList()
        {
            var devices = deviceManager.EnumerateDevices();
            consoleAdd($"Got {devices.Length} devices");
            Array.Sort(devices, delegate(Frida.Device a, Frida.Device b)
            {
                var aHasIcon = a.Icon != null;
                var bHasIcon = b.Icon != null;
                if (aHasIcon == bHasIcon)
                    return a.Id.CompareTo(b.Id);
                else
                    return bHasIcon.CompareTo(aHasIcon);
            });
            Devices.Clear();
            foreach (var device in devices)
            {
                Devices.Add(device);
            }
        }

        private void RefreshProcessList()
        {
            var device = deviceList.SelectedItem as Frida.Device;
            if (device == null)
            {
                Processes.Clear();
                return;
            }

            try
            {
                var processes = device.EnumerateProcesses(Frida.Scope.Full);
                Array.Sort(processes, delegate(Frida.Process a, Frida.Process b) {
                    var aHasIcon = a.Icons.Length != 0;
                    var bHasIcon = b.Icons.Length != 0;
                    if (aHasIcon == bHasIcon)
                        return a.Name.CompareTo(b.Name);
                    else
                        return bHasIcon.CompareTo(aHasIcon);
                });
                Processes.Clear();
                foreach (var process in processes)
                {
                    Processes.Add(process);
                }
            }
            catch (Exception ex)
            {
                consoleAdd("EnumerateProcesses failed: " + ex.Message);
                Processes.Clear();
            }
        }

        private void deviceManager_Changed(object sender, EventArgs e)
        {
            consoleAdd("DeviceManager Changed");
            RefreshDeviceList();
        }

        private void deviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshAllowedActions();
            RefreshProcessList();
        }

        private void refreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshProcessList();
        }

        private void processList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshAllowedActions();
        }

        private void spawnButton_Click(object sender, RoutedEventArgs e)
        {
            var device = deviceList.SelectedItem as Frida.Device;
            try
            {
                device.Spawn("C:\\Windows\\notepad.exe", new string[] { "C:\\Windows\\notepad.exe", "C:\\document.txt" }, null, null, null);
            }
            catch (Exception ex)
            {
                consoleAdd("Spawn failed: " + ex.Message);
            }
        }

        private void resumeButton_Click(object sender, RoutedEventArgs e)
        {
            var device = deviceList.SelectedItem as Frida.Device;
            try
            {
                device.Resume(session.Pid);
            }
            catch (Exception ex)
            {
                consoleAdd("Resume failed: " + ex.Message);
            }
        }

        private void attachButton_Click(object sender, RoutedEventArgs e)
        {
            var device = deviceList.SelectedItem as Frida.Device;
            var process = processList.SelectedItem as Frida.Process;

            try
            {
                session = device.Attach(process.Pid);
            }
            catch (Exception ex)
            {
                consoleAdd("Attach failed: " + ex.Message);
                return;
            }
            session.Detached += new Frida.SessionDetachedHandler(session_Detached);
            consoleAdd("Attached to " + session.Pid);
            RefreshAllowedActions();
        }

        private void detachButton_Click(object sender, RoutedEventArgs e)
        {
            session.Detach();
            session = null;
            script = null;
            RefreshAllowedActions();
        }

        private void session_Detached(object sender, Frida.SessionDetachedEventArgs e)
        {
            if (sender == session)
            {
                consoleAdd($"Detached from Session with PID {session.Pid} ({e.Reason})");
                
                session = null;
                script = null;
                RefreshAllowedActions();
            }
        }

        private void createScriptButton_Click(object sender, RoutedEventArgs e)
        {
            if (script != null)
            {
                try
                {
                    script.Unload();
                }
                catch (Exception ex)
                {
                    consoleAdd("Failed to unload previous script: " + ex.Message);
                }
                script = null;
                scriptLoaded = false;
                RefreshAllowedActions();
            }

            try
            {
                script = session.CreateScript(scriptSource.Text);
                scriptLoaded = false;
                RefreshAllowedActions();
            }
            catch (Exception ex)
            {
                consoleAdd("CreateScript failed: " + ex.Message);

                return;
            }
            script.Message += new Frida.ScriptMessageHandler(script_Message);
        }

        private void loadScriptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                script.Load();
                scriptLoaded = true;
                RefreshAllowedActions();
            }
            catch (Exception ex)
            {
                consoleAdd("Load failed: " + ex.Message);
            }
        }

        private void unloadScriptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                script.Unload();
            }
            catch (Exception ex)
            {
                consoleAdd("Failed to unload script: " + ex.Message);
            }
            script = null;
            scriptLoaded = false;
            RefreshAllowedActions();
        }

        private void postToScriptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                script.Post("{\"type\":\"" + messageSource.Text + "\"}");
            }
            catch (Exception ex)
            {
                consoleAdd("PostMessage failed: " + ex.Message);
            }
        }

        private void script_Message(object sender, Frida.ScriptMessageEventArgs e)
        {
            if (sender == script)
            {
                consoleAdd(string.Format("Message from Script: {0}", e.Message));
                consoleAdd(string.Format("  Data: {0}", e.Data == null ? "null" : string.Join(", ", e.Data)));
            }
        }

        private void consoleAdd(string str)
        {
            debugConsole.Dispatcher.BeginInvoke(new Action(() =>
            {
                debugConsole.Items.Add(str);
            }));
        }
    }
}
