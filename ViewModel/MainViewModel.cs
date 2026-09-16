using System.Collections.Generic;
using System.Windows;
using MHC.Model;
using System;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MHC.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private Dictionary<string, double> _signalValues;
        private FaultManager _faultManager;
        
        private Dictionary<string, string> _signalDisplayNameMap;
        private Dictionary<string, string> _signalFormatMap;

        public double OverTemperatureThreshold { get; set; } = 85.0;
        public double CapacityMinThreshold { get; set; } = 5.0;
        public double CapacityMaxThreshold { get; set; } = 6.0;
        public double VoltageDiffThreshold { get; set; } = 0.12;
        public double DischargeCurrentThreshold { get; set; } = -10000.0;
        public double InternalResistanceThreshold { get; set; } = 200.0;

        private string _passwordHash;
        private string _settingsFilePath;
        private string _passwordFilePath;

        private int _faultCount;
        public int FaultCount
        {
            get { return _faultCount; }
            set
            {
                _faultCount = value;
                OnPropertyChanged("FaultCount");
            }
        }

        public Dictionary<string, double> SignalValues
        {
            get { return _signalValues; }
            set
            {
                _signalValues = value;
                OnPropertyChanged("SignalValues");
            }
        }

        public MainViewModel()
        {
            _signalValues = new Dictionary<string, double>();
            _faultManager = new FaultManager();
            InitializeSignalValues();
            InitializeDisplayMappings();
            InitializeSettingsPaths();
            LoadPassword();
            LoadSettings();
        }

        private void InitializeSettingsPaths()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "MHC");
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            _settingsFilePath = Path.Combine(appFolder, "settings.ini");
            _passwordFilePath = Path.Combine(appFolder, "password.ini");
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private void LoadPassword()
        {
            if (File.Exists(_passwordFilePath))
            {
                _passwordHash = File.ReadAllText(_passwordFilePath).Trim();
            }
            else
            {
                _passwordHash = HashPassword("admin");
                File.WriteAllText(_passwordFilePath, _passwordHash);
            }
        }

        public bool VerifyPassword(string password)
        {
            return HashPassword(password) == _passwordHash;
        }

        public void ChangePassword(string newPassword)
        {
            _passwordHash = HashPassword(newPassword);
            File.WriteAllText(_passwordFilePath, _passwordHash);
        }

        public void SaveSettings()
        {
            var settings = new Dictionary<string, string>
            {
                { "OverTemperatureThreshold", OverTemperatureThreshold.ToString() },
                { "CapacityMinThreshold", CapacityMinThreshold.ToString() },
                { "CapacityMaxThreshold", CapacityMaxThreshold.ToString() },
                { "VoltageDiffThreshold", VoltageDiffThreshold.ToString() },
                { "DischargeCurrentThreshold", DischargeCurrentThreshold.ToString() },
                { "InternalResistanceThreshold", InternalResistanceThreshold.ToString() }
            };

            using (var writer = new StreamWriter(_settingsFilePath))
            {
                foreach (var kvp in settings)
                {
                    writer.WriteLine($"{kvp.Key}={kvp.Value}");
                }
            }
        }

        private void LoadSettings()
        {
            if (File.Exists(_settingsFilePath))
            {
                var lines = File.ReadAllLines(_settingsFilePath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();

                        switch (key)
                        {
                            case "OverTemperatureThreshold":
                                if (double.TryParse(value, out double temp))
                                    OverTemperatureThreshold = temp;
                                break;
                            case "CapacityMinThreshold":
                                if (double.TryParse(value, out double capMin))
                                    CapacityMinThreshold = capMin;
                                break;
                            case "CapacityMaxThreshold":
                                if (double.TryParse(value, out double capMax))
                                    CapacityMaxThreshold = capMax;
                                break;
                            case "VoltageDiffThreshold":
                                if (double.TryParse(value, out double voltDiff))
                                    VoltageDiffThreshold = voltDiff;
                                break;
                            case "DischargeCurrentThreshold":
                                if (double.TryParse(value, out double discharge))
                                    DischargeCurrentThreshold = discharge;
                                break;
                            case "InternalResistanceThreshold":
                                if (double.TryParse(value, out double resistance))
                                    InternalResistanceThreshold = resistance;
                                break;
                        }
                    }
                }
            }
        }

        public void ClearFaultCount()
        {
            _faultManager.ClearFaultCount();
            FaultCount = 0;
        }

        private void InitializeDisplayMappings()
        {
            _signalDisplayNameMap = new Dictionary<string, string>
            {
                {"SuperCapController_Cap_Cell1Vol", "Cell1电压"},
                {"SuperCapController_Cap_Cell2Vol", "Cell2电压"},
                {"SuperCapController_Cap_Cell3Vol", "Cell3电压"},
                {"SuperCapController_Cap_Cell4Vol", "Cell4电压"},
                {"SuperCapController_Cap_Cell5Vol", "Cell5电压"},
                {"SuperCapController_Cap_Cell1OverVoltageState", "Cell1过压"},
                {"SuperCapController_Cap_Cell1ShortCircuitState", "Cell1短路"},
                {"SuperCapController_Cap_Cell1BlanceState", "Cell1均衡"},
                {"SuperCapController_Cap_Cell1BlanceCount", "Cell1均衡次数"},
                {"SuperCapController_Cap_Cell2OverVoltageState", "Cell2过压"},
                {"SuperCapController_Cap_Cell2ShortCircuitState", "Cell2短路"},
                {"SuperCapController_Cap_Cell2BlanceState", "Cell2均衡"},
                {"SuperCapController_Cap_Cell2BlanceCount", "Cell2均衡次数"},
                {"SuperCapController_Cap_Cell3OverVoltageState", "Cell3过压"},
                {"SuperCapController_Cap_Cell3ShortCircuitState", "Cell3短路"},
                {"SuperCapController_Cap_Cell3BlanceState", "Cell3均衡"},
                {"SuperCapController_Cap_Cell3BlanceCount", "Cell3均衡次数"},
                {"SuperCapController_Cap_Cell4OverVoltageState", "Cell4过压"},
                {"SuperCapController_Cap_Cell4ShortCircuitState", "Cell4短路"},
                {"SuperCapController_Cap_Cell4BlanceState", "Cell4均衡"},
                {"SuperCapController_Cap_Cell4BlanceCount", "Cell4均衡次数"},
                {"SuperCapController_Cap_Cell5OverVoltageState", "Cell5过压"},
                {"SuperCapController_Cap_Cell5ShortCircuitState", "Cell5短路"},
                {"SuperCapController_Cap_Cell5BlanceState", "Cell5均衡"},
                {"SuperCapController_Cap_Cell5BlanceCount", "Cell5均衡次数"},
                {"SuperCapController_Cap_ModulePower", "模组电量"},
                {"SuperCapController_Cap_ModuleVoltage", "模组电压"},
                {"SuperCapController_Cap_ModuleCurrent", "模组电流"},
                {"SuperCapController_Cap_DisChargeCurrent", "放电平均电流"},
                {"SuperCapController_Cap_MochainStatus", "充放电状态"},
                {"SuperCapController_Cap_ModuleTempreture", "温度"},
                {"SuperCapController_Cap_OverTempretureState", "过温"},
                {"SuperCapController_Cap_ModuleCapacity", "容值"},
                {"SuperCapController_Cap_ModuleCapacityState", "容值状态"},
                {"SuperCapController_Cap_InternalResistance", "内阻"},
                {"SuperCapController_Cap_InternalResistanceStatus", "内阻状态"},
                {"SuperCapController_Cap_OpenCircuitFault", "断路状态"},
                {"SuperCapController_Cap_ImbalanceFault", "不均衡"},
                {"SuperCapController_Cap_ChargeFault", "充电故障"},
                {"SuperCapController_Cap_DisChargeFault", "放电故障"},
                {"SuperCapController_Cap_KL30Voltage", "KL30电压"},
                {"SuperCapController_Cap_KL30FaultStatus", "KL30故障状态"},
                {"SuperCapController_Cap_KL15Voltage", "KL15电压"},
                {"SuperCapController_Cap_KL15FaultStatus", "KL15故障状态"},
                {"SuperCapController_Cap_CollisionSignalDuty", "PWM占空比"},
                {"SuperCapController_Cap_CollisionSignalStatus", "PWM状态"},
                {"SuperCapController_VdsLFCenterMPlus", "LFCenterM+ VDS"},
                {"SuperCapController_VgsLFCenterMPlus", "LFCenterM+ VGS"},
                {"SuperCapController_VdsLRCenterMPlus", "LRCenterM+ VDS"},
                {"SuperCapController_VgsLRCenterMPlus", "LRCenterM+ VGS"},
                {"SuperCapController_VdsLFCenterMMinus", "LFCenterM- VDS"},
                {"SuperCapController_VgsLFCenterMMinus", "LFCenterM- VGS"},
                {"SuperCapController_VdsLRMMinus", "LRMMinus VDS"},
                {"SuperCapController_VgsLRMMinus", "LRMMinus VGS"},
                {"SuperCapController_VdsLRChildMPlus", "LRChildM+ VDS"},
                {"SuperCapController_VgsLRChildMPlus", "LRChildM+ VGS"},
                {"SuperCapController_VdsRFCenterMMinus", "RFCenterM- VDS"},
                {"SuperCapController_VgsRFCenterMMinus", "RFCenterM- VGS"},
                {"SuperCapController_VdsRFCenterMPlus", "RFCenterM+ VDS"},
                {"SuperCapController_VgsRFCenterMPlus", "RFCenterM+ VGS"},
                {"SuperCapController_VdsRRMMinus", "RRMMinus VDS"},
                {"SuperCapController_VgsRRMMinus", "RRMMinus VGS"},
                {"SuperCapController_VdsRRCenterMPlus", "RRCenterM+ VDS"},
                {"SuperCapController_VgsRRCenterMPlus", "RRCenterM+ VGS"},
                {"SuperCapController_VdsRRChildMPlus", "RRChildM+ VDS"},
                {"SuperCapController_VgsRRChildMPlus", "RRChildM+ VGS"},
                {"SuperCapController_Motor1ShortPower", "电机1短电源"},
                {"SuperCapController_Motor1ShortGround", "电机1短地"},
                {"SuperCapController_Motor1OpenCircuit", "电机1开路"},
                {"SuperCapController_Motor2ShortPower", "电机2短电源"},
                {"SuperCapController_Motor2ShortGround", "电机2短地"},
                {"SuperCapController_Motor2OpenCircuit", "电机2开路"},
                {"SuperCapController_Motor3ShortPower", "电机3短电源"},
                {"SuperCapController_Motor3ShortGround", "电机3短地"},
                {"SuperCapController_Motor3OpenCircuit", "电机3开路"},
                {"SuperCapController_Motor4ShortPower", "电机4短电源"},
                {"SuperCapController_Motor4ShortGround", "电机4短地"},
                {"SuperCapController_Motor4OpenCircuit", "电机4开路"},
                {"SuperCapController_Motor5ShortPower", "电机5短电源"},
                {"SuperCapController_Motor5ShortGround", "电机5短地"},
                {"SuperCapController_Motor5OpenCircuit", "电机5开路"},
                {"SuperCapController_Motor6ShortPower", "电机6短电源"},
                {"SuperCapController_Motor6ShortGround", "电机6短地"},
                {"SuperCapController_Motor6OpenCircuit", "电机6开路"},
                {"SuperCapController_InternalStatus", "内部状态"}
            };

            _signalFormatMap = new Dictionary<string, string>
            {
                {"SuperCapController_Cap_Cell1Vol", "F0"},
                {"SuperCapController_Cap_Cell2Vol", "F0"},
                {"SuperCapController_Cap_Cell3Vol", "F0"},
                {"SuperCapController_Cap_Cell4Vol", "F0"},
                {"SuperCapController_Cap_Cell5Vol", "F0"},
                {"SuperCapController_Cap_ModulePower", "F1"},
                {"SuperCapController_Cap_ModuleVoltage", "F0"},
                {"SuperCapController_Cap_ModuleCurrent", "F0"},
                {"SuperCapController_Cap_DisChargeCurrent", "F0"},
                {"SuperCapController_Cap_ModuleTempreture", "F1"},
                {"SuperCapController_Cap_ModuleCapacity", "F1"},
                {"SuperCapController_Cap_InternalResistance", "F0"},
                {"SuperCapController_Cap_KL30Voltage", "F0"},
                {"SuperCapController_Cap_KL15Voltage", "F0"},
                {"SuperCapController_Cap_CollisionSignalDuty", "F1"}
            };
        }

        public void InitializeSignalValues()
        {
            string[] signalKeys = {
                "SuperCapController_Cap_Cell1Vol",
                "SuperCapController_Cap_Cell2Vol",
                "SuperCapController_Cap_Cell3Vol",
                "SuperCapController_Cap_Cell4Vol",
                "SuperCapController_Cap_Cell5Vol",
                "SuperCapController_Cap_Cell1OverVoltageState",
                "SuperCapController_Cap_Cell1ShortCircuitState",
                "SuperCapController_Cap_Cell1BlanceState",
                "SuperCapController_Cap_Cell1BlanceCount",
                "SuperCapController_Cap_Cell2OverVoltageState",
                "SuperCapController_Cap_Cell2ShortCircuitState",
                "SuperCapController_Cap_Cell2BlanceState",
                "SuperCapController_Cap_Cell2BlanceCount",
                "SuperCapController_Cap_Cell3OverVoltageState",
                "SuperCapController_Cap_Cell3ShortCircuitState",
                "SuperCapController_Cap_Cell3BlanceState",
                "SuperCapController_Cap_Cell3BlanceCount",
                "SuperCapController_Cap_Cell4OverVoltageState",
                "SuperCapController_Cap_Cell4ShortCircuitState",
                "SuperCapController_Cap_Cell4BlanceState",
                "SuperCapController_Cap_Cell4BlanceCount",
                "SuperCapController_Cap_Cell5OverVoltageState",
                "SuperCapController_Cap_Cell5ShortCircuitState",
                "SuperCapController_Cap_Cell5BlanceState",
                "SuperCapController_Cap_Cell5BlanceCount",
                "SuperCapController_Cap_ModulePower",
                "SuperCapController_Cap_ModuleVoltage",
                "SuperCapController_Cap_ModuleCurrent",
                "SuperCapController_Cap_DisChargeCurrent",
                "SuperCapController_Cap_MochainStatus",
                "SuperCapController_Cap_ModuleTempreture",
                "SuperCapController_Cap_OverTempretureState",
                "SuperCapController_Cap_ModuleCapacity",
                "SuperCapController_Cap_ModuleCapacityState",
                "SuperCapController_Cap_InternalResistance",
                "SuperCapController_Cap_InternalResistanceStatus",
                "SuperCapController_Cap_OpenCircuitFault",
                "SuperCapController_Cap_ImbalanceFault",
                "SuperCapController_Cap_ChargeFault",
                "SuperCapController_Cap_DisChargeFault",
                "SuperCapController_Cap_KL30Voltage",
                "SuperCapController_Cap_KL30FaultStatus",
                "SuperCapController_Cap_KL15Voltage",
                "SuperCapController_Cap_KL15FaultStatus",
                "SuperCapController_Cap_CollisionSignalDuty",
                "SuperCapController_Cap_CollisionSignalStatus",
                "SuperCapController_VdsLFCenterMPlus",
                "SuperCapController_VgsLFCenterMPlus",
                "SuperCapController_VdsLRCenterMPlus",
                "SuperCapController_VgsLRCenterMPlus",
                "SuperCapController_VdsLFCenterMMinus",
                "SuperCapController_VgsLFCenterMMinus",
                "SuperCapController_VdsLRMMinus",
                "SuperCapController_VgsLRMMinus",
                "SuperCapController_VdsLRChildMPlus",
                "SuperCapController_VgsLRChildMPlus",
                "SuperCapController_VdsRFCenterMMinus",
                "SuperCapController_VgsRFCenterMMinus",
                "SuperCapController_VdsRFCenterMPlus",
                "SuperCapController_VgsRFCenterMPlus",
                "SuperCapController_VdsRRMMinus",
                "SuperCapController_VgsRRMMinus",
                "SuperCapController_VdsRRCenterMPlus",
                "SuperCapController_VgsRRCenterMPlus",
                "SuperCapController_VdsRRChildMPlus",
                "SuperCapController_VgsRRChildMPlus",
                "SuperCapController_Motor1ShortPower",
                "SuperCapController_Motor1ShortGround",
                "SuperCapController_Motor1OpenCircuit",
                "SuperCapController_Motor2ShortPower",
                "SuperCapController_Motor2ShortGround",
                "SuperCapController_Motor2OpenCircuit",
                "SuperCapController_Motor3ShortPower",
                "SuperCapController_Motor3ShortGround",
                "SuperCapController_Motor3OpenCircuit",
                "SuperCapController_Motor4ShortPower",
                "SuperCapController_Motor4ShortGround",
                "SuperCapController_Motor4OpenCircuit",
                "SuperCapController_Motor5ShortPower",
                "SuperCapController_Motor5ShortGround",
                "SuperCapController_Motor5OpenCircuit",
                "SuperCapController_Motor6ShortPower",
                "SuperCapController_Motor6ShortGround",
                "SuperCapController_Motor6OpenCircuit",
                "SuperCapController_InternalStatus"
            };

            foreach (var signalKey in signalKeys)
            {
                if (!_signalValues.ContainsKey(signalKey))
                {
                    _signalValues[signalKey] = -1.0;
                }
            }
        }

        public void UpdateSignalValue(string signalKey, double value)
        {
            if (_signalValues.ContainsKey(signalKey))
            {
                _signalValues[signalKey] = value;
                OnPropertyChanged("SignalValues");
                OnPropertyChanged(signalKey);
            }
            
            CalculateUpperFaults();
        }

        private void CalculateUpperFaults()
        {
            bool hasValidData = _signalValues.Any(kvp => kvp.Value != -1.0);
            if (!hasValidData) return;

            CalculateCapacityFault();
            CalculateInternalResistanceFault();
            CalculateImbalanceFault();
            CalculateDischargeFault();
            CalculateOverTemperatureFault();
        }

        private void CalculateOverTemperatureFault()
        {
            double temperature = _signalValues.TryGetValue("SuperCapController_Cap_ModuleTempreture", out double temp) ? temp : -1.0;
            if (temperature == -1.0) return;

            double overTempState = temperature > OverTemperatureThreshold ? 1.0 : 0.0;
            UpdateSignalValueInternal("SuperCapController_Cap_OverTempretureState", overTempState);
        }

        private void CalculateCapacityFault()
        {
            double capacity = _signalValues.TryGetValue("SuperCapController_Cap_ModuleCapacity", out double cap) ? cap : -1.0;
            if (capacity == -1.0) return;

            double capacityState = capacity < CapacityMinThreshold || capacity > CapacityMaxThreshold ? 1.0 : 0.0;
            UpdateSignalValueInternal("SuperCapController_Cap_ModuleCapacityState", capacityState);
        }

        private void CalculateInternalResistanceFault()
        {
            double resistance = _signalValues.TryGetValue("SuperCapController_Cap_InternalResistance", out double res) ? res : -1.0;
            if (resistance == -1.0) return;

            double resistanceState = resistance > InternalResistanceThreshold ? 1.0 : 0.0;
            UpdateSignalValueInternal("SuperCapController_Cap_InternalResistanceStatus", resistanceState);
        }

        private void CalculateImbalanceFault()
        {
            double cell1 = _signalValues.TryGetValue("SuperCapController_Cap_Cell1Vol", out double c1) ? c1 : -1.0;
            double cell2 = _signalValues.TryGetValue("SuperCapController_Cap_Cell2Vol", out double c2) ? c2 : -1.0;
            double cell3 = _signalValues.TryGetValue("SuperCapController_Cap_Cell3Vol", out double c3) ? c3 : -1.0;
            double cell4 = _signalValues.TryGetValue("SuperCapController_Cap_Cell4Vol", out double c4) ? c4 : -1.0;
            double cell5 = _signalValues.TryGetValue("SuperCapController_Cap_Cell5Vol", out double c5) ? c5 : -1.0;

            if (cell1 == -1.0 || cell2 == -1.0 || cell3 == -1.0 || cell4 == -1.0 || cell5 == -1.0) return;

            double[] voltages = { cell1, cell2, cell3, cell4, cell5 };
            double maxVoltage = voltages.Max();
            double minVoltage = voltages.Min();
            double voltageDiff = (maxVoltage - minVoltage) / 1000.0;

            double imbalanceState = voltageDiff >= VoltageDiffThreshold ? 1.0 : 0.0;
            UpdateSignalValueInternal("SuperCapController_Cap_ImbalanceFault", imbalanceState);
        }

        private void CalculateDischargeFault()
        {
            double mochainStatus = _signalValues.TryGetValue("SuperCapController_Cap_MochainStatus", out double status) ? status : -1.0;
            double dischargeCurrent = _signalValues.TryGetValue("SuperCapController_Cap_DisChargeCurrent", out double current) ? current : -1.0;

            if (mochainStatus == -1.0 || dischargeCurrent == -1.0) return;

            bool isDischarging = mochainStatus == 2;
            double dischargeFaultState = (isDischarging && dischargeCurrent < DischargeCurrentThreshold) ? 1.0 : 0.0;
            UpdateSignalValueInternal("SuperCapController_Cap_DisChargeFault", dischargeFaultState);
        }

        private void UpdateSignalValueInternal(string signalKey, double value)
        {
            if (_signalValues.ContainsKey(signalKey))
            {
                _signalValues[signalKey] = value;
                OnPropertyChanged("SignalValues");
                OnPropertyChanged(signalKey);
            }
        }

        public void CheckAllFaultsAndLog()
        {
            var allFaultSignalKeys = new List<string>();
            var faultDescriptions = new Dictionary<string, string>();

            bool hasValidData = false;
            foreach (var kvp in _signalValues)
            {
                if (kvp.Value != -1.0)
                {
                    hasValidData = true;
                    break;
                }
            }

            if (!hasValidData)
            {
                return;
            }

            foreach (var kvp in _signalValues)
            {
                string signalKey = kvp.Key;
                double value = kvp.Value;

                if (value == -1.0) continue;

                if (signalKey.Contains("OverVoltageState"))
                {
                    if (value > 0)
                    {
                        allFaultSignalKeys.Add(signalKey);
                        faultDescriptions[signalKey] = "过压故障";
                    }
                    else
                    {
                        _faultManager.ClearFaultStatus(signalKey);
                    }
                }
                else if (signalKey.Contains("ShortCircuitState"))
                {
                    if (value > 0)
                    {
                        allFaultSignalKeys.Add(signalKey);
                        faultDescriptions[signalKey] = "短路故障";
                    }
                    else
                    {
                        _faultManager.ClearFaultStatus(signalKey);
                    }
                }
                else if (signalKey.Contains("OpenCircuitFault"))
                {
                    if (value > 0)
                    {
                        allFaultSignalKeys.Add(signalKey);
                        faultDescriptions[signalKey] = "开路故障";
                    }
                    else
                    {
                        _faultManager.ClearFaultStatus(signalKey);
                    }
                }
                else if (signalKey.Contains("ImbalanceFault"))
                {
                    if (value > 0)
                    {
                        allFaultSignalKeys.Add(signalKey);
                        faultDescriptions[signalKey] = "不平衡故障";
                    }
                    else
                    {
                        _faultManager.ClearFaultStatus(signalKey);
                    }
                }
                else if (signalKey.Contains("ChargeFault"))
                {
                    if (value > 0)
                    {
                        allFaultSignalKeys.Add(signalKey);
                        faultDescriptions[signalKey] = "充电故障";
                    }
                    else
                    {
                        _faultManager.ClearFaultStatus(signalKey);
                    }
                }
                else if (signalKey.Contains("DisChargeFault"))
                {
                    if (value > 0)
                    {
                        allFaultSignalKeys.Add(signalKey);
                        faultDescriptions[signalKey] = "放电故障";
                    }
                    else
                    {
                        _faultManager.ClearFaultStatus(signalKey);
                    }
                }
                else if (signalKey.Contains("OverTempretureState"))
                {
                    if (value > 0)
                    {
                        allFaultSignalKeys.Add(signalKey);
                        faultDescriptions[signalKey] = "过温故障";
                    }
                    else
                    {
                        _faultManager.ClearFaultStatus(signalKey);
                    }
                }
                else if (signalKey.Contains("KL30FaultStatus"))
                {
                    if (value > 0)
                    {
                        allFaultSignalKeys.Add(signalKey);
                        faultDescriptions[signalKey] = "KL30故障";
                    }
                    else
                    {
                        _faultManager.ClearFaultStatus(signalKey);
                    }
                }
                else if (signalKey.Contains("KL15FaultStatus"))
                {
                    if (value > 0)
                    {
                        allFaultSignalKeys.Add(signalKey);
                        faultDescriptions[signalKey] = "KL15故障";
                    }
                    else
                    {
                        _faultManager.ClearFaultStatus(signalKey);
                    }
                }
                else if (signalKey.Contains("CollisionSignalStatus"))
                {
                    if (value > 0)
                    {
                        allFaultSignalKeys.Add(signalKey);
                        faultDescriptions[signalKey] = "碰撞信号故障";
                    }
                    else
                    {
                        _faultManager.ClearFaultStatus(signalKey);
                    }
                }
                else if (signalKey.Contains("ShortPower"))
                {
                    if (value > 0)
                    {
                        allFaultSignalKeys.Add(signalKey);
                        faultDescriptions[signalKey] = "短电源故障";
                    }
                    else
                    {
                        _faultManager.ClearFaultStatus(signalKey);
                    }
                }
                else if (signalKey.Contains("ShortGround"))
                {
                    if (value > 0)
                    {
                        allFaultSignalKeys.Add(signalKey);
                        faultDescriptions[signalKey] = "短地故障";
                    }
                    else
                    {
                        _faultManager.ClearFaultStatus(signalKey);
                    }
                }
                else if (signalKey.Contains("OpenCircuit") && signalKey.Contains("Motor"))
                {
                    if (value > 0)
                    {
                        allFaultSignalKeys.Add(signalKey);
                        faultDescriptions[signalKey] = "开路故障";
                    }
                    else
                    {
                        _faultManager.ClearFaultStatus(signalKey);
                    }
                }
                else if (signalKey.Contains("Vds"))
                {
                    if (value > 0)
                    {
                        allFaultSignalKeys.Add(signalKey);
                        faultDescriptions[signalKey] = "VDS故障";
                    }
                    else
                    {
                        _faultManager.ClearFaultStatus(signalKey);
                    }
                }
                else if (signalKey.Contains("Vgs"))
                {
                    if (value > 0)
                    {
                        allFaultSignalKeys.Add(signalKey);
                        faultDescriptions[signalKey] = "VGS故障";
                    }
                    else
                    {
                        _faultManager.ClearFaultStatus(signalKey);
                    }
                }
                else if (signalKey.Contains("InternalStatus"))
                {
                    int status = (int)value;
                    if (status != 0)
                    {
                        allFaultSignalKeys.Add(signalKey);
                        faultDescriptions[signalKey] = GetInternalStatusDisplay(status);
                    }
                    else
                    {
                        _faultManager.ClearFaultStatus(signalKey);
                    }
                }
                else if (signalKey.Contains("ModuleCapacityState"))
                {
                    if (value > 0)
                    {
                        allFaultSignalKeys.Add(signalKey);
                        faultDescriptions[signalKey] = "容量故障";
                    }
                    else
                    {
                        _faultManager.ClearFaultStatus(signalKey);
                    }
                }
                else if (signalKey.Contains("InternalResistanceStatus"))
                {
                    if (value > 0)
                    {
                        allFaultSignalKeys.Add(signalKey);
                        faultDescriptions[signalKey] = "内阻异常";
                    }
                    else
                    {
                        _faultManager.ClearFaultStatus(signalKey);
                    }
                }
            }

            if (allFaultSignalKeys.Count > 0)
            {
                var logEntry = new FaultLogEntry();
                
                foreach (var signalKey in _signalValues.Keys)
                {
                    string displayName = GetSignalDisplayName(signalKey);
                    string displayValue = GetSignalDisplayValue(signalKey, _signalValues[signalKey]);
                    bool isFault = allFaultSignalKeys.Contains(signalKey);
                    
                    logEntry.DisplayValues[displayName] = displayValue;
                    logEntry.IsFaultColumn[displayName] = isFault;
                }

                _faultManager.RecordFaultWithDisplayNames(allFaultSignalKeys, logEntry);
                FaultCount = _faultManager.FaultCount;
            }
        }

        private string GetSignalDisplayName(string signalKey)
        {
            if (_signalDisplayNameMap.TryGetValue(signalKey, out string displayName))
            {
                return displayName;
            }
            return signalKey;
        }

        private string GetSignalDisplayValue(string signalKey, double value)
        {
            if (signalKey.Contains("BlanceState"))
            {
                if (value == -1.0) return "无";
                return value > 0 ? "是" : "否";
            }

            if (signalKey.Contains("OverVoltageState") || 
                signalKey.Contains("ShortCircuitState") || 
                signalKey.Contains("OverTempretureState") ||
                signalKey.Contains("FaultStatus") ||
                signalKey.Contains("Fault") ||
                signalKey.Contains("OpenCircuit") ||
                signalKey.Contains("Imbalance") ||
                signalKey.Contains("ChargeFault") ||
                signalKey.Contains("DisChargeFault") ||
                signalKey.Contains("ShortPower") ||
                signalKey.Contains("ShortGround") ||
                signalKey.Contains("Vds") ||
                signalKey.Contains("Vgs"))
            {
                if (value == -1.0) return "无";
                return value > 0 ? "故障" : "正常";
            }

            if (signalKey.Contains("MochainStatus"))
            {
                if (value == -1.0) return "无";
                return GetMochainStatusDisplay((int)value);
            }

            if (signalKey.Contains("InternalStatus"))
            {
                if (value == -1.0) return "无";
                return GetInternalStatusDisplay((int)value);
            }

            if (signalKey.Contains("CollisionSignalStatus"))
            {
                if (value == -1.0) return "无";
                return GetCollisionStatusDisplay((int)value);
            }

            if (_signalFormatMap.TryGetValue(signalKey, out string format))
            {
                return value.ToString(format);
            }

            if (value == Math.Truncate(value))
            {
                return value.ToString("F0");
            }
            return value.ToString("F1");
        }

        private string GetMochainStatusDisplay(int status)
        {
            switch (status)
            {
                case 0: return "放电";
                case 1: return "充电";
                case 2: return "静置";
                default: return "未知";
            }
        }

        private string GetInternalStatusDisplay(int status)
        {
            if (status == 0) return "正常";

            var statuses = new List<string>();
            if ((status & (1 << 0)) != 0) statuses.Add("SPI错误");
            if ((status & (1 << 1)) != 0) statuses.Add("SPI时钟错误");
            if ((status & (1 << 2)) != 0) statuses.Add("上电复位");
            if ((status & (1 << 3)) != 0) statuses.Add("nFAULT引脚故障");
            if ((status & (1 << 4)) != 0) statuses.Add("警告指示");
            if ((status & (1 << 5)) != 0) statuses.Add("DS/GS故障");
            if ((status & (1 << 6)) != 0) statuses.Add("欠压故障");
            if ((status & (1 << 7)) != 0) statuses.Add("过压故障");
            if ((status & (1 << 8)) != 0) statuses.Add("PVDD欠压");
            if ((status & (1 << 9)) != 0) statuses.Add("PVDD过压");
            if ((status & (1 << 10)) != 0) statuses.Add("VCP欠压");
            if ((status & (1 << 11)) != 0) statuses.Add("过温警告");
            if ((status & (1 << 12)) != 0) statuses.Add("过温关断");
            if ((status & (1 << 13)) != 0) statuses.Add("看门狗故障");
            if ((status & (1 << 14)) != 0) statuses.Add("复合警告");

            return string.Join("; ", statuses);
        }

        private string GetCollisionStatusDisplay(int status)
        {
            switch (status)
            {
                case 0: return "正常";
                case 1: return "PVDD过压";
                case 2: return "欠压";
                case 3: return "VCP欠压";
                case 4: return "SPI时钟异常";
                default: return "正常";
            }
        }

        public double GetSignalValue(string signalKey)
        {
            return _signalValues.ContainsKey(signalKey) ? _signalValues[signalKey] : 0;
        }
    }
}