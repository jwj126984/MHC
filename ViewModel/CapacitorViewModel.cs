using MHC.ViewModel;

namespace MHC.ViewModel
{
    public class CapacitorViewModel : ViewModelBase
    {
        private MainViewModel _mainViewModel;

        public CapacitorViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        // 电容模组综合监控信号
        public double ModuleSOC
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_ModulePower"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_ModulePower", value); }
        }

        public double ModuleVoltage
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_ModuleVoltage"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_ModuleVoltage", value); }
        }

        public double ModuleCurrent
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_ModuleCurrent"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_ModuleCurrent", value); }
        }

        public double ModuleDischargeAvgCurrent
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_DisChargeCurrent"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_DisChargeCurrent", value); }
        }

        public double ModuleStatus
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_MochainStatus"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_MochainStatus", value); }
        }

        // 模组告警监控信号
        public double ChargeFault
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_ChargeFault"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_ChargeFault", value); }
        }

        public double DischargeFault
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_DisChargeFault"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_DisChargeFault", value); }
        }

        // 唤醒源监控信号
        public double CanWakeup
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_SleepStatus"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_SleepStatus", value); }
        }

        public double KL15Wakeup
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_KL15Voltage"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_KL15Voltage", value); }
        }
    }
}