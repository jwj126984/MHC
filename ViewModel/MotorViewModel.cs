using MHC.ViewModel;

namespace MHC.ViewModel
{
    public class MotorViewModel : ViewModelBase
    {
        private MainViewModel _mainViewModel;

        public MotorViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        // LF前门电机信号
        public double LFMotorShortPower
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_DirverMotorLFFault"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_DirverMotorLFFault", value); }
        }

        public double LFMotorShortGround
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_DirverMotorLFFault"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_DirverMotorLFFault", value); }
        }

        public double LFMotorOpenCircuit
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_DirverMotorLFFault"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_DirverMotorLFFault", value); }
        }

        // RF前门电机信号
        public double RFMotorShortPower
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_DirverMotorRFFault"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_DirverMotorRFFault", value); }
        }

        public double RFMotorShortGround
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_DirverMotorRFFault"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_DirverMotorRFFault", value); }
        }

        public double RFMotorOpenCircuit
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_DirverMotorRFFault"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_DirverMotorRFFault", value); }
        }

        // LR后门电机信号
        public double LRMotorShortPower
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_DirverMotorLRFault"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_DirverMotorLRFault", value); }
        }

        public double LRMotorShortGround
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_DirverMotorLRFault"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_DirverMotorLRFault", value); }
        }

        public double LRMotorOpenCircuit
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_DirverMotorLRFault"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_DirverMotorLRFault", value); }
        }

        // RR后门电机信号
        public double RRMotorShortPower
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_DirverMotorRRFault"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_DirverMotorRRFault", value); }
        }

        public double RRMotorShortGround
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_DirverMotorRRFault"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_DirverMotorRRFault", value); }
        }

        public double RRMotorOpenCircuit
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_DirverMotorRRFault"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_DirverMotorRRFault", value); }
        }

        // LR儿童锁电机信号
        public double LRChildLockMotorShortPower
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_DirverMotorLRChildFault"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_DirverMotorLRChildFault", value); }
        }

        public double LRChildLockMotorShortGround
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_DirverMotorLRChildFault"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_DirverMotorLRChildFault", value); }
        }

        public double LRChildLockMotorOpenCircuit
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_DirverMotorLRChildFault"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_DirverMotorLRChildFault", value); }
        }

        // RR儿童锁电机信号
        public double RRChildLockMotorShortPower
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_DirverMotorRRChildFault"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_DirverMotorRRChildFault", value); }
        }

        public double RRChildLockMotorShortGround
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_DirverMotorRRChildFault"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_DirverMotorRRChildFault", value); }
        }

        public double RRChildLockMotorOpenCircuit
        {
            get { return _mainViewModel.GetSignalValue("SuperCapController_Cap_DirverMotorRRChildFault"); }
            set { _mainViewModel.UpdateSignalValue("SuperCapController_Cap_DirverMotorRRChildFault", value); }
        }
    }
}