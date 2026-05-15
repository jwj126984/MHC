using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MHC
{
    /// <summary>
    /// CAN配置窗口
    /// </summary>
    public partial class CANConfigWindow : Window
    {
        /// <summary>
        /// 配置结果事件
        /// </summary>
        public event Action<CANCommunication>? OnConfigSuccess;
        
        /// <summary>
        /// 取消事件
        /// </summary>
        public event Action? OnCancel;
        
        private CANCommunication _canCommunication;
        
        public CANConfigWindow()
        {
            InitializeComponent();
            InitializeComponents();
            InitializeEventHandlers();
        }
        
        private void InitializeComponents()
        {
            // 初始化设备类型
            var devices = CANCommunication.GetAvailableDevices();
            foreach (var device in devices)
            {
                DeviceTypeComboBox.Items.Add(new ComboBoxItem { Content = device.Name, Tag = device.Type });
            }
            if (devices.Count > 0)
            {
                DeviceTypeComboBox.SelectedIndex = 0;
            }

            // 初始化设备索引号 (0-3)
            for (uint i = 0; i < 4; i++)
            {
                DeviceIndexComboBox.Items.Add(new ComboBoxItem { Content = i.ToString(), Tag = i });
            }
            DeviceIndexComboBox.SelectedIndex = 0;

            // 初始化通道号 (0-3)
            for (uint i = 0; i < 4; i++)
            {
                ChannelIndexComboBox.Items.Add(new ComboBoxItem { Content = i.ToString(), Tag = i });
            }
            ChannelIndexComboBox.SelectedIndex = 0;
            
            // 初始化波特率
            var baudRates = CANCommunication.GetCommonBaudRates();
            foreach (var baudRate in baudRates)
            {
                BaudRateComboBox.Items.Add(new ComboBoxItem { Content = baudRate.Name, Tag = baudRate.BaudRate });
            }
            if (baudRates.Count > 0)
            {
                // 默认选择500 kbps
                for (int i = 0; i < baudRates.Count; i++)
                {
                    if (baudRates[i].BaudRate == 500000)
                    {
                        BaudRateComboBox.SelectedIndex = i;
                        break;
                    }
                }
                if (BaudRateComboBox.SelectedIndex == -1)
                {
                    BaudRateComboBox.SelectedIndex = 0;
                }
            }
        }
        
        private void InitializeEventHandlers()
        {
            ConnectButton.Click += ConnectButton_Click;
            CancelButton.Click += CancelButton_Click;
        }
        
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取选中的配置
                var selectedDeviceType = (CANDeviceType)((ComboBoxItem)DeviceTypeComboBox.SelectedItem).Tag;
                var selectedDeviceIndex = (uint)((ComboBoxItem)DeviceIndexComboBox.SelectedItem).Tag;
                var selectedChannelIndex = (uint)((ComboBoxItem)ChannelIndexComboBox.SelectedItem).Tag;
                var selectedBaudRate = (uint)((ComboBoxItem)BaudRateComboBox.SelectedItem).Tag;
                
                // 显示连接状态
                StatusTextBlock.Text = "正在连接...";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Blue;
                
                // 创建CAN通信实例
                _canCommunication = new CANCommunication();
                
                // 订阅错误事件
                _canCommunication.OnError += (error) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        StatusTextBlock.Text = error;
                        StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    });
                };
                
                // 打开设备
                bool deviceOpened = _canCommunication.OpenDevice(selectedDeviceType, selectedDeviceIndex);
                if (!deviceOpened)
                {
                    StatusTextBlock.Text = "打开设备失败";
                    StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }
                
                // 初始化通道，根据设备类型设置是否为CAN FD
                bool isCANFD = selectedDeviceType == CANDeviceType.USBCANFD_200U || selectedDeviceType == CANDeviceType.USBCANFD_100U;
                bool channelInitialized = _canCommunication.InitChannel(selectedChannelIndex, selectedBaudRate, CANWorkMode.Normal, isCANFD);
                if (!channelInitialized)
                {
                    StatusTextBlock.Text = "初始化通道失败";
                    StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    _canCommunication.CloseDevice();
                    return;
                }
                
                // 启动CAN
                bool canStarted = _canCommunication.StartCAN();
                if (!canStarted)
                {
                    StatusTextBlock.Text = "启动CAN失败";
                    StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    _canCommunication.CloseDevice();
                    return;
                }
                
                // 连接成功
                StatusTextBlock.Text = "连接成功";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                
                // 延迟一下，让用户看到成功消息
                System.Threading.Thread.Sleep(500);
                
                // 触发配置成功事件
                OnConfigSuccess?.Invoke(_canCommunication);
                
                // 关闭窗口
                this.Close();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "连接失败: " + ex.Message;
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                _canCommunication?.CloseDevice();
            }
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            OnCancel?.Invoke();
            this.Close();
        }
    }
}