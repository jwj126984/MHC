using System;
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.IO;
using System.Collections.Concurrent;
using MHC.ViewModel;
using MHC.Model;

namespace MHC
{
    /// <summary>
    /// CAN帧数据模型
    /// </summary>
    public class CANFrame
    {
        public string Time { get; set; }
        public string Direction { get; set; }
        public string FrameId { get; set; }
        public string FrameType { get; set; }
        public string ProtocolType { get; set; }
        public string DataLength { get; set; }
        public string Data { get; set; }
        public string Status { get; set; }
    }

    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                if (doubleValue == -1.0)
                {
                    return Brushes.Gray; // 没有报文时显示灰色
                }
                return doubleValue > 0 ? Brushes.Red : Brushes.Green;
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                if (doubleValue == -1.0)
                {
                    return "无"; // 没有报文时显示"无"
                }
                // 检查参数，判断是否为均衡状态
                bool isBalance = parameter != null && parameter.ToString().Equals("balance", StringComparison.OrdinalIgnoreCase);
                if (isBalance)
                {
                    return doubleValue > 0 ? "是" : "否";
                }
                else
                {
                    return doubleValue > 0 ? "故障" : "正常";
                }
            }
            return "无";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                if (doubleValue == -1.0)
                {
                    return "无"; // 没有报文时显示"无"
                }
                switch ((int)doubleValue)
                {
                    case 0:
                        return "正常";
                    case 1:
                        return "PVDD过压";
                    case 2:
                        return "欠压";
                    case 3:
                        return "VCP欠压";
                    case 4:
                        return "SPI时钟异常";
                    default:
                        return "正常";
                }
            }
            return "无";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                if (doubleValue == -1.0)
                {
                    return Brushes.Gray; // 没有报文时显示灰色
                }
                switch ((int)doubleValue)
                {
                    case 0:
                        return Brushes.Green; // 正常
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        return Brushes.Red; // 异常
                    default:
                        return Brushes.Green;
                }
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InternalStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                if (doubleValue == -1.0)
                {
                    return "无"; // 没有报文时显示"无"
                }
                
                int status = (int)doubleValue;
                if (status == 0)
                {
                    return "正常";
                }
                
                StringBuilder sb = new StringBuilder();
                
                if ((status & (1 << 0)) != 0) sb.Append("SPI错误, ");
                if ((status & (1 << 1)) != 0) sb.Append("SPI时钟错误, ");
                if ((status & (1 << 2)) != 0) sb.Append("上电复位, ");
                if ((status & (1 << 3)) != 0) sb.Append("nFAULT引脚故障, ");
                if ((status & (1 << 4)) != 0) sb.Append("警告指示, ");
                if ((status & (1 << 5)) != 0) sb.Append("DS/GS故障, ");
                if ((status & (1 << 6)) != 0) sb.Append("欠压故障, ");
                if ((status & (1 << 7)) != 0) sb.Append("过压故障, ");
                if ((status & (1 << 8)) != 0) sb.Append("PVDD欠压, ");
                if ((status & (1 << 9)) != 0) sb.Append("PVDD过压, ");
                if ((status & (1 << 10)) != 0) sb.Append("VCP欠压, ");
                if ((status & (1 << 11)) != 0) sb.Append("过温警告, ");
                if ((status & (1 << 12)) != 0) sb.Append("过温关断, ");
                if ((status & (1 << 13)) != 0) sb.Append("看门狗故障, ");
                if ((status & (1 << 14)) != 0) sb.Append("复合警告, ");
                
                if (sb.Length > 0)
                {
                    return sb.ToString().TrimEnd(',', ' ');
                }
                
                return "正常";
            }
            return "无";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InternalStatusBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                if (doubleValue == -1.0)
                {
                    return Brushes.Gray; // 没有报文时显示灰色
                }
                
                int status = (int)doubleValue;
                return status == 0 ? Brushes.Green : Brushes.Red;
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

   

    public class CurrentToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                if (doubleValue == -1.0)
                {
                    return Brushes.Gray; // 没有报文时显示灰色
                }
                return doubleValue > 0 ? Brushes.Green : Brushes.Red;
            }
            return Brushes.Gray; // 没有报文时显示灰色
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MochainStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                if (doubleValue == -1.0)
                {
                    return "无"; // 没有报文时显示"无"
                }
                switch ((int)doubleValue)
                {
                    case 0:
                        return "正常";
                    case 1:
                        return "充电状态";
                    case 2:
                        return "放电状态";
                    case 3:
                        return "故障模式";
                    default:
                        return "正常";
                }
            }
            return "无";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MochainStatusBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                if (doubleValue == -1.0)
                {
                    return Brushes.Gray; // 没有报文时显示灰色
                }
                switch ((int)doubleValue)
                {
                    case 0:
                        return Brushes.Green; // 正常
                    case 1:
                    case 2:
                        return Brushes.Orange; // 充电/放电状态
                    case 3:
                        return Brushes.Red; // 故障模式
                    default:
                        return Brushes.Green;
                }
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Threading.Timer? _dataRefreshTimer;
        private bool _canConnected = true;
        private CANCommunication? _canCommunication;
        private MainViewModel _mainViewModel;
        private MotorViewModel _motorViewModel;
        private CapacitorViewModel _capacitorViewModel;
        public ObservableCollection<CANFrame> CANFrames { get; set; }
        private FaultManager _faultManager;
        
        // 点控按钮状态变量
        private bool _frontLeftLockEnabled = false;
        private bool _frontRightLockEnabled = false;
        private bool _rearLeftLockEnabled = false;
        private bool _rearRightLockEnabled = false;
        private bool _rearLeftChildLockEnabled = false;
        private bool _rearRightChildLockEnabled = false;
        
        // 自动化控制按钮状态变量
        private bool _dvTestEnabled = false;
        private bool _heatTestEnabled = false;
        private bool _emcTestEnabled = false;
        
        // 充放电控制按钮状态变量
        private bool _chargingEnabled = false;
        private bool _dischargingEnabled = false;
        
        // 存储参数值
        private ushort _storedCycleCount = 0;
        private ushort _storedDelay = 0;
        
        // 计数变量
        private int _sentFrameCount = 0;
        private int _receivedFrameCount = 0;
        private int _errorFrameCount = 0;

        public MainWindow()
        {
            InitializeComponent();
            InitializeViewModels();
            InitializeCANFrames();
            InitializeFaultManager();
            InitializeDataRefresh();
            InitializeEventHandlers();
        }

        public MainWindow(CANCommunication canCommunication)
        {
            InitializeComponent();
            InitializeViewModels();
            InitializeCANFrames();
            InitializeFaultManager();
            _canCommunication = canCommunication;
            _canConnected = true;
            InitializeDataRefresh();
            InitializeEventHandlers();
            InitializeCANCommunication();
        }

        private void InitializeViewModels()
        {
            _mainViewModel = new MainViewModel();
            _motorViewModel = new MotorViewModel(_mainViewModel);
            _capacitorViewModel = new CapacitorViewModel(_mainViewModel);
            DataContext = _mainViewModel;
        }

        private void InitializeCANFrames()
        {
            CANFrames = new ObservableCollection<CANFrame>();
        }

        private void InitializeFaultManager()
        {
            // 初始化故障管理器，日志文件存储在Logs目录
            _faultManager = new FaultManager();
        }

        private void InitializeDataRefresh()
        {
            // 数据刷新周期 ≤100ms
            _dataRefreshTimer = new System.Threading.Timer(RefreshData, null, 0, 100);
        }

        private void InitializeEventHandlers()
        {
            // 读取故障码按钮
            ReadFaultCodesButton.Click += (sender, e) => ReadFaultCodes();

            // 生成日志按钮


            // 读取版本号按钮
            ReadVersionButton.Click += (sender, e) => ReadVersion();

            // 进入休眠按钮
            EnterSleepButton.Click += (sender, e) => EnterSleep();

            // 退出休眠按钮
            ExitSleepButton.Click += (sender, e) => ExitSleep();

            // CAN唤醒按钮
            CanWakeupButton.Click += (sender, e) => CanWakeup();

            // 充电按钮
            ChargeButton.Click += (sender, e) => StartCharging();

            // 放电按钮
            DischargeButton.Click += (sender, e) => StartDischarging();

            // 点控功能按钮
            FrontLeftLockUnlockButton.Click += (sender, e) => ControlFrontLeftLock();
            FrontRightLockUnlockButton.Click += (sender, e) => ControlFrontRightLock();
            RearLeftLockUnlockButton.Click += (sender, e) => ControlRearLeftLock();
            RearRightLockUnlockButton.Click += (sender, e) => ControlRearRightLock();
            RearLeftChildLockButton.Click += (sender, e) => ControlRearLeftChildLock();
            RearRightChildLockButton.Click += (sender, e) => ControlRearRightChildLock();

            // 自动化控制按钮事件
            DVTestButton.Click += (sender, e) => StartDVTest();
            HeatTestButton.Click += (sender, e) => StartHeatTest();
            EMCTestButton.Click += (sender, e) => StartEMCTest();

            // 参数设置按钮
            SetCycleCountButton.Click += (sender, e) => SetCycleCount();
            SetIntervalButton.Click += (sender, e) => SetDischargeToChargeInterval();


        }

        private void RefreshData(object? state)
        {
            // 实际数据刷新 - 数据通过ViewModel的SignalValues属性自动更新
            Dispatcher.Invoke(() =>
            {
                // 检查CAN连接状态
                CheckCANConnection();
            });
        }



        private void CheckCANConnection()
        {
            // 检查实际的CAN连接状态
            if (_canCommunication != null)
            {
                _canConnected = _canCommunication.IsOpen && _canCommunication.IsStart;
            }
        }

        private void ReadFaultCodes()
        {
            // 实际读取故障码
            if (_canCommunication != null)
            {
                // 发送读取故障码的CAN命令
                // 这里需要根据实际的CAN协议实现
                FaultCodesList.Items.Clear();
                // 实际项目中，这里会等待CAN响应并解析故障码
                FaultCodesList.Items.Add("读取中...");
            }
            else
            {
                FaultCodesList.Items.Clear();
                FaultCodesList.Items.Add("CAN未连接");
            }
        }



        private void ReadVersion()
        {
            // 实际读取版本号
            if (_canCommunication != null)
            {
                // 发送读取版本号的CAN命令（使用UDS协议）
                SoftwareVersion.Text = "读取中...";
                HardwareVersion.Text = "读取中...";
                
                // 调用CANCommunication的ReadVersion方法
                _canCommunication.ReadVersion(
                    softwareVersion => {
                        Dispatcher.Invoke(() => {
                            SoftwareVersion.Text = softwareVersion;
                        });
                    },
                    hardwareVersion => {
                        Dispatcher.Invoke(() => {
                            HardwareVersion.Text = hardwareVersion;
                        });
                    }
                );
            }
            else
            {
                SoftwareVersion.Text = "CAN未连接";
                HardwareVersion.Text = "CAN未连接";
            }
        }

        private void EnterSleep()
        {
            // 实际进入休眠
            if (_canCommunication != null)
            {
                // 发送进入休眠的CAN命令
                _canCommunication.SendControlCommand(
                    chargeCmd: false, dischargeCmd: false, sleepCmd: true,
                    cycleSetting: _storedCycleCount, delay: _storedDelay, operatingConditions: 0,
                    doorLFCmd: false, doorRFCmd: false, doorLRCmd: false, doorRRCmd: false,
                    doorLRChild: false, doorRRChild: false
                );
            }
        }

        private void ExitSleep()
        {
            // 实际退出休眠
            if (_canCommunication != null)
            {
                // 发送退出休眠的CAN命令
                _canCommunication.SendControlCommand(
                    chargeCmd: false, dischargeCmd: false, sleepCmd: false,
                    cycleSetting: _storedCycleCount, delay: _storedDelay, operatingConditions: 0,
                    doorLFCmd: false, doorRFCmd: false, doorLRCmd: false, doorRRCmd: false,
                    doorLRChild: false, doorRRChild: false
                );
            }
        }

        private void CanWakeup()
        {
            // CAN唤醒功能：发送ID:0x400, data:00 00 00 00 00 00 00 00
            if (_canCommunication != null)
            {
                _canCommunication.SendCanWakeupMessage();
            }
        }

        private void StartCharging()
        {
            // 切换状态
            _chargingEnabled = !_chargingEnabled;
            
            // 如果激活充电，则禁用放电
            if (_chargingEnabled)
            {
                _dischargingEnabled = false;
                DischargeButton.Background = System.Windows.SystemColors.ControlBrush;
            }
            
            // 设置按钮颜色
            if (_chargingEnabled)
            {
                ChargeButton.Background = Brushes.Green;
            }
            else
            {
                ChargeButton.Background = System.Windows.SystemColors.ControlBrush;
            }
            
            // 实际开始充电
            if (_canCommunication != null)
            {
                // 发送开始充电的CAN命令
                _canCommunication.SendControlCommand(
                    chargeCmd: _chargingEnabled, dischargeCmd: false, sleepCmd: false,
                    cycleSetting: _storedCycleCount, delay: _storedDelay, operatingConditions: 0,
                    doorLFCmd: _frontLeftLockEnabled, doorRFCmd: _frontRightLockEnabled, doorLRCmd: _rearLeftLockEnabled, doorRRCmd: _rearRightLockEnabled,
                    doorLRChild: _rearLeftChildLockEnabled, doorRRChild: _rearRightChildLockEnabled
                );
            }
        }

        private void StartDischarging()
        {
            // 切换状态
            _dischargingEnabled = !_dischargingEnabled;
            
            // 如果激活放电，则禁用充电
            if (_dischargingEnabled)
            {
                _chargingEnabled = false;
                ChargeButton.Background = System.Windows.SystemColors.ControlBrush;
            }
            
            // 设置按钮颜色
            if (_dischargingEnabled)
            {
                DischargeButton.Background = Brushes.Green;
            }
            else
            {
                DischargeButton.Background = System.Windows.SystemColors.ControlBrush;
            }
            
            // 实际开始放电
            if (_canCommunication != null)
            {
                // 发送开始放电的CAN命令
                _canCommunication.SendControlCommand(
                    chargeCmd: false, dischargeCmd: _dischargingEnabled, sleepCmd: false,
                    cycleSetting: _storedCycleCount, delay: _storedDelay, operatingConditions: 0,
                    doorLFCmd: _frontLeftLockEnabled, doorRFCmd: _frontRightLockEnabled, doorLRCmd: _rearLeftLockEnabled, doorRRCmd: _rearRightLockEnabled,
                    doorLRChild: _rearLeftChildLockEnabled, doorRRChild: _rearRightChildLockEnabled
                );
            }
        }

        // 点控功能方法
        private bool IsAllDoorLocksDisabled()
        {
            return !_frontLeftLockEnabled && !_frontRightLockEnabled && !_rearLeftLockEnabled && 
                   !_rearRightLockEnabled && !_rearLeftChildLockEnabled && !_rearRightChildLockEnabled;
        }
        
        private void ControlFrontLeftLock()
        {
            // 切换状态
            _frontLeftLockEnabled = !_frontLeftLockEnabled;
            
            // 设置按钮颜色
            if (_frontLeftLockEnabled)
            {
                FrontLeftLockUnlockButton.Background = Brushes.Green;
            }
            else
            {
                FrontLeftLockUnlockButton.Background = System.Windows.SystemColors.ControlBrush;
            }
            
            // 实际左前门锁控制操作
            if (_canCommunication != null)
            {
                // 检查是否所有点控按钮都不控制
                bool allDisabled = IsAllDoorLocksDisabled();
                
                // 发送左前门锁控制的CAN命令
                _canCommunication.SendControlCommand(
                    chargeCmd: false, dischargeCmd: false, sleepCmd: false,
                    cycleSetting: _storedCycleCount, delay: _storedDelay, operatingConditions: 7,
                    doorLFCmd: _frontLeftLockEnabled, doorRFCmd: _frontRightLockEnabled, doorLRCmd: _rearLeftLockEnabled, doorRRCmd: _rearRightLockEnabled,
                    doorLRChild: _rearLeftChildLockEnabled, doorRRChild: _rearRightChildLockEnabled
                );
                
                // 如果所有点控按钮都不控制，单独发送一次设置信号Cap_TestOperatingConditions为0

            }
        }

        private void ControlFrontRightLock()
        {
            // 切换状态
            _frontRightLockEnabled = !_frontRightLockEnabled;
            
            // 设置按钮颜色
            if (_frontRightLockEnabled)
            {
                FrontRightLockUnlockButton.Background = Brushes.Green;
            }
            else
            {
                FrontRightLockUnlockButton.Background = System.Windows.SystemColors.ControlBrush;
            }
            
            // 实际右前门锁控制操作
            if (_canCommunication != null)
            {
                // 检查是否所有点控按钮都不控制
                bool allDisabled = IsAllDoorLocksDisabled();
                
                // 发送右前门锁控制的CAN命令
                _canCommunication.SendControlCommand(
                    chargeCmd: false, dischargeCmd: false, sleepCmd: false,
                    cycleSetting: _storedCycleCount, delay: _storedDelay, operatingConditions:  7 ,
                    doorLFCmd: _frontLeftLockEnabled, doorRFCmd: _frontRightLockEnabled, doorLRCmd: _rearLeftLockEnabled, doorRRCmd: _rearRightLockEnabled,
                    doorLRChild: _rearLeftChildLockEnabled, doorRRChild: _rearRightChildLockEnabled
                );
                
                // 如果所有点控按钮都不控制，单独发送一次设置信号Cap_TestOperatingConditions为0

            }
        }

        private void ControlRearLeftLock()
        {
            // 切换状态
            _rearLeftLockEnabled = !_rearLeftLockEnabled;
            
            // 设置按钮颜色
            if (_rearLeftLockEnabled)
            {
                RearLeftLockUnlockButton.Background = Brushes.Green;
            }
            else
            {
                RearLeftLockUnlockButton.Background = System.Windows.SystemColors.ControlBrush;
            }
            
            // 实际左后门锁控制操作
            if (_canCommunication != null)
            {
                // 检查是否所有点控按钮都不控制
                bool allDisabled = IsAllDoorLocksDisabled();
                
                // 发送左后门锁控制的CAN命令
                _canCommunication.SendControlCommand(
                    chargeCmd: false, dischargeCmd: false, sleepCmd: false,
                    cycleSetting: _storedCycleCount, delay: _storedDelay, operatingConditions: 7,
                    doorLFCmd: _frontLeftLockEnabled, doorRFCmd: _frontRightLockEnabled, doorLRCmd: _rearLeftLockEnabled, doorRRCmd: _rearRightLockEnabled,
                    doorLRChild: _rearLeftChildLockEnabled, doorRRChild: _rearRightChildLockEnabled
                );
                
                // 如果所有点控按钮都不控制，单独发送一次设置信号Cap_TestOperatingConditions为0

            }
        }

        private void ControlRearRightLock()
        {
            // 切换状态
            _rearRightLockEnabled = !_rearRightLockEnabled;
            
            // 设置按钮颜色
            if (_rearRightLockEnabled)
            {
                RearRightLockUnlockButton.Background = Brushes.Green;
            }
            else
            {
                RearRightLockUnlockButton.Background = System.Windows.SystemColors.ControlBrush;
            }
            
            // 实际右后门锁控制操作
            if (_canCommunication != null)
            {
                // 检查是否所有点控按钮都不控制
                bool allDisabled = IsAllDoorLocksDisabled();
                
                // 发送右后门锁控制的CAN命令
                _canCommunication.SendControlCommand(
                    chargeCmd: false, dischargeCmd: false, sleepCmd: false,
                    cycleSetting: _storedCycleCount, delay: _storedDelay, operatingConditions: 7,
                    doorLFCmd: _frontLeftLockEnabled, doorRFCmd: _frontRightLockEnabled, doorLRCmd: _rearLeftLockEnabled, doorRRCmd: _rearRightLockEnabled,
                    doorLRChild: _rearLeftChildLockEnabled, doorRRChild: _rearRightChildLockEnabled
                );
                
                // 如果所有点控按钮都不控制，单独发送一次设置信号Cap_TestOperatingConditions为0

            }
        }

        private void ControlRearLeftChildLock()
        {
            // 切换状态
            _rearLeftChildLockEnabled = !_rearLeftChildLockEnabled;
            
            // 设置按钮颜色
            if (_rearLeftChildLockEnabled)
            {
                RearLeftChildLockButton.Background = Brushes.Green;
            }
            else
            {
                RearLeftChildLockButton.Background = System.Windows.SystemColors.ControlBrush;
            }
            
            // 实际左后门儿童锁控制操作
            if (_canCommunication != null)
            {
                // 检查是否所有点控按钮都不控制
                bool allDisabled = IsAllDoorLocksDisabled();
                
                // 发送左后门儿童锁控制的CAN命令
                _canCommunication.SendControlCommand(
                    chargeCmd: false, dischargeCmd: false, sleepCmd: false,
                    cycleSetting: _storedCycleCount, delay: _storedDelay, operatingConditions: 7,
                    doorLFCmd: _frontLeftLockEnabled, doorRFCmd: _frontRightLockEnabled, doorLRCmd: _rearLeftLockEnabled, doorRRCmd: _rearRightLockEnabled,
                    doorLRChild: _rearLeftChildLockEnabled, doorRRChild: _rearRightChildLockEnabled
                );
                
                // 如果所有点控按钮都不控制，单独发送一次设置信号Cap_TestOperatingConditions为0

            }
        }

        private void ControlRearRightChildLock()
        {
            // 切换状态
            _rearRightChildLockEnabled = !_rearRightChildLockEnabled;
            
            // 设置按钮颜色
            if (_rearRightChildLockEnabled)
            {
                RearRightChildLockButton.Background = Brushes.Green;
            }
            else
            {
                RearRightChildLockButton.Background = System.Windows.SystemColors.ControlBrush;
            }
            
            // 实际右后门儿童锁控制操作
            if (_canCommunication != null)
            {
                // 检查是否所有点控按钮都不控制
                bool allDisabled = IsAllDoorLocksDisabled();
                
                // 发送右后门儿童锁控制的CAN命令
                _canCommunication.SendControlCommand(
                    chargeCmd: false, dischargeCmd: false, sleepCmd: false,
                    cycleSetting: _storedCycleCount, delay: _storedDelay, operatingConditions: 7,
                    doorLFCmd: _frontLeftLockEnabled, doorRFCmd: _frontRightLockEnabled, doorLRCmd: _rearLeftLockEnabled, doorRRCmd: _rearRightLockEnabled,
                    doorLRChild: _rearLeftChildLockEnabled, doorRRChild: _rearRightChildLockEnabled
                );
                
                // 如果所有点控按钮都不控制，单独发送一次设置信号Cap_TestOperatingConditions为0

            }
        }

        // 自动化控制方法
        private void StartDVTest()
        {
            // 切换状态
            _dvTestEnabled = !_dvTestEnabled;
            
            // 如果激活DV测试，则禁用其他测试
            if (_dvTestEnabled)
            {
                _heatTestEnabled = false;
                _emcTestEnabled = false;
                
                // 更新其他按钮状态
                HeatTestButton.Background = System.Windows.SystemColors.ControlBrush;
                HeatTestStatus.Text = "停止";
                HeatTestStatus.Foreground = Brushes.Green;
                
                EMCTestButton.Background = System.Windows.SystemColors.ControlBrush;
                EMCTestStatus.Text = "停止";
                EMCTestStatus.Foreground = Brushes.Green;
            }
            
            // 设置当前按钮颜色
            if (_dvTestEnabled)
            {
                DVTestButton.Background = Brushes.Green;
                DVTestStatus.Text = "执行中";
                DVTestStatus.Foreground = Brushes.Red;
            }
            else
            {
                DVTestButton.Background = System.Windows.SystemColors.ControlBrush;
                DVTestStatus.Text = "停止";
                DVTestStatus.Foreground = Brushes.Green;
            }
            
            // 实际DV环境工况操作
            if (_canCommunication != null)
            {
                // 发送DV环境工况的CAN命令 (DV标准工况 - 1，停止为0)
                _canCommunication.SendControlCommand(
                    chargeCmd: false, dischargeCmd: false, sleepCmd: false,
                    cycleSetting: _storedCycleCount, delay: _storedDelay, operatingConditions: (byte)(_dvTestEnabled ? 1 : 0),
                    doorLFCmd: _frontLeftLockEnabled, doorRFCmd: _frontRightLockEnabled, doorLRCmd: _rearLeftLockEnabled, doorRRCmd: _rearRightLockEnabled,
                    doorLRChild: _rearLeftChildLockEnabled, doorRRChild: _rearRightChildLockEnabled
                );
            }
        }

        private void StartHeatTest()
        {
            // 切换状态
            _heatTestEnabled = !_heatTestEnabled;
            
            // 如果激活热测试，则禁用其他测试
            if (_heatTestEnabled)
            {
                _dvTestEnabled = false;
                _emcTestEnabled = false;
                
                // 更新其他按钮状态
                DVTestButton.Background = System.Windows.SystemColors.ControlBrush;
                DVTestStatus.Text = "停止";
                DVTestStatus.Foreground = Brushes.Green;
                
                EMCTestButton.Background = System.Windows.SystemColors.ControlBrush;
                EMCTestStatus.Text = "停止";
                EMCTestStatus.Foreground = Brushes.Green;
            }
            
            // 设置当前按钮颜色
            if (_heatTestEnabled)
            {
                HeatTestButton.Background = Brushes.Green;
                HeatTestStatus.Text = "执行中";
                HeatTestStatus.Foreground = Brushes.Red;
            }
            else
            {
                HeatTestButton.Background = System.Windows.SystemColors.ControlBrush;
                HeatTestStatus.Text = "停止";
                HeatTestStatus.Foreground = Brushes.Green;
            }
            
            // 实际DV热测试工况操作
            if (_canCommunication != null)
            {
                // 发送DV热测试工况的CAN命令 (DV热工况 - 2，停止为0)
                _canCommunication.SendControlCommand(
                    chargeCmd: false, dischargeCmd: false, sleepCmd: false,
                    cycleSetting: _storedCycleCount, delay: _storedDelay, operatingConditions: (byte)(_heatTestEnabled ? 2 : 0),
                    doorLFCmd: _frontLeftLockEnabled, doorRFCmd: _frontRightLockEnabled, doorLRCmd: _rearLeftLockEnabled, doorRRCmd: _rearRightLockEnabled,
                    doorLRChild: _rearLeftChildLockEnabled, doorRRChild: _rearRightChildLockEnabled
                );
            }
        }

        private void StartEMCTest()
        {
            // 切换状态
            _emcTestEnabled = !_emcTestEnabled;
            
            // 如果激活EMC测试，则禁用其他测试
            if (_emcTestEnabled)
            {
                _dvTestEnabled = false;
                _heatTestEnabled = false;
                
                // 更新其他按钮状态
                DVTestButton.Background = System.Windows.SystemColors.ControlBrush;
                DVTestStatus.Text = "停止";
                DVTestStatus.Foreground = Brushes.Green;
                
                HeatTestButton.Background = System.Windows.SystemColors.ControlBrush;
                HeatTestStatus.Text = "停止";
                HeatTestStatus.Foreground = Brushes.Green;
            }
            
            // 设置当前按钮颜色
            if (_emcTestEnabled)
            {
                EMCTestButton.Background = Brushes.Green;
                EMCTestStatus.Text = "执行中";
                EMCTestStatus.Foreground = Brushes.Red;
            }
            else
            {
                EMCTestButton.Background = System.Windows.SystemColors.ControlBrush;
                EMCTestStatus.Text = "停止";
                EMCTestStatus.Foreground = Brushes.Green;
            }
            
            // 实际EMC试验工况操作
            if (_canCommunication != null)
            {
                // 发送EMC试验工况的CAN命令 (EMC试验工况 - 3，停止为0)
                _canCommunication.SendControlCommand(
                    chargeCmd: false, dischargeCmd: false, sleepCmd: false,
                    cycleSetting: _storedCycleCount, delay: _storedDelay, operatingConditions: (byte)(_emcTestEnabled ? 3 : 0),
                    doorLFCmd: _frontLeftLockEnabled, doorRFCmd: _frontRightLockEnabled, doorLRCmd: _rearLeftLockEnabled, doorRRCmd: _rearRightChildLockEnabled,
                    doorLRChild: _rearLeftChildLockEnabled, doorRRChild: _rearRightChildLockEnabled
                );
            }
        }

        // 获取当前工况的operatingConditions值
        private byte GetCurrentOperatingConditions()
        {
            // 优先检查自动化测试工况
            if (_dvTestEnabled)
                return 1; // DV标准工况
            if (_heatTestEnabled)
                return 2; // DV热工况
            if (_emcTestEnabled)
                return 3; // EMC试验工况
            
            // 检查点控功能
            if (_frontLeftLockEnabled || _frontRightLockEnabled || _rearLeftLockEnabled || 
                _rearRightLockEnabled || _rearLeftChildLockEnabled || _rearRightChildLockEnabled)
            {
                return 7; // 点控工况
            }
            
            // 默认工况
            return 0;
        }

        // 参数设置方法
        private void SetCycleCount()
        {
            // 解析总循环次数并存储
            if (ushort.TryParse(TotalCycleCount.Text, out ushort cycleCount))
            {
                _storedCycleCount = cycleCount;
            }
        }

        private void SetDischargeToChargeInterval()
        {
            // 解析循环间隔时间（单位：ms）并存储
            if (int.TryParse(CycleIntervalTime.Text, out int intervalMs) && intervalMs >= 0)
            {
                // 转换为delay值：如果输入0则发送0（系统默认10s），否则 delay = 输入值 / 5
                // 例如：输入1000ms -> delay = 200（表示1s延时）
                _storedDelay = (ushort)(intervalMs == 0 ? 0 : intervalMs / 5);
            }
        }

        // CAN监控方法
        private void ClearCANFrames()
        {
            // 清空CAN帧列表
            CANFrames.Clear();
            
            // 重置计数
            _sentFrameCount = 0;
            _receivedFrameCount = 0;
            _errorFrameCount = 0;
        }

        // 用于存储待处理的CAN报文，设置容量限制为10000，防止内存溢出
        private System.Collections.Concurrent.BlockingCollection<CANMessage> _messageQueue = new System.Collections.Concurrent.BlockingCollection<CANMessage>(10000);
        private System.Threading.CancellationTokenSource _cts = new System.Threading.CancellationTokenSource();
        
        // 用于批量更新的集合
        private System.Collections.Generic.List<CANFrame> _batchFrames = new System.Collections.Generic.List<CANFrame>();
        private int _batchSize = 50; // 每批处理的报文数量
        private readonly object _batchLock = new object();
        private bool _isUpdating = false;
        
        // 用于标记是否需要滚动
        private bool _needsScroll = false;
        // 用于标记是否暂停
        private bool _isPaused = false;
        
        // CSV文件保存相关字段
        private string _logsDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private string _currentCsvFilePath;
        private StreamWriter _csvWriter;
        private DateTime _lastFileCreationTime;
        private readonly TimeSpan _fileSplitInterval = TimeSpan.FromMinutes(10);

        private void InitializeCANCommunication()
        {
            if (_canCommunication != null)
            {
                // 初始化信号解析
                _canCommunication.InitializeSignalParsing();
                // 订阅CAN报文接收事件
                _canCommunication.OnMessageReceived += OnCANMessageReceived;
                _canCommunication.OnCANFrameSent += OnCANFrameSent;
                _canCommunication.OnSignalValueUpdated += OnSignalValueUpdated;
                _canCommunication.OnError += OnCANError;
                _canCommunication.OnConnectionStatusChanged += OnCANConnectionStatusChanged;
                // _canCommunication.OnFaultDetected += OnFaultDetected;  // 禁用：避免双重记录，现在用CheckAllFaultsAndLog统一处理
                
                // 启动报文处理线程
                StartMessageProcessingThread();
            }
        }
        
        private void InitializeCsvFile()
        {
            // 确保日志目录存在
            EnsureLogsDirectoryExists();
            
            // 创建新的CSV文件
            CreateNewCsvFile();
        }
        
        private void EnsureLogsDirectoryExists()
        {
            // 创建主日志目录
            if (!Directory.Exists(_logsDirectory))
            {
                Directory.CreateDirectory(_logsDirectory);
            }
            
            // 创建当天日期的子目录
            string todayDirectory = GetTodayDirectory();
            if (!Directory.Exists(todayDirectory))
            {
                Directory.CreateDirectory(todayDirectory);
            }
        }
        
        private string GetTodayDirectory()
        {
            string todayDate = DateTime.Now.ToString("yyyy-MM-dd");
            return System.IO.Path.Combine(_logsDirectory, todayDate);
        }
        
        private void CreateNewCsvFile()
        {
            lock (_csvLock)
            {
                // 关闭当前的CSV writer
                CloseCsvWriter();
                
                // 获取当天目录
                string todayDirectory = GetTodayDirectory();
                
                // 生成文件名（使用当前时间）
                string fileName = DateTime.Now.ToString("HH-mm-ss") + ".csv";
                _currentCsvFilePath = System.IO.Path.Combine(todayDirectory, fileName);
                
                // 创建新的CSV文件并写入表头
                _csvWriter = new StreamWriter(_currentCsvFilePath);
                _csvWriter.WriteLine("Time,Direction,FrameId,FrameType,ProtocolType,DataLength,Data,Status");
                
                // 更新最后文件创建时间
                _lastFileCreationTime = DateTime.Now;
            }
        }
        
        private void CloseCsvWriter()
        {
            _csvWritingEnabled = false;
            
            if (_csvWriteTask != null)
            {
                _csvWriteTask.Wait(3000);
            }

            lock (_csvLock)
            {
                if (_csvWriter != null)
                {
                    _csvWriter.Flush();
                    _csvWriter.Dispose();
                    _csvWriter = null;
                }
            }
        }
        
        private void CheckCsvFileSplit()
        {
            // 检查是否需要创建新的CSV文件
            if (DateTime.Now - _lastFileCreationTime >= _fileSplitInterval)
            {
                CreateNewCsvFile();
            }
        }
        
        private object _csvLock = new object();

        private readonly ConcurrentQueue<CANFrame> _csvWriteQueue = new ConcurrentQueue<CANFrame>();
        private Task? _csvWriteTask;
        private bool _csvWritingEnabled = true;

        private void StartCsvWriterTask()
        {
            if (_csvWriteTask != null && !_csvWriteTask.IsCompleted)
                return;

            _csvWriteTask = Task.Run(async () =>
            {
                while (_csvWritingEnabled || _csvWriteQueue.Count > 0)
                {
                    if (_csvWriteQueue.TryDequeue(out CANFrame? frame))
                    {
                        WriteCanFrameToCsvSync(frame);
                    }
                    else
                    {
                        await Task.Delay(10);
                    }
                }
            });
        }

        private void WriteCanFrameToCsvSync(CANFrame canFrame)
        {
            lock (_csvLock)
            {
                if (_csvWriter == null)
                {
                    EnsureLogsDirectoryExists();
                    CreateNewCsvFile();
                }
                else
                {
                    CheckCsvFileSplit();
                }

                _csvWriter?.WriteLine($"{canFrame.Time},{canFrame.Direction},{canFrame.FrameId},{canFrame.FrameType},{canFrame.ProtocolType},{canFrame.DataLength},{canFrame.Data},{canFrame.Status}");
            }
        }

        private void WriteCanFrameToCsv(CANFrame canFrame)
        {
            _csvWriteQueue.Enqueue(canFrame);
            StartCsvWriterTask();
        }

        private bool _pendingUIUpdate = false;

        private void TriggerUIUpdate()
        {
            lock (_batchLock)
            {
                if (_isUpdating)
                {
                    _pendingUIUpdate = true;
                    return;
                }
                _isUpdating = true;
                _pendingUIUpdate = false;
            }
            
            Dispatcher.BeginInvoke(new Action(() => {
                try
                {
                    UpdateUIInternal();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UI更新异常: {ex.Message}");
                }
                finally
                {
                    lock (_batchLock)
                    {
                        _isUpdating = false;
                        if (_pendingUIUpdate)
                        {
                            _pendingUIUpdate = false;
                            Dispatcher.BeginInvoke(new Action(TriggerUIUpdate), System.Windows.Threading.DispatcherPriority.Background);
                        }
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void OnCANMessageReceived(CANMessage message)
        {
            // 将接收到的报文加入队列，由后台线程处理
            message.IsTransmit = false;
            // 使用TryAdd避免队列满时阻塞接收线程
            if (!_messageQueue.TryAdd(message))
            {
                // 队列已满，记录日志
                System.Diagnostics.Debug.WriteLine("报文队列已满，丢弃报文");
            }
        }

        private void OnCANFrameSent(CANMessage message)
        {
            // 将发送的报文加入队列，由后台线程处理
            message.IsTransmit = true;
            // 使用TryAdd避免队列满时阻塞发送线程
            if (!_messageQueue.TryAdd(message))
            {
                // 队列已满，记录日志
                System.Diagnostics.Debug.WriteLine("报文队列已满，丢弃报文");
            }
        }

        private void ProcessCANMessage(CANMessage message)
        {
            try
            {
                // 在后台线程中处理CAN报文
                // 增加计数
                if (message.IsTransmit)
                {
                    _sentFrameCount++;
                }
                else
                {
                    _receivedFrameCount++;
                }
                
                // 创建新的CANFrame对象
                var canFrame = new CANFrame
                {
                    Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    Direction = message.IsTransmit ? "发送" : "接收",
                    FrameId = message.ID.ToString("X"),
                    FrameType = message.FrameType == CANFrameType.Extended ? "扩展帧" : "标准帧",
                    ProtocolType = message.IsCANFD ? "CAN FD" : "CAN",
                    DataLength = message.DataLength.ToString(),
                    Data = BitConverter.ToString(message.Data).Replace("-", " "),
                    Status = "正常"
                };

                // 将报文写入CSV文件
                WriteCanFrameToCsv(canFrame);

                // 将报文加入批量更新集合
                lock (_batchFrames)
                {
                    _batchFrames.Add(canFrame);
                }
            }
            catch (Exception ex)
            {
                // 记录异常，确保线程不会崩溃
                System.Diagnostics.Debug.WriteLine($"处理报文异常: {ex.Message}");
                _errorFrameCount++;
            }
        }

        private void UpdateUIInternal()
        {
            System.Collections.Generic.List<CANFrame> framesToAdd;
            int currentSentCount, currentReceivedCount, currentErrorCount;
            int batchCount;
            
            // 锁定批量集合，获取需要更新的数据
            lock (_batchFrames)
            {
                batchCount = _batchFrames.Count;
                if (batchCount == 0)
                    return;
                    
                // 限制批量大小，避免一次性处理过多数据
                int maxBatchSize = 100;
                int processCount = Math.Min(batchCount, maxBatchSize);
                framesToAdd = new System.Collections.Generic.List<CANFrame>(_batchFrames.GetRange(0, processCount));
                _batchFrames.RemoveRange(0, processCount);
                currentSentCount = _sentFrameCount;
                currentReceivedCount = _receivedFrameCount;
                currentErrorCount = _errorFrameCount;
            }
            
            if (framesToAdd.Count == 0)
                return;
            
            // 更新计数显示
            SentFrameCountText.Text = currentSentCount.ToString();
            ReceivedFrameCountText.Text = currentReceivedCount.ToString();
            ErrorFrameCountText.Text = currentErrorCount.ToString();
            
            // 批量添加到集合
            foreach (var frame in framesToAdd)
            {
                CANFrames.Add(frame);
            }

            // 确保集合不超过1000条
            while (CANFrames.Count > 1000)
            {
                CANFrames.RemoveAt(0);
            }
        }

        // 信号值更新批处理集合
        private Dictionary<string, double> _signalValueBatch = new Dictionary<string, double>();
        private object _signalValueLock = new object();
        private DateTime _lastSignalUpdate = DateTime.Now;

        private void OnSignalValueUpdated(string signalKey, double value)
        {
            // 将信号值添加到批处理集合
            lock (_signalValueLock)
            {
                _signalValueBatch[signalKey] = value;
            }

            // 每100ms或累积100个信号值时批量更新UI
            DateTime now = DateTime.Now;
            if (_signalValueBatch.Count >= 100 || (now - _lastSignalUpdate).TotalMilliseconds >= 100)
            {
                ProcessSignalValueBatch();
            }
        }

        private void ProcessSignalValueBatch()
        {
            Dictionary<string, double> batch;
            lock (_signalValueLock)
            {
                if (_signalValueBatch.Count == 0)
                    return;
                batch = new Dictionary<string, double>(_signalValueBatch);
                _signalValueBatch.Clear();
                _lastSignalUpdate = DateTime.Now;
            }

            // 批量更新UI
            Dispatcher.BeginInvoke(new Action(() => {
                foreach (var kvp in batch)
                {
                    _mainViewModel.UpdateSignalValue(kvp.Key, kvp.Value);
                    
                    // 处理测试状态信号
                            switch (kvp.Key)
                            {
                                case "SuperCapController_Cap_EMCStatus":
                                    // EMC试验工况状态显示，1-正在测试；0-停止测试
                                    if (kvp.Value == 1)
                                    {
                                        EMCTestStatus.Text = "执行中";
                                        EMCTestStatus.Foreground = System.Windows.Media.Brushes.Red;
                                    }
                                    else
                                    {
                                        EMCTestStatus.Text = "停止";
                                        EMCTestStatus.Foreground = System.Windows.Media.Brushes.Green;
                                    }
                                    break;
                                case "SuperCapController_Cap_DVHotStatus":
                                    // DV热试验工况状态显示，1-正在测试；0-停止测试
                                    if (kvp.Value == 1)
                                    {
                                        HeatTestStatus.Text = "执行中";
                                        HeatTestStatus.Foreground = System.Windows.Media.Brushes.Red;
                                    }
                                    else
                                    {
                                        HeatTestStatus.Text = "停止";
                                        HeatTestStatus.Foreground = System.Windows.Media.Brushes.Green;
                                    }
                                    break;
                                case "SuperCapController_Cap_DVNormalStatus":
                                    // DV试验工况状态显示，1-正在测试；0-停止测试
                                    if (kvp.Value == 1)
                                    {
                                        DVTestStatus.Text = "执行中";
                                        DVTestStatus.Foreground = System.Windows.Media.Brushes.Red;
                                    }
                                    else
                                    {
                                        DVTestStatus.Text = "停止";
                                        DVTestStatus.Foreground = System.Windows.Media.Brushes.Green;
                                    }
                                    break;
                                case "SuperCapController_Cap_CycleTestReport":
                                    // 循环次数计数，16位无符号整数
                                    CurrentCycleCount.Text = kvp.Value.ToString();
                                    break;
                                case "SuperCapController_Cap_SleepStatus":
                                    // 休眠状态显示，1-休眠状态；0-不休眠
                                    if (kvp.Value == 1)
                                    {
                                        SleepWakeStatus.Text = "当前状态: 休眠";
                                        SleepWakeStatus.Foreground = System.Windows.Media.Brushes.Red;
                                    }
                                    else
                                    {
                                        SleepWakeStatus.Text = "当前状态: 唤醒";
                                        SleepWakeStatus.Foreground = System.Windows.Media.Brushes.Green;
                                    }
                                    break;
                                case "SuperCapController_Cap_ModuleTempreture":
                                    if (kvp.Value > 85)
                                    {
                                        _mainViewModel.UpdateSignalValue("SuperCapController_Cap_OverTempretureState", 1);
                                    }
                                    else
                                    {
                                        _mainViewModel.UpdateSignalValue("SuperCapController_Cap_OverTempretureState", 0);
                                    }
                                    break;
                                case "SuperCapController_Cap_InternalResistance":
                                    if (kvp.Value < 5 || kvp.Value > 6)
                                    {
                                        _mainViewModel.UpdateSignalValue("SuperCapController_Cap_InternalResistanceStatus", 1);
                                    }
                                    else
                                    {
                                        _mainViewModel.UpdateSignalValue("SuperCapController_Cap_InternalResistanceStatus", 0);
                                    }
                                    break;
                            }
                }
                
                _mainViewModel.CheckAllFaultsAndLog();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private int _errorCount = 0;
        private DateTime _lastErrorTime = DateTime.MinValue;
        private const int ERROR_THRESHOLD = 3; // 错误阈值
        private const int ERROR_WINDOW_SECONDS = 5; // 错误窗口时间（秒）

        private void OnCANError(string error)
        {
            // 处理CAN错误
            DateTime now = DateTime.Now;
            
            // 重置错误计数如果超过时间窗口
            if ((now - _lastErrorTime).TotalSeconds > ERROR_WINDOW_SECONDS)
            {
                _errorCount = 0;
            }
            
            _errorCount++;
            _lastErrorTime = now;
            
            // 只有当错误超过阈值时才弹出弹窗
            if (_errorCount >= ERROR_THRESHOLD)
            {
                Dispatcher.Invoke(() => {
                    System.Windows.MessageBox.Show("CAN错误: " + error, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    // 重置错误计数
                    _errorCount = 0;
                });
            }
            else
            {
                // 记录错误但不弹出弹窗
                System.Diagnostics.Debug.WriteLine($"CAN错误: {error}");
            }
        }

        private void OnCANConnectionStatusChanged(bool connected)
        {
            // 处理CAN连接状态变化
            _canConnected = connected;
            Dispatcher.Invoke(() => {
                if (connected)
                {
                    // 连接成功
                }
                else
                {
                    // 连接断开
                    // 记录CAN连接故障
                    _faultManager.RecordFault("CAN通讯", "连接断开", "");
                }
            });
        }

        private void OnFaultDetected(string location, string type, string value)
        {
            // 处理故障检测事件
            Dispatcher.Invoke(() => {
                // 记录故障信息
                _faultManager.RecordFault(location, type, value);
            });
        }



        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // 取消后台线程
            _cts?.Cancel();
            
            // 等待后台线程结束
            _messageProcessingTask?.Wait(1000);
            
            // 释放定时器
            _dataRefreshTimer?.Dispose();
            
            // 释放CAN通信
            _canCommunication?.Dispose();
            
            // 关闭CSV写入器
            CloseCsvWriter();
        }
        
        private Task? _messageProcessingTask;
        
        private void StartMessageProcessingThread()
        {
            // 创建一个后台线程来处理CAN报文和UI更新
            _messageProcessingTask = Task.Run(() => {
                try
                {
                    int processedCount = 0;
                    DateTime lastUIUpdate = DateTime.Now;
                    int dynamicBatchSize = _batchSize;
                    
                    foreach (var message in _messageQueue.GetConsumingEnumerable(_cts.Token))
                    {
                        if (_cts.Token.IsCancellationRequested)
                            break;
                            
                        ProcessCANMessage(message);
                        processedCount++;
                        
                        if (_messageQueue.Count > 100)
                        {
                            dynamicBatchSize = Math.Min(100, _batchSize * 2);
                        }
                        else
                        {
                            dynamicBatchSize = _batchSize;
                        }
                        
                        DateTime now = DateTime.Now;
                        if (processedCount >= dynamicBatchSize || 
                            _messageQueue.Count >= dynamicBatchSize || 
                            (now - lastUIUpdate).TotalMilliseconds >= 100)
                        {
                            TriggerUIUpdate();
                            processedCount = 0;
                            lastUIUpdate = now;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // 线程被取消，正常退出
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"报文处理线程异常: {ex.Message}");
                }
            }, _cts.Token);
        }
    }
}