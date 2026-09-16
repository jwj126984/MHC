using System.Windows;
using System.Windows.Controls;
using MHC.ViewModel;

namespace MHC
{
    public partial class PasswordDialog : Window
    {
        public bool IsAuthenticated { get; private set; }
        public string EnteredPassword { get; private set; }

        private MainViewModel _viewModel;
        private TextBox _visibleTextBox;

        public PasswordDialog()
        {
            InitializeComponent();
            IsAuthenticated = false;
            EnteredPassword = string.Empty;
        }

        public PasswordDialog(MainViewModel viewModel) : this()
        {
            _viewModel = viewModel;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            EnteredPassword = ReadCurrentPassword();

            if (string.IsNullOrEmpty(EnteredPassword))
            {
                ShowError("请输入密码");
                return;
            }

            if (_viewModel != null)
            {
                if (_viewModel.VerifyPassword(EnteredPassword))
                {
                    // 校验通过，关闭对话框
                    IsAuthenticated = true;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    // 校验失败：保持对话框打开，清空输入并提示错误
                    IsAuthenticated = false;
                    ShowError("密码错误，请重新输入");
                    ResetInputToPasswordMode();
                }
            }
            else
            {
                // 未注入 ViewModel 时退回到原行为
                IsAuthenticated = !string.IsNullOrEmpty(EnteredPassword);
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsAuthenticated = false;
            DialogResult = false;
            Close();
        }

        public void ShowError(string message)
        {
            ErrorMessage.Text = message;
        }

        private void ResetInputToPasswordMode()
        {
            if (_visibleTextBox != null)
            {
                // 当前是明文模式：先清空容器再放回密文 PasswordBox
                PasswordContainer.Children.Clear();
                _visibleTextBox = null;

                PasswordBox box = new PasswordBox
                {
                    Height = 30,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                box.PasswordChanged += PasswordInput_PasswordChanged;
                box.Password = string.Empty;

                PasswordInput = box;
                PasswordContainer.Children.Add(box);

                ToggleVisibilityButton.Content = "显示";
                HintMessage.Text = "提示：点击“显示”可查看当前输入";
            }
            else
            {
                // 当前已经是密文模式，仅清空已有内容
                PasswordInput.Password = string.Empty;
            }
        }

        private void ToggleVisibilityButton_Click(object sender, RoutedEventArgs e)
        {
            if (_visibleTextBox == null)
            {
                // 切换为显示明文
                string current = PasswordInput.Password;
                PasswordContainer.Children.Clear();

                _visibleTextBox = new TextBox
                {
                    Height = 30,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Text = current
                };
                _visibleTextBox.TextChanged += VisibleTextBox_TextChanged;
                PasswordContainer.Children.Add(_visibleTextBox);

                ToggleVisibilityButton.Content = "隐藏";
                HintMessage.Text = "提示：密码以明文显示，输入完成后可点击“隐藏”";
            }
            else
            {
                // 切换回密文
                string current = _visibleTextBox.Text;
                PasswordContainer.Children.Clear();
                _visibleTextBox = null;

                PasswordBox box = new PasswordBox
                {
                    Height = 30,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                box.PasswordChanged += PasswordInput_PasswordChanged;
                box.Password = current;

                PasswordInput = box;
                PasswordContainer.Children.Add(box);

                ToggleVisibilityButton.Content = "显示";
                HintMessage.Text = "提示：点击“显示”可查看当前输入";
            }
        }

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ErrorMessage.Text))
            {
                ErrorMessage.Text = string.Empty;
            }
        }

        private void VisibleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ErrorMessage.Text))
            {
                ErrorMessage.Text = string.Empty;
            }
        }

        private string ReadCurrentPassword()
        {
            if (_visibleTextBox != null)
            {
                return _visibleTextBox.Text ?? string.Empty;
            }
            return PasswordInput.Password ?? string.Empty;
        }
    }
}
