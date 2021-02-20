using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Display;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DetectMonitorConnect
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DeviceWatcher _displayWatcher;
        private bool _fWatcherStarted;
        private DeviceWatcher _deviceWatcher;

        public ObservableCollection<DiscoveredDevice> DiscoveredDevices { get; } = new ObservableCollection<DiscoveredDevice>();

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            StartWatcher();
        }

        public void StartWatcher()
        {
            if (_displayWatcher == null)
            {
                var deviceSelector = WiFiDirectDevice.GetDeviceSelector(WiFiDirectDeviceSelectorType.DeviceInterface);
                _deviceWatcher = DeviceInformation.CreateWatcher(deviceSelector, new string[] { "System.Devices.WiFiDirect.InformationElements" });

                _deviceWatcher.Added += OnDeviceAdded;
                _deviceWatcher.Removed += OnDeviceRemoved;
                _deviceWatcher.Updated += OnDeviceUpdated;
                _deviceWatcher.EnumerationCompleted += OnEnumerationCompleted;
                _deviceWatcher.Stopped += OnStopped;

                _deviceWatcher.Start();

                var ds = DisplayMonitor.GetDeviceSelector();
                _displayWatcher = DeviceInformation.CreateWatcher(ds);
                _displayWatcher.Added += OnDisplayMonitorAdded;
                _displayWatcher.Removed += OnDisplayMonitorRemoved;
                _displayWatcher.Updated += OnDisplayMonitorUpdated;
                _displayWatcher.EnumerationCompleted += OnEnumerationDisplayMonitorCompleted;
                _displayWatcher.Stopped += OnDisplayMonitorStopped;

                _displayWatcher.Start();

                _fWatcherStarted = true;
            }
            else
            {
                StopWatcher();

                NotifyUser("Device watcher stopped.");
            }
        }

        private void StopWatcher()
        {
            _displayWatcher.Added -= OnDisplayMonitorAdded;
            _displayWatcher.Removed -= OnDisplayMonitorRemoved;
            _displayWatcher.Updated -= OnDisplayMonitorUpdated;
            _displayWatcher.EnumerationCompleted -= OnEnumerationDisplayMonitorCompleted;
            _displayWatcher.Stopped -= OnDisplayMonitorStopped;

            _displayWatcher.Stop();

            _deviceWatcher = null;
            _deviceWatcher.Added -= OnDeviceAdded;
            _deviceWatcher.Removed -= OnDeviceRemoved;
            _deviceWatcher.Updated -= OnDeviceUpdated;
            _deviceWatcher.EnumerationCompleted -= OnEnumerationCompleted;
            _deviceWatcher.Stopped -= OnStopped;

            _deviceWatcher.Stop();

            _deviceWatcher = null;

        }

        #region DeviceWatcherEvents
        private async void OnDisplayMonitorAdded(DeviceWatcher deviceWatcher, DeviceInformation deviceInfo)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                DiscoveredDevices.Add(new DiscoveredDevice(deviceInfo));
                NotifyUserFromBackground($"Display monitor added. Name: {deviceInfo.Name}");

            });

        }

        private async void OnDisplayMonitorRemoved(DeviceWatcher deviceWatcher, DeviceInformationUpdate deviceInfoUpdate)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                foreach (DiscoveredDevice discoveredDevice in DiscoveredDevices)
                {
                    if (discoveredDevice.DeviceInfo.Id == deviceInfoUpdate.Id)
                    {
                        DiscoveredDevices.Remove(discoveredDevice);
                        NotifyUserFromBackground($"Display monitor removed. Name: {discoveredDevice.DisplayName}");
                        break;
                    }
                }
            });
        }

        private async void OnDisplayMonitorUpdated(DeviceWatcher deviceWatcher, DeviceInformationUpdate deviceInfoUpdate)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                foreach (DiscoveredDevice discoveredDevice in DiscoveredDevices)
                {
                    if (discoveredDevice.DeviceInfo.Id == deviceInfoUpdate.Id)
                    {
                        discoveredDevice.UpdateDeviceInfo(deviceInfoUpdate);
                        NotifyUserFromBackground($"Display monitor updated. Name: {discoveredDevice.DisplayName}");

                        break;
                    }
                }
            });
        }

        private void OnEnumerationDisplayMonitorCompleted(DeviceWatcher deviceWatcher, object o)
        {
            NotifyUserFromBackground("Display monitor enumeration completed");
        }

        public async void NotifyUserFromBackground(string strMessage)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                NotifyUser(strMessage);
            });
        }

        private void NotifyUser(string strMessage)
        {
            txtStatus.Text = txtStatus.Text + "\n" + strMessage;
        }

        private void OnDisplayMonitorStopped(DeviceWatcher deviceWatcher, object o)
        {
            NotifyUserFromBackground("DeviceWatcher stopped");

        }
        #endregion



        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (_displayWatcher != null)
            {
                StopWatcher();
            }
        }

        #region DeviceWatcherEvents
        private async void OnDeviceAdded(DeviceWatcher deviceWatcher, DeviceInformation deviceInfo)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                DiscoveredDevices.Add(new DiscoveredDevice(deviceInfo));
            });

            var isWifiDirect = deviceInfo.Id.ToLower().Contains("wifidi");
            NotifyUserFromBackground($"Device added. Name: {deviceInfo.Name} WiFiDirect: {(isWifiDirect ? "Yes" : "No")}");

        }

        private async void OnDeviceRemoved(DeviceWatcher deviceWatcher, DeviceInformationUpdate deviceInfoUpdate)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                foreach (DiscoveredDevice discoveredDevice in DiscoveredDevices)
                {
                    if (discoveredDevice.DeviceInfo.Id == deviceInfoUpdate.Id)
                    {
                        DiscoveredDevices.Remove(discoveredDevice);
                        NotifyUserFromBackground($"Device removed. Name: {discoveredDevice.DisplayName}");

                        break;
                    }
                }
            });
        }

        private async void OnDeviceUpdated(DeviceWatcher deviceWatcher, DeviceInformationUpdate deviceInfoUpdate)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                foreach (DiscoveredDevice discoveredDevice in DiscoveredDevices)
                {
                    if (discoveredDevice.DeviceInfo.Id == deviceInfoUpdate.Id)
                    {
                        discoveredDevice.UpdateDeviceInfo(deviceInfoUpdate);
                        NotifyUserFromBackground($"Device updated. Name: {discoveredDevice.DisplayName}");

                        break;
                    }
                }
            });
        }

        private void OnEnumerationCompleted(DeviceWatcher deviceWatcher, object o)
        {
            NotifyUserFromBackground("DeviceWatcher enumeration completed");
        }

        private void OnStopped(DeviceWatcher deviceWatcher, object o)
        {
            NotifyUserFromBackground("DeviceWatcher stopped");
        }
        #endregion


    }
}
