﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using MHC;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZLGAPI;
using static ZLGAPI.ZDBC;
using static ZLGAPI.ZLGCAN;

namespace MHC
{
    public enum CANDeviceType
    {
        USBCAN1 = 3,
        USBCAN2 = 4,
        USBCAN_E_U = 20,
        USBCAN_2E_U = 21,
        USBCAN_4E_U = 31,
        USBCAN_8E_U = 34,
        USBCANFD_200U = 41,
        USBCANFD_100U = 42,
        USBCANFD_MINI = 43,
        USBCANFD_400U = 76,
        USBCANFD_800U = 59,
        USBCANFD_800H = 85,
        PCIE_CANFD_200U = 39,
        PCIE_CANFD_400U = 40,
        PCIE_CANFD_100U_EX = 60,
        PCIE_CANFD_400U_EX = 61,
        PCIE_CANFD_200U_MINI = 62,
        PCIE_CANFD_200U_EX = 63,
        PCIE_CANFD_800U = 82,
        PCIE_CANFD_1200U = 83,
        CANET_TCP = 17,
        CANET_UDP = 12,
        CANFDNET_200U_TCP = 48,
        CANFDNET_200U_UDP = 49,
        CANFDNET_400U_TCP = 52,
        CANFDNET_400U_UDP = 53,
        CANFDNET_800U_TCP = 57,
        CANFDNET_800U_UDP = 58,
        CANFDWIFI_100U_TCP = 50,
        CANFDWIFI_100U_UDP = 51,
        CANFDWIFI_200U_TCP = 66,
        CANFDWIFI_200U_UDP = 67
    }

    public enum CANWorkMode
    {
        Normal = 0,
        ListenOnly = 1
    }

    public enum CANFrameType
    {
        Standard = 0,
        Extended = 1
    }

    public class CANMessage
    {
        public uint ID { get; set; }
        public CANFrameType FrameType { get; set; }
        public byte[] Data { get; set; }
        public byte DataLength => (byte)(Data?.Length ?? 0);
        public ulong Timestamp { get; set; }
        public bool IsTransmit { get; set; }
        public bool IsCANFD { get; set; }
        public string ProtocolType => IsCANFD ? "CAN FD" : "CAN";
        public string Direction => IsTransmit ? "发送" : "接收";
        public string FrameTypeString => FrameType == CANFrameType.Extended ? "扩展帧" : "标准帧";
        public string DataString => Data != null ? BitConverter.ToString(Data).Replace("-", " ") : "";
        public string Status { get; set; } = "正常";
        public DateTime Time { get; set; }

        public CANMessage()
        {
            Data = new byte[8];
            Time = DateTime.Now;
        }
    }

    public class SignalUpdate
    {
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
    }

    public class CANCommunication : IDisposable
    {
        private IntPtr _deviceHandle = IntPtr.Zero;
        private IntPtr _channelHandle = IntPtr.Zero;
        private uint _deviceType;
        private uint _deviceIndex;
        private uint _channelIndex;
        private bool _isOpen = false;
        private bool _isStart = false;
        
        private CancellationTokenSource? _cts;
        private Task? _receiveTask;
        private Task? _processTask;
        private Task? _uiUpdateTask;
        
        private readonly ConcurrentQueue<CANMessage> _messageQueue = new ConcurrentQueue<CANMessage>();
        private readonly ConcurrentQueue<SignalUpdate> _signalUpdateQueue = new ConcurrentQueue<SignalUpdate>();
        
        private long _messageCount = 0;
        private long _lastSecondTicks = 0;
        private const int MaxMessagesPerSecond = 1000;
        private const int MaxQueueSize = 10000;  // 队列最大容量
        private DateTime _startTime;
        private const int WarmupPeriodSeconds = 3;  // 启动预热期（秒）
        private const int ReceiveIntervalMs = 10;  // 接收间隔
        private const int ProcessIntervalMs = 10;  // 处理间隔
        private const int UIUpdateIntervalMs = 50;  // UI更新间隔
        
        private string _softwareVersionBuffer = "";
        private string _hardwareVersionBuffer = "";
        private Action<string>? _softwareVersionCallback;
        private Action<string>? _hardwareVersionCallback;

        public event Action<CANMessage>? OnMessageReceived;
        public event Action<CANMessage>? OnCANFrameSent;
        public event Action<string, double>? OnSignalValueUpdated;
        public event Action<bool>? OnConnectionStatusChanged;
        public event Action<string>? OnError;
        public event Action<string, string, string>? OnFaultDetected;

        public bool IsOpen => _isOpen;
        public bool IsStart => _isStart;
        public string DeviceTypeName { get; private set; } = "";

        public CANCommunication()
        {
        }

        public void InitializeSignalParsing()
        {
        }

        public bool OpenDevice(CANDeviceType deviceType, uint deviceIndex = 0)
        {
            try
            {
                if (_isOpen)
                {
                    CloseDevice();
                }

                _deviceType = (uint)deviceType;
                _deviceIndex = deviceIndex;
                DeviceTypeName = GetDeviceTypeName(deviceType);

                _deviceHandle = ZCAN_OpenDevice(_deviceType, _deviceIndex, 0);
                if (_deviceHandle == IntPtr.Zero)
                {
                    OnError?.Invoke($"打开设备失败: {DeviceTypeName}");
                    return false;
                }

                _isOpen = true;
                OnConnectionStatusChanged?.Invoke(true);
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"打开设备异常: {ex.Message}");
                return false;
            }
        }

        public bool InitChannel(uint channelIndex, uint baudRate, CANWorkMode mode = CANWorkMode.Normal, bool isCANFD = false)
        {
            try
            {
                if (!_isOpen)
                {
                    OnError?.Invoke("设备未打开");
                    return false;
                }

                _channelIndex = channelIndex;
                bool isFD = isCANFD || _deviceType == (uint)CANDeviceType.USBCANFD_200U || _deviceType == (uint)CANDeviceType.USBCANFD_100U;

                if (isFD)
                {
                    string canFdArbitrationBitRatePath = string.Format("{0}/canfd_abit_baud_rate", channelIndex);
                    uint setArbitrationBaudRateResult = ZCAN_SetValue(_deviceHandle, canFdArbitrationBitRatePath, (baudRate).ToString());
                    if (setArbitrationBaudRateResult != 1)
                    {
                        OnError?.Invoke("设置CAN FD仲裁相位波特率失败");
                        return false;
                    }

                    string canFdDataBitRatePath = string.Format("{0}/canfd_dbit_baud_rate", channelIndex);
                    uint setDataBaudRateResult = ZCAN_SetValue(_deviceHandle, canFdDataBitRatePath, (2000000).ToString());
                    if (setDataBaudRateResult != 1)
                    {
                        OnError?.Invoke("设置CAN FD数据相位波特率失败");
                        return false;
                    }

                    string resistancePath = string.Format("{0}/initenal_resistance", channelIndex);
                    uint setResistanceResult = ZCAN_SetValue(_deviceHandle, resistancePath, "1");
                    if (setResistanceResult != 1)
                    {
                        OnError?.Invoke("设置终端电阻失败");
                        return false;
                    }

                    ZCAN_CHANNEL_INIT_CONFIG config = new ZCAN_CHANNEL_INIT_CONFIG();
                    config.can_type = 1U;
                    config.config.canfd.mode = (byte)mode;

                    _channelHandle = ZCAN_InitCAN(_deviceHandle, _channelIndex, ref config);
                    if (_channelHandle == IntPtr.Zero)
                    {
                        OnError?.Invoke("初始化CAN FD通道失败");
                        return false;
                    }
                }
                else
                {
                    string canBitRatePath = string.Format("{0}/baud_rate", channelIndex);
                    uint setBaudRateResult = ZCAN_SetValue(_deviceHandle, canBitRatePath, (baudRate * 1000).ToString());
                    if (setBaudRateResult != 1)
                    {
                        OnError?.Invoke("设置CAN波特率失败");
                        return false;
                    }

                    ZCAN_CHANNEL_INIT_CONFIG config = new ZCAN_CHANNEL_INIT_CONFIG();
                    config.can_type = 0U;
                    config.config.can.acc_code = 0;
                    config.config.can.acc_mask = 0xFFFFFFFF;
                    config.config.can.reserved = 0;
                    config.config.can.filter = 0;
                    config.config.can.timing0 = GetTiming0(baudRate);
                    config.config.can.timing1 = GetTiming1(baudRate);
                    config.config.can.mode = (byte)mode;

                    _channelHandle = ZCAN_InitCAN(_deviceHandle, _channelIndex, ref config);
                    if (_channelHandle == IntPtr.Zero)
                    {
                        OnError?.Invoke("初始化CAN通道失败");
                        return false;
                    }
                }

                uint clearResult = ZCAN_ClearBuffer(_channelHandle);
                if (clearResult != 1)
                {
                    OnError?.Invoke("清除缓冲区失败");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"初始化通道异常: {ex.Message}");
                return false;
            }
        }

        public bool StartCAN()
        {
            try
            {
                if (_channelHandle == IntPtr.Zero)
                {
                    OnError?.Invoke("通道未初始化");
                    return false;
                }

                uint result = ZCAN_StartCAN(_channelHandle);
                if (result != 1)
                {
                    OnError?.Invoke("启动CAN失败");
                    return false;
                }

                _isStart = true;
                _startTime = DateTime.Now;
                _cts = new CancellationTokenSource();

                _receiveTask = Task.Run(ReceiveLoop, _cts.Token);
                _processTask = Task.Run(ProcessLoop, _cts.Token);
                _uiUpdateTask = Task.Run(UIUpdateLoop, _cts.Token);

                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"启动CAN异常: {ex.Message}");
                return false;
            }
        }

        public bool StopCAN()
        {
            try
            {
                _isStart = false;

                _cts?.Cancel();

                try
                {
                    if (_receiveTask != null && !_receiveTask.IsCompleted)
                        _receiveTask.Wait(1000);
                }
                catch { }

                try
                {
                    if (_processTask != null && !_processTask.IsCompleted)
                        _processTask.Wait(1000);
                }
                catch { }

                try
                {
                    if (_uiUpdateTask != null && !_uiUpdateTask.IsCompleted)
                        _uiUpdateTask.Wait(1000);
                }
                catch { }

                _cts?.Dispose();
                _cts = null;

                if (_channelHandle != IntPtr.Zero)
                {
                    try
                    {
                        ZCAN_ResetCAN(_channelHandle);
                    }
                    catch { }
                }

                _messageQueue.Clear();
                _signalUpdateQueue.Clear();

                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"停止CAN异常: {ex.Message}");
                return false;
            }
        }

        public bool SendMessage(CANMessage message)
        {
            try
            {
                if (!_isStart)
                {
                    OnError?.Invoke("CAN未启动");
                    return false;
                }

                bool result;
                if (message.IsCANFD)
                {
                    result = SendCANFDMessage(message);
                }
                else
                {
                    result = SendCANMessage(message);
                }

                if (result)
                {
                    message.Time = DateTime.Now;
                    OnCANFrameSent?.Invoke(message);
                }

                return result;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"发送报文异常: {ex.Message}");
                return false;
            }
        }

        public bool SendControlCommand(bool chargeCmd, bool dischargeCmd, bool sleepCmd,
                                      ushort cycleSetting, ushort delay, byte operatingConditions,
                                      bool doorLFCmd, bool doorRFCmd, bool doorLRCmd, bool doorRRCmd,
                                      bool doorLRChild, bool doorRRChild)
        {
            try
            {
                var message = new CANMessage
                {
                    ID = 1537,
                    FrameType = CANFrameType.Standard,
                    Data = new byte[8],
                    IsCANFD = true
                };

                SetBit(message.Data, 14, chargeCmd);
                SetBit(message.Data, 13, dischargeCmd);
                SetBit(message.Data, 15, sleepCmd);
                SetBits(message.Data, 8, 3, operatingConditions);
                SetBit(message.Data, 7, doorLFCmd);
                SetBit(message.Data, 6, doorRFCmd);
                SetBit(message.Data, 5, doorLRCmd);
                SetBit(message.Data, 4, doorRRCmd);
                SetBit(message.Data, 3, doorLRChild);
                SetBit(message.Data, 2, doorRRChild);
                
                message.Data[2] = (byte)(delay >> 8);
                message.Data[3] = (byte)(delay & 0xFF);
                message.Data[4] = (byte)(cycleSetting >> 8);
                message.Data[5] = (byte)(cycleSetting & 0xFF);

                return SendMessage(message);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"发送控制命令异常: {ex.Message}");
                return false;
            }
        }

        public void ReadVersion(Action<string> softwareVersionCallback, Action<string> hardwareVersionCallback)
        {
            try
            {
                _softwareVersionCallback = softwareVersionCallback;
                _hardwareVersionCallback = hardwareVersionCallback;
                _softwareVersionBuffer = "";
                _hardwareVersionBuffer = "";

                var softwareVersionMessage = new CANMessage
                {
                    ID = 0x718,
                    FrameType = CANFrameType.Standard,
                    Data = new byte[8] { 0x03, 0x22, 0xF1, 0x89, 0x00, 0x00, 0x00, 0x00 },
                    IsCANFD = false
                };

                SendMessage(softwareVersionMessage);

                Task.Delay(100).Wait();

                var hardwareVersionMessage = new CANMessage
                {
                    ID = 0x718,
                    FrameType = CANFrameType.Standard,
                    Data = new byte[8] { 0x03, 0x22, 0xF0, 0x89, 0x00, 0x00, 0x00, 0x00 },
                    IsCANFD = false
                };

                SendMessage(hardwareVersionMessage);

                Task.Delay(200).Wait();

                if (string.IsNullOrEmpty(_softwareVersionBuffer))
                {
                    softwareVersionCallback?.Invoke("读取失败");
                }

                if (string.IsNullOrEmpty(_hardwareVersionBuffer))
                {
                    hardwareVersionCallback?.Invoke("读取失败");
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"读取版本号异常: {ex.Message}");
                softwareVersionCallback?.Invoke("读取失败");
                hardwareVersionCallback?.Invoke("读取失败");
            }
        }

        public bool SendCanWakeupMessage()
        {
            try
            {
                var message = new CANMessage
                {
                    ID = 0x400,
                    FrameType = CANFrameType.Standard,
                    Data = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                    IsCANFD = false
                };

                return SendMessage(message);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"发送CAN唤醒报文异常: {ex.Message}");
                return false;
            }
        }

        private void SetBit(byte[] data, int bitPosition, bool value)
        {
            int byteIndex = bitPosition / 8;
            int bitIndex = bitPosition % 8;
            if (byteIndex < data.Length)
            {
                if (value)
                {
                    data[byteIndex] |= (byte)(1 << bitIndex);
                }
                else
                {
                    data[byteIndex] &= (byte)~(1 << bitIndex);
                }
            }
        }

        private void SetBits(byte[] data, int startBit, int length, ushort value)
        {
            for (int i = 0; i < length; i++)
            {
                bool bitValue = (value & (1 << i)) != 0;
                SetBit(data, startBit + i, bitValue);
            }
        }

        private bool SendCANMessage(CANMessage message)
        {
            ZCAN_Transmit_Data transmitData = new ZCAN_Transmit_Data();
            transmitData.frame = new can_frame();
            transmitData.frame.can_id = message.FrameType == CANFrameType.Extended ?
                message.ID | 0x80000000U : message.ID;
            transmitData.frame.can_dlc = message.DataLength;
            transmitData.frame.data = new byte[8];
            Array.Copy(message.Data, transmitData.frame.data, Math.Min(message.Data.Length, 8));
            transmitData.transmit_type = 0;

            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ZCAN_Transmit_Data)));
            try
            {
                Marshal.StructureToPtr(transmitData, ptr, false);
                uint result = ZCAN_Transmit(_channelHandle, ptr, 1);
                return result == 1;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        private bool SendCANFDMessage(CANMessage message)
        {
            ZCAN_TransmitFD_Data transmitData = new ZCAN_TransmitFD_Data();
            transmitData.frame = new canfd_frame();
            transmitData.frame.can_id = message.FrameType == CANFrameType.Extended ?
                message.ID | 0x80000000U : message.ID;
            transmitData.frame.len = message.DataLength;
            transmitData.frame.data = new byte[64];
            Array.Copy(message.Data, transmitData.frame.data, Math.Min(message.Data.Length, 64));
            transmitData.transmit_type = 0;

            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ZCAN_TransmitFD_Data)));
            try
            {
                Marshal.StructureToPtr(transmitData, ptr, false);
                uint result = ZCAN_TransmitFD(_channelHandle, ptr, 1);
                return result == 1;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        private void ReceiveLoop()
        {
            while (!_cts?.IsCancellationRequested ?? false)
            {
                try
                {
                    if (!_isStart || _channelHandle == IntPtr.Zero)
                    {
                        Thread.Sleep(ReceiveIntervalMs);
                        continue;
                    }

                    uint count = ZCAN_GetReceiveNum(_channelHandle, 0);
                    if (count > 0)
                    {
                        int receiveCount = (int)Math.Min(count, 100);
                        IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ZCAN_Receive_Data)));
                        try
                        {
                            for (int i = 0; i < receiveCount; i++)
                            {
                                if (_cts?.IsCancellationRequested ?? false) break;
                                
                                uint result = ZCAN_Receive(_channelHandle, ptr, 1, 0);
                                if (result == 0) break;

                                ZCAN_Receive_Data receiveData = (ZCAN_Receive_Data)Marshal.PtrToStructure(ptr, typeof(ZCAN_Receive_Data))!;

                                CANMessage message = new CANMessage
                                {
                                    ID = receiveData.frame.can_id & 0x7FFFFFFFU,
                                    FrameType = (receiveData.frame.can_id & 0x80000000U) != 0 ? CANFrameType.Extended : CANFrameType.Standard,
                                    Data = new byte[receiveData.frame.can_dlc],
                                    Timestamp = receiveData.timestamp,
                                    IsTransmit = false,
                                    IsCANFD = false,
                                    Time = DateTime.Now
                                };

                                Array.Copy(receiveData.frame.data, message.Data, receiveData.frame.can_dlc);
                                EnqueueMessage(message);
                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(ptr);
                        }
                    }

                    if (_cts?.IsCancellationRequested ?? false) break;

                    count = ZCAN_GetReceiveNum(_channelHandle, 1);
                    if (count > 0)
                    {
                        int receiveCount = (int)Math.Min(count, 100);
                        IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ZCAN_ReceiveFD_Data)));
                        try
                        {
                            for (int i = 0; i < receiveCount; i++)
                            {
                                if (_cts?.IsCancellationRequested ?? false) break;
                                
                                uint result = ZCAN_ReceiveFD(_channelHandle, ptr, 1, 0);
                                if (result == 0) break;

                                ZCAN_ReceiveFD_Data receiveData = (ZCAN_ReceiveFD_Data)Marshal.PtrToStructure(ptr, typeof(ZCAN_ReceiveFD_Data))!;

                                CANMessage message = new CANMessage
                                {
                                    ID = receiveData.frame.can_id & 0x7FFFFFFFU,
                                    FrameType = (receiveData.frame.can_id & 0x80000000U) != 0 ? CANFrameType.Extended : CANFrameType.Standard,
                                    Data = new byte[receiveData.frame.len],
                                    Timestamp = receiveData.timestamp,
                                    IsTransmit = false,
                                    IsCANFD = true,
                                    Time = DateTime.Now
                                };

                                Array.Copy(receiveData.frame.data, message.Data, receiveData.frame.len);
                                EnqueueMessage(message);
                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(ptr);
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnError?.Invoke($"接收报文异常: {ex.Message}");
                }
                finally
                {
                    if (!_cts?.IsCancellationRequested ?? false)
                    {
                        Thread.Sleep(ReceiveIntervalMs);
                    }
                }
            }
        }

        private void EnqueueMessage(CANMessage message)
        {
            while (_messageQueue.Count >= MaxQueueSize)
            {
                if (_messageQueue.TryDequeue(out CANMessage? discarded))
                {
                    OnError?.Invoke($"消息队列已满，丢弃旧消息 (ID: {discarded.ID:X})");
                }
                else
                {
                    break;
                }
            }
            _messageQueue.Enqueue(message);
        }

        private void ProcessLoop()
        {
            while (!_cts?.IsCancellationRequested ?? false)
            {
                try
                {
                    if (!_isStart)
                    {
                        Thread.Sleep(ProcessIntervalMs);
                        continue;
                    }

                    long currentTicks = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
                    long lastSecond = Interlocked.Read(ref _lastSecondTicks);

                    if (currentTicks != lastSecond)
                    {
                        Interlocked.Exchange(ref _messageCount, 0);
                        Interlocked.Exchange(ref _lastSecondTicks, currentTicks);
                    }

                    int maxProcessPerCycle = 100;
                    TimeSpan elapsed = DateTime.Now - _startTime;
                    if (elapsed.TotalSeconds < WarmupPeriodSeconds)
                    {
                        double progress = elapsed.TotalSeconds / WarmupPeriodSeconds;
                        maxProcessPerCycle = (int)(20 + progress * 80);
                    }

                    int processedCount = 0;
                    while (_messageQueue.TryDequeue(out CANMessage? message) && processedCount < maxProcessPerCycle)
                    {
                        if (_cts?.IsCancellationRequested ?? false) break;
                        
                        Interlocked.Increment(ref _messageCount);

                        if (message.ID == 0x710)
                        {
                            ProcessUDSResponse(message);
                        }

                        ParseMessage(message);
                        
                        OnMessageReceived?.Invoke(message);
                        processedCount++;
                    }
                }
                catch (Exception ex)
                {
                    OnError?.Invoke($"处理报文异常: {ex.Message}");
                }
                finally
                {
                    if (!_cts?.IsCancellationRequested ?? false)
                    {
                        Thread.Sleep(ProcessIntervalMs);
                    }
                }
            }
        }

        private void UIUpdateLoop()
        {
            while (!_cts?.IsCancellationRequested ?? false)
            {
                try
                {
                    if (!_isStart)
                    {
                        Thread.Sleep(UIUpdateIntervalMs);
                        continue;
                    }

                    List<SignalUpdate> updates = new List<SignalUpdate>();
                    while (_signalUpdateQueue.TryDequeue(out SignalUpdate? update))
                    {
                        if (_cts?.IsCancellationRequested ?? false) break;
                        updates.Add(update);
                    }

                    if (updates.Count > 0)
                    {
                        foreach (var update in updates)
                        {
                            if (_cts?.IsCancellationRequested ?? false) break;
                            OnSignalValueUpdated?.Invoke(update.Name, update.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnError?.Invoke($"UI更新异常: {ex.Message}");
                }
                finally
                {
                    if (!_cts?.IsCancellationRequested ?? false)
                    {
                        Thread.Sleep(UIUpdateIntervalMs);
                    }
                }
            }
        }

        private void ParseMessage(CANMessage message)
        {
            try
            {
                switch (message.ID)
                {
                    case 1536:
                        ParseSuperCapControllerMessage(message.Data);
                        break;
                    case 1537:
                        break;
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"解析报文异常: {ex.Message}");
            }
        }

        private void ParseSuperCapControllerMessage(byte[] data)
        {
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell1Vol", Value = ParseSignal(data, 8, 16, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell2Vol", Value = ParseSignal(data, 24, 16, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell3Vol", Value = ParseSignal(data, 40, 16, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell4Vol", Value = ParseSignal(data, 56, 16, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell5Vol", Value = ParseSignal(data, 72, 16, 1.0, 0.0) });

            double cell1OverVoltage = ParseSignal(data, 87, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell1OverVoltageState", Value = cell1OverVoltage });
            if (cell1OverVoltage > 0) OnFaultDetected?.Invoke("电容单体1", "过压", "");

            double cell2OverVoltage = ParseSignal(data, 86, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell2OverVoltageState", Value = cell2OverVoltage });
            if (cell2OverVoltage > 0) OnFaultDetected?.Invoke("电容单体2", "过压", "");

            double cell3OverVoltage = ParseSignal(data, 85, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell3OverVoltageState", Value = cell3OverVoltage });
            if (cell3OverVoltage > 0) OnFaultDetected?.Invoke("电容单体3", "过压", "");

            double cell4OverVoltage = ParseSignal(data, 84, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell4OverVoltageState", Value = cell4OverVoltage });
            if (cell4OverVoltage > 0) OnFaultDetected?.Invoke("电容单体4", "过压", "");

            double cell5OverVoltage = ParseSignal(data, 83, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell5OverVoltageState", Value = cell5OverVoltage });
            if (cell5OverVoltage > 0) OnFaultDetected?.Invoke("电容单体5", "过压", "");

            double cell1ShortCircuit = ParseSignal(data, 95, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell1ShortCircuitState", Value = cell1ShortCircuit });
            if (cell1ShortCircuit > 0) OnFaultDetected?.Invoke("电容单体1", "短路", "");

            double cell2ShortCircuit = ParseSignal(data, 94, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell2ShortCircuitState", Value = cell2ShortCircuit });
            if (cell2ShortCircuit > 0) OnFaultDetected?.Invoke("电容单体2", "短路", "");

            double cell3ShortCircuit = ParseSignal(data, 93, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell3ShortCircuitState", Value = cell3ShortCircuit });
            if (cell3ShortCircuit > 0) OnFaultDetected?.Invoke("电容单体3", "短路", "");

            double cell4ShortCircuit = ParseSignal(data, 92, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell4ShortCircuitState", Value = cell4ShortCircuit });
            if (cell4ShortCircuit > 0) OnFaultDetected?.Invoke("电容单体4", "短路", "");

            double cell5ShortCircuit = ParseSignal(data, 91, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell5ShortCircuitState", Value = cell5ShortCircuit });
            if (cell5ShortCircuit > 0) OnFaultDetected?.Invoke("电容单体5", "短路", "");

            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_MochainStatus", Value = ParseSignal(data, 80, 3, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_ModulePower", Value = ParseSignal(data, 96, 8, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_ModuleVoltage", Value = ParseSignal(data, 112, 16, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_ModuleCurrent", Value = ParseSignal(data, 128, 16, 1.0, 0.0, true) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_ModuleTempreture", Value = ParseSignal(data, 136, 8, 1.0, 0.0, true) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_ModuleCapacity", Value = ParseSignal(data, 144, 8, 0.1, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_InternalResistance", Value = ParseSignal(data, 160, 16, 1.0, 0.0) });

            double resistanceStatus = ParseSignal(data, 172, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_InternalResistanceStatus", Value = resistanceStatus });
            if (resistanceStatus > 0) OnFaultDetected?.Invoke("电容模组", "内阻异常", "");

            double openCircuitFault = ParseSignal(data, 173, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_OpenCircuitFault", Value = openCircuitFault });
            if (openCircuitFault > 0) OnFaultDetected?.Invoke("电容模组", "断路", "");

            double chargeFault = ParseSignal(data, 174, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_ChargeFault", Value = chargeFault });
            if (chargeFault > 0) OnFaultDetected?.Invoke("充电模块", "充电故障", "");

            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_ChargeVoltage", Value = ParseSignal(data, 199, 16, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_DisChargeCurrent", Value = ParseSignal(data, 184, 16, 1.0, 0.0, true) });

            double kl30Voltage = ParseSignal(data, 216, 16, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_KL30Voltage", Value = kl30Voltage });

            double kl15Voltage = ParseSignal(data, 232, 16, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_KL15Voltage", Value = kl15Voltage });

            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_CollisionSignalStatus", Value = ParseSignal(data, 242, 3, 1.0, 0.0) });
            
            double kl15FaultStatus = ParseSignal(data, 245, 3, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_KL15FaultStatus", Value = kl15FaultStatus });
            if (kl15FaultStatus > 0) OnFaultDetected?.Invoke("KL15电源", "故障", $"状态码: {kl15FaultStatus}");

            double kl30FaultStatus = ParseSignal(data, 88, 3, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_KL30FaultStatus", Value = kl30FaultStatus });
            if (kl30FaultStatus > 0) OnFaultDetected?.Invoke("KL30电源", "故障", $"状态码: {kl30FaultStatus}");
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_CollisionSignalDuty", Value = ParseSignal(data, 248, 8, 1.0, 0.0) });

            double vdsLFCenterMPlus = ParseSignal(data, 169, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_VdsLFCenterMPlus", Value = vdsLFCenterMPlus });
            if (vdsLFCenterMPlus > 0) OnFaultDetected?.Invoke("通道保护", "VdsLFCenterMPlus异常", "");

            double vgsLFCenterMPlus = ParseSignal(data, 257, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_VgsLFCenterMPlus", Value = vgsLFCenterMPlus });
            if (vgsLFCenterMPlus > 0) OnFaultDetected?.Invoke("通道保护", "VgsLFCenterMPlus异常", "");

            double vdsLRCenterMPlus = ParseSignal(data, 168, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_VdsLRCenterMPlus", Value = vdsLRCenterMPlus });
            if (vdsLRCenterMPlus > 0) OnFaultDetected?.Invoke("通道保护", "VdsLRCenterMPlus异常", "");

            double vgsLRCenterMPlus = ParseSignal(data, 271, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_VgsLRCenterMPlus", Value = vgsLRCenterMPlus });
            if (vgsLRCenterMPlus > 0) OnFaultDetected?.Invoke("通道保护", "VgsLRCenterMPlus异常", "");

            double vdsLFCenterMMinus = ParseSignal(data, 241, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_VdsLFCenterMMinus", Value = vdsLFCenterMMinus });
            if (vdsLFCenterMMinus > 0) OnFaultDetected?.Invoke("通道保护", "VdsLFCenterMMinus异常", "");

            double vgsLFCenterMMinus = ParseSignal(data, 258, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_VgsLFCenterMMinus", Value = vgsLFCenterMMinus });
            if (vgsLFCenterMMinus > 0) OnFaultDetected?.Invoke("通道保护", "VgsLFCenterMMinus异常", "");

            double vdsLRMMinus = ParseSignal(data, 240, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_VdsLRMMinus", Value = vdsLRMMinus });
            if (vdsLRMMinus > 0) OnFaultDetected?.Invoke("通道保护", "VdsLRMMinus异常", "");

            double vgsLRMMinus = ParseSignal(data, 270, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_VgsLRMMinus", Value = vgsLRMMinus });
            if (vgsLRMMinus > 0) OnFaultDetected?.Invoke("通道保护", "VgsLRMMinus异常", "");

            double vdsLRChildMPlus = ParseSignal(data, 263, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_VdsLRChildMPlus", Value = vdsLRChildMPlus });
            if (vdsLRChildMPlus > 0) OnFaultDetected?.Invoke("通道保护", "VdsLRChildMPlus异常", "");

            double vgsLRChildMPlus = ParseSignal(data, 256, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_VgsLRChildMPlus", Value = vgsLRChildMPlus });
            if (vgsLRChildMPlus > 0) OnFaultDetected?.Invoke("通道保护", "VgsLRChildMPlus异常", "");

            double vdsRFCenterMMinus = ParseSignal(data, 262, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_VdsRFCenterMMinus", Value = vdsRFCenterMMinus });
            if (vdsRFCenterMMinus > 0) OnFaultDetected?.Invoke("通道保护", "VdsRFCenterMMinus异常", "");

            double vgsRFCenterMMinus = ParseSignal(data, 269, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_VgsRFCenterMMinus", Value = vgsRFCenterMMinus });
            if (vgsRFCenterMMinus > 0) OnFaultDetected?.Invoke("通道保护", "VgsRFCenterMMinus异常", "");

            double vdsRFCenterMPlus = ParseSignal(data, 261, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_VdsRFCenterMPlus", Value = vdsRFCenterMPlus });
            if (vdsRFCenterMPlus > 0) OnFaultDetected?.Invoke("通道保护", "VdsRFCenterMPlus异常", "");

            double vgsRFCenterMPlus = ParseSignal(data, 268, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_VgsRFCenterMPlus", Value = vgsRFCenterMPlus });
            if (vgsRFCenterMPlus > 0) OnFaultDetected?.Invoke("通道保护", "VgsRFCenterMPlus异常", "");

            double vdsRRMMinus = ParseSignal(data, 260, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_VdsRRMMinus", Value = vdsRRMMinus });
            if (vdsRRMMinus > 0) OnFaultDetected?.Invoke("通道保护", "VdsRRMMinus异常", "");

            double vgsRRMMinus = ParseSignal(data, 265, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_VgsRRMMinus", Value = vgsRRMMinus });
            if (vgsRRMMinus > 0) OnFaultDetected?.Invoke("通道保护", "VgsRRMMinus异常", "");

            double vdsRRCenterMPlus = ParseSignal(data, 259, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_VdsRRCenterMPlus", Value = vdsRRCenterMPlus });
            if (vdsRRCenterMPlus > 0) OnFaultDetected?.Invoke("通道保护", "VdsRRCenterMPlus异常", "");

            double vgsRRCenterMPlus = ParseSignal(data, 267, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_VgsRRCenterMPlus", Value = vgsRRCenterMPlus });
            if (vgsRRCenterMPlus > 0) OnFaultDetected?.Invoke("通道保护", "VgsRRCenterMPlus异常", "");

            double vdsRRChildMPlus = ParseSignal(data, 264, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_VdsRRChildMPlus", Value = vdsRRChildMPlus });
            if (vdsRRChildMPlus > 0) OnFaultDetected?.Invoke("通道保护", "VdsRRChildMPlus异常", "");

            double vgsRRChildMPlus = ParseSignal(data, 266, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_VgsRRChildMPlus", Value = vgsRRChildMPlus });
            if (vgsRRChildMPlus > 0) OnFaultDetected?.Invoke("通道保护", "VgsRRChildMPlus异常", "");

            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_ModuleCapacityState", Value = ParseSignal(data, 303, 1, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_CycleTestReport", Value = ParseSignal(data, 312, 16, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_SleepStatus", Value = ParseSignal(data, 299, 1, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_EMCStatus", Value = ParseSignal(data, 300, 1, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_DVHotStatus", Value = ParseSignal(data, 301, 1, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_DVNormalStatus", Value = ParseSignal(data, 302, 1, 1.0, 0.0) });

            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell1BlanceState", Value = ParseSignal(data, 331, 1, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell2BlanceState", Value = ParseSignal(data, 347, 1, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell3BlanceState", Value = ParseSignal(data, 363, 1, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell4BlanceState", Value = ParseSignal(data, 379, 1, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell5BlanceState", Value = ParseSignal(data, 395, 1, 1.0, 0.0) });

            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell1BlanceCount", Value = ParseSignal(data, 332, 12, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell2BlanceCount", Value = ParseSignal(data, 348, 12, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell3BlanceCount", Value = ParseSignal(data, 364, 12, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell4BlanceCount", Value = ParseSignal(data, 380, 12, 1.0, 0.0) });
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Cap_Cell5BlanceCount", Value = ParseSignal(data, 396, 12, 1.0, 0.0) });

            double motor1Fault = ParseSignal(data, 416, 8, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor1Fault", Value = motor1Fault });
            if (motor1Fault > 0) OnFaultDetected?.Invoke("左前中控锁电机", "故障", $"故障码: {motor1Fault}");

            double motor2Fault = ParseSignal(data, 432, 8, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor2Fault", Value = motor2Fault });
            if (motor2Fault > 0) OnFaultDetected?.Invoke("右前中控锁电机", "故障", $"故障码: {motor2Fault}");

            double motor3Fault = ParseSignal(data, 408, 8, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor3Fault", Value = motor3Fault });
            if (motor3Fault > 0) OnFaultDetected?.Invoke("左后中控锁电机", "故障", $"故障码: {motor3Fault}");

            double motor4Fault = ParseSignal(data, 424, 8, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor4Fault", Value = motor4Fault });
            if (motor4Fault > 0) OnFaultDetected?.Invoke("右后中控锁电机", "故障", $"故障码: {motor4Fault}");

            double motor5Fault = ParseSignal(data, 440, 8, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor5Fault", Value = motor5Fault });
            if (motor5Fault > 0) OnFaultDetected?.Invoke("左后儿童锁电机", "故障", $"故障码: {motor5Fault}");

            double motor6Fault = ParseSignal(data, 448, 8, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor6Fault", Value = motor6Fault });
            if (motor6Fault > 0) OnFaultDetected?.Invoke("右后儿童锁电机", "故障", $"故障码: {motor6Fault}");

            double motor1ShortGround = ParseSignal(data, 416, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor1ShortGround", Value = motor1ShortGround });
            if (motor1ShortGround > 0) OnFaultDetected?.Invoke("左前中控锁电机", "短地故障", "");

            double motor1OpenCircuit = ParseSignal(data, 417, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor1OpenCircuit", Value = motor1OpenCircuit });
            if (motor1OpenCircuit > 0) OnFaultDetected?.Invoke("左前中控锁电机", "开路故障", "");

            double motor1ShortPower = ParseSignal(data, 418, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor1ShortPower", Value = motor1ShortPower });
            if (motor1ShortPower > 0) OnFaultDetected?.Invoke("左前中控锁电机", "短电源故障", "");

            double motor2ShortGround = ParseSignal(data, 432, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor2ShortGround", Value = motor2ShortGround });
            if (motor2ShortGround > 0) OnFaultDetected?.Invoke("右前中控锁电机", "短地故障", "");

            double motor2OpenCircuit = ParseSignal(data, 433, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor2OpenCircuit", Value = motor2OpenCircuit });
            if (motor2OpenCircuit > 0) OnFaultDetected?.Invoke("右前中控锁电机", "开路故障", "");

            double motor2ShortPower = ParseSignal(data, 434, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor2ShortPower", Value = motor2ShortPower });
            if (motor2ShortPower > 0) OnFaultDetected?.Invoke("右前中控锁电机", "短电源故障", "");

            double motor3ShortGround = ParseSignal(data, 408, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor3ShortGround", Value = motor3ShortGround });
            if (motor3ShortGround > 0) OnFaultDetected?.Invoke("左后中控锁电机", "短地故障", "");

            double motor3OpenCircuit = ParseSignal(data, 409, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor3OpenCircuit", Value = motor3OpenCircuit });
            if (motor3OpenCircuit > 0) OnFaultDetected?.Invoke("左后中控锁电机", "开路故障", "");

            double motor3ShortPower = ParseSignal(data, 410, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor3ShortPower", Value = motor3ShortPower });
            if (motor3ShortPower > 0) OnFaultDetected?.Invoke("左后中控锁电机", "短电源故障", "");

            double motor4ShortGround = ParseSignal(data, 424, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor4ShortGround", Value = motor4ShortGround });
            if (motor4ShortGround > 0) OnFaultDetected?.Invoke("右后中控锁电机", "短地故障", "");

            double motor4OpenCircuit = ParseSignal(data, 425, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor4OpenCircuit", Value = motor4OpenCircuit });
            if (motor4OpenCircuit > 0) OnFaultDetected?.Invoke("右后中控锁电机", "开路故障", "");

            double motor4ShortPower = ParseSignal(data, 426, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor4ShortPower", Value = motor4ShortPower });
            if (motor4ShortPower > 0) OnFaultDetected?.Invoke("右后中控锁电机", "短电源故障", "");

            double motor5ShortGround = ParseSignal(data, 440, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor5ShortGround", Value = motor5ShortGround });
            if (motor5ShortGround > 0) OnFaultDetected?.Invoke("左后儿童锁电机", "短地故障", "");

            double motor5OpenCircuit = ParseSignal(data, 441, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor5OpenCircuit", Value = motor5OpenCircuit });
            if (motor5OpenCircuit > 0) OnFaultDetected?.Invoke("左后儿童锁电机", "开路故障", "");

            double motor5ShortPower = ParseSignal(data, 442, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor5ShortPower", Value = motor5ShortPower });
            if (motor5ShortPower > 0) OnFaultDetected?.Invoke("左后儿童锁电机", "短电源故障", "");

            double motor6ShortGround = ParseSignal(data, 448, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor6ShortGround", Value = motor6ShortGround });
            if (motor6ShortGround > 0) OnFaultDetected?.Invoke("右后儿童锁电机", "短地故障", "");

            double motor6OpenCircuit = ParseSignal(data, 449, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor6OpenCircuit", Value = motor6OpenCircuit });
            if (motor6OpenCircuit > 0) OnFaultDetected?.Invoke("右后儿童锁电机", "开路故障", "");

            double motor6ShortPower = ParseSignal(data, 450, 1, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_Motor6ShortPower", Value = motor6ShortPower });
            if (motor6ShortPower > 0) OnFaultDetected?.Invoke("右后儿童锁电机", "短电源故障", "");

            double internalStatus = ParseSignal(data, 280, 16, 1.0, 0.0);
            _signalUpdateQueue.Enqueue(new SignalUpdate { Name = "SuperCapController_InternalStatus", Value = internalStatus });
            if (internalStatus > 0) OnFaultDetected?.Invoke("内部驱动", "内部故障", $"故障码: {internalStatus}");
        }

        private void ProcessUDSResponse(CANMessage message)
        {
            try
            {
                if (message.Data.Length < 3)
                    return;

                byte serviceId = message.Data[1];

                if (serviceId == 0x62)
                {
                    ushort did = (ushort)((message.Data[2] << 8) | message.Data[3]);

                    if (did == 0xF189)
                    {
                        string version = Encoding.ASCII.GetString(message.Data, 4, message.Data.Length - 4).Trim();
                        _softwareVersionBuffer = version;
                        _softwareVersionCallback?.Invoke(version);
                    }
                    else if (did == 0xF089)
                    {
                        string version = Encoding.ASCII.GetString(message.Data, 4, message.Data.Length - 4).Trim();
                        _hardwareVersionBuffer = version;
                        _hardwareVersionCallback?.Invoke(version);
                    }
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"处理UDS响应异常: {ex.Message}");
            }
        }

        private double ParseSignal(byte[] data, int startBit, int length, double scaling, double offset, bool isSigned = false, bool isMotorola = true)
        {
            ulong rawValue = 0;

            if (data == null || data.Length == 0 || length <= 0)
            {
                return offset;
            }

            if (isMotorola)
            {
                int byteIndex = startBit / 8;
                int bitIndex = startBit % 8;

                int remainingBits = length;
                int bitPos = 0;
                while (remainingBits > 0 && byteIndex >= 0 && byteIndex < data.Length)
                {
                    int bitsToTake = Math.Min(remainingBits, 8 - bitIndex);
                    byte mask = (byte)((1 << bitsToTake) - 1);
                    byte value = (byte)((data[byteIndex] >> bitIndex) & mask);
                    rawValue |= (ulong)value << bitPos;

                    bitPos += bitsToTake;
                    remainingBits -= bitsToTake;
                    byteIndex--;
                    bitIndex = 0;
                }
            }
            else
            {
                int byteIndex = startBit / 8;
                int bitIndex = startBit % 8;

                int remainingBits = length;
                int bitPos = 0;
                while (remainingBits > 0 && byteIndex >= 0 && byteIndex < data.Length)
                {
                    int bitsToTake = Math.Min(remainingBits, 8 - bitIndex);
                    byte mask = (byte)((1 << bitsToTake) - 1);
                    byte value = (byte)((data[byteIndex] >> bitIndex) & mask);
                    rawValue |= (ulong)value << bitPos;

                    bitPos += bitsToTake;
                    remainingBits -= bitsToTake;
                    byteIndex++;
                    bitIndex = 0;
                }
            }

            long signedValue = 0;
            if (isSigned && length > 0)
            {
                if ((rawValue & (1UL << (length - 1))) != 0)
                {
                    signedValue = (long)(rawValue | (~0UL << length));
                }
                else
                {
                    signedValue = (long)rawValue;
                }
            }
            else
            {
                signedValue = (long)rawValue;
            }

            double scaledValue = signedValue * scaling + offset;
            return scaledValue;
        }

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            StopCAN();

            if (_channelHandle != IntPtr.Zero)
            {
                try
                {
                    ZCAN_ResetCAN(_channelHandle);
                }
                catch { }
            }

            if (_deviceHandle != IntPtr.Zero)
            {
                try
                {
                    ZCAN_CloseDevice(_deviceHandle);
                    _deviceHandle = IntPtr.Zero;
                }
                catch { }
            }

            _channelHandle = IntPtr.Zero;
            _isOpen = false;
            _isStart = false;

            _disposed = true;
        }

        ~CANCommunication()
        {
            Dispose(false);
        }

        public void CloseDevice()
        {
            try
            {
                StopCAN();

                if (_deviceHandle != IntPtr.Zero)
                {
                    ZCAN_CloseDevice(_deviceHandle);
                    _deviceHandle = IntPtr.Zero;
                }

                _channelHandle = IntPtr.Zero;
                _isOpen = false;
                OnConnectionStatusChanged?.Invoke(false);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"关闭设备异常: {ex.Message}");
            }
        }

        public bool ClearBuffer()
        {
            try
            {
                if (_channelHandle != IntPtr.Zero)
                {
                    uint result = ZCAN_ClearBuffer(_channelHandle);
                    return result == 1;
                }
                return false;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"清空缓冲区异常: {ex.Message}");
                return false;
            }
        }

        public string GetChannelErrorInfo()
        {
            try
            {
                if (_channelHandle == IntPtr.Zero)
                    return "通道未初始化";

                ZCAN_CHANNEL_ERR_INFO errInfo = new ZCAN_CHANNEL_ERR_INFO();
                uint result = ZCAN_ReadChannelErrInfo(_channelHandle, ref errInfo);
                if (result != 1)
                    return "读取错误信息失败";

                return $"错误码: 0x{errInfo.error_code:X8}";
            }
            catch (Exception ex)
            {
                return $"获取错误信息异常: {ex.Message}";
            }
        }

        private string GetDeviceTypeName(CANDeviceType type)
        {
            return type switch
            {
                CANDeviceType.USBCAN1 => "USBCAN1",
                CANDeviceType.USBCAN2 => "USBCAN2",
                CANDeviceType.USBCAN_E_U => "USBCAN-E-U",
                CANDeviceType.USBCAN_2E_U => "USBCAN-2E-U",
                CANDeviceType.USBCAN_4E_U => "USBCAN-4E-U",
                CANDeviceType.USBCAN_8E_U => "USBCAN-8E-U",
                CANDeviceType.USBCANFD_200U => "USBCANFD-200U",
                CANDeviceType.USBCANFD_100U => "USBCANFD-100U",
                CANDeviceType.USBCANFD_MINI => "USBCANFD-MINI",
                CANDeviceType.USBCANFD_400U => "USBCANFD-400U",
                CANDeviceType.USBCANFD_800U => "USBCANFD-800U",
                CANDeviceType.USBCANFD_800H => "USBCANFD-800H",
                CANDeviceType.PCIE_CANFD_200U => "PCIE-CANFD-200U",
                CANDeviceType.PCIE_CANFD_400U => "PCIE-CANFD-400U",
                CANDeviceType.CANET_TCP => "CANET-TCP",
                CANDeviceType.CANET_UDP => "CANET-UDP",
                CANDeviceType.CANFDNET_200U_TCP => "CANFDNET-200U-TCP",
                CANDeviceType.CANFDNET_200U_UDP => "CANFDNET-200U-UDP",
                CANDeviceType.CANFDNET_400U_TCP => "CANFDNET-400U-TCP",
                CANDeviceType.CANFDNET_400U_UDP => "CANFDNET-400U-UDP",
                CANDeviceType.CANFDNET_800U_TCP => "CANFDNET-800U-TCP",
                CANDeviceType.CANFDNET_800U_UDP => "CANFDNET-800U-UDP",
                CANDeviceType.CANFDWIFI_100U_TCP => "CANFDWIFI-100U-TCP",
                CANDeviceType.CANFDWIFI_100U_UDP => "CANFDWIFI-100U-UDP",
                CANDeviceType.CANFDWIFI_200U_TCP => "CANFDWIFI-200U-TCP",
                CANDeviceType.CANFDWIFI_200U_UDP => "CANFDWIFI-200U-UDP",
                _ => "Unknown"
            };
        }

        private byte GetTiming0(uint baudRate)
        {
            return baudRate switch
            {
                1000000 => 0x00,
                800000 => 0x00,
                500000 => 0x00,
                250000 => 0x01,
                125000 => 0x03,
                100000 => 0x04,
                50000 => 0x09,
                20000 => 0x18,
                10000 => 0x31,
                5000 => 0xBF,
                _ => 0x00
            };
        }

        private byte GetTiming1(uint baudRate)
        {
            return baudRate switch
            {
                1000000 => 0x14,
                800000 => 0x16,
                500000 => 0x1C,
                250000 => 0x1C,
                125000 => 0x1C,
                100000 => 0x1C,
                50000 => 0x1C,
                20000 => 0x1C,
                10000 => 0x1C,
                5000 => 0xFF,
                _ => 0x1C
            };
        }

        public static List<(CANDeviceType Type, string Name)> GetAvailableDevices()
        {
            return new List<(CANDeviceType, string)>
            {
                (CANDeviceType.USBCANFD_200U, "USBCANFD-200U"),
                (CANDeviceType.USBCANFD_100U, "USBCANFD-100U")
            };
        }

        public static List<(uint BaudRate, string Name)> GetCommonBaudRates()
        {
            return new List<(uint, string)>
            {
                (1000000, "1 Mbps"),
                (800000, "800 kbps"),
                (500000, "500 kbps"),
                (250000, "250 kbps"),
                (125000, "125 kbps"),
                (100000, "100 kbps"),
                (50000, "50 kbps"),
                (20000, "20 kbps"),
                (10000, "10 kbps"),
                (5000, "5 kbps")
            };
        }
    }
}