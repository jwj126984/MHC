using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using MHC.ViewModel;

namespace MHC
{
    public partial class SettingsWindow : Window
    {
        private MainViewModel _viewModel;
        private TextBox _newPasswordVisibleTextBox;

        public SettingsWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            CapacityMinInput.Text = _viewModel.CapacityMinThreshold.ToString("F1");
            CapacityMaxInput.Text = _viewModel.CapacityMaxThreshold.ToString("F1");
            VoltageDiffInput.Text = _viewModel.VoltageDiffThreshold.ToString("F2");
            ResistanceThresholdInput.Text = _viewModel.InternalResistanceThreshold.ToString("F0");
            TemperatureThresholdInput.Text = _viewModel.OverTemperatureThreshold.ToString("F1");
            DischargeCurrentInput.Text = _viewModel.DischargeCurrentThreshold.ToString("F0");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder errors = new StringBuilder();
            bool hasError = false;

            if (double.TryParse(CapacityMinInput.Text, out double capacityMin))
            {
                _viewModel.CapacityMinThreshold = capacityMin;
            }
            else { errors.AppendLine("容值最小值输入无效"); hasError = true; }

            if (double.TryParse(CapacityMaxInput.Text, out double capacityMax))
            {
                _viewModel.CapacityMaxThreshold = capacityMax;
            }
            else { errors.AppendLine("容值最大值输入无效"); hasError = true; }

            if (double.TryParse(VoltageDiffInput.Text, out double voltageDiff))
            {
                _viewModel.VoltageDiffThreshold = voltageDiff;
            }
            else { errors.AppendLine("均衡电压差输入无效"); hasError = true; }

            if (double.TryParse(ResistanceThresholdInput.Text, out double resistanceThreshold))
            {
                _viewModel.InternalResistanceThreshold = resistanceThreshold;
            }
            else { errors.AppendLine("内阻门限输入无效"); hasError = true; }

            if (double.TryParse(TemperatureThresholdInput.Text, out double temperatureThreshold))
            {
                _viewModel.OverTemperatureThreshold = temperatureThreshold;
            }
            else { errors.AppendLine("过温阈值输入无效"); hasError = true; }

            if (double.TryParse(DischargeCurrentInput.Text, out double dischargeCurrent))
            {
                _viewModel.DischargeCurrentThreshold = dischargeCurrent;
            }
            else { errors.AppendLine("放电电流阈值输入无效"); hasError = true; }

            if (hasError)
            {
                MessageText.Text = errors.ToString();
                return;
            }

            _viewModel.SaveSettings();
            MessageBox.Show("参数保存成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string newPassword = ReadNewPassword();

            if (string.IsNullOrEmpty(newPassword))
            {
                MessageText.Text = "请输入新密码";
                return;
            }

            if (newPassword.Length < 4)
            {
                MessageText.Text = "新密码长度至少4位";
                return;
            }

            try
            {
                _viewModel.ChangePassword(newPassword);
                _viewModel.SaveSettings();

                // 修改成功后清空输入
                ClearNewPasswordInput();
                MessageText.Text = "密码修改成功！";

                MessageBox.Show("密码修改成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageText.Text = "密码修改失败：" + ex.Message;
            }
        }

        private void ToggleNewPasswordVisibilityButton_Click(object sender, RoutedEventArgs e)
        {
            if (_newPasswordVisibleTextBox == null)
            {
                // 切换为显示明文
                string current = NewPasswordInput.Password;
                NewPasswordContainer.Children.Clear();

                _newPasswordVisibleTextBox = new TextBox
                {
                    Height = 28,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Text = current
                };
                _newPasswordVisibleTextBox.TextChanged += NewPasswordVisibleTextChanged;
                NewPasswordContainer.Children.Add(_newPasswordVisibleTextBox);

                ToggleNewPasswordVisibilityButton.Content = "隐藏";
                PasswordHintText.Text = "提示：密码以明文显示，输入完成后可点击“隐藏”；点击“修改密码”立即更新。";
            }
            else
            {
                // 切换回密文
                string current = _newPasswordVisibleTextBox.Text;
                NewPasswordContainer.Children.Clear();
                _newPasswordVisibleTextBox = null;

                PasswordBox box = new PasswordBox
                {
                    Height = 28,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                box.PasswordChanged += NewPasswordInput_PasswordChanged;
                box.Password = current;

                NewPasswordInput = box;
                NewPasswordContainer.Children.Add(box);

                ToggleNewPasswordVisibilityButton.Content = "显示";
                PasswordHintText.Text = "提示：输入新密码后点击“修改密码”即可立即更新；也可点击“显示”查看输入内容。";
            }
        }

        private void NewPasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(MessageText.Text))
            {
                MessageText.Text = string.Empty;
            }
        }

        private void NewPasswordVisibleTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(MessageText.Text))
            {
                MessageText.Text = string.Empty;
            }
        }

        private string ReadNewPassword()
        {
            if (_newPasswordVisibleTextBox != null)
            {
                return _newPasswordVisibleTextBox.Text ?? string.Empty;
            }
            return NewPasswordInput.Password ?? string.Empty;
        }

        private void ClearNewPasswordInput()
        {
            if (_newPasswordVisibleTextBox != null)
            {
                _newPasswordVisibleTextBox.Text = string.Empty;
            }
            else
            {
                NewPasswordInput.Password = string.Empty;
            }
        }
    }
}
