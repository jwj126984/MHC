using System.Configuration;
using System.Data;
using System.Windows;

namespace MHC
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 显示CAN配置窗口
            var configWindow = new CANConfigWindow();
            configWindow.OnConfigSuccess += (canCommunication) =>
            {
                // 配置成功，打开主窗口
                var mainWindow = new MainWindow(canCommunication);
                mainWindow.Show();
            };
            configWindow.OnCancel += () =>
            {
                // 取消配置，退出应用
                this.Shutdown();
            };
            configWindow.Show();
        }
    }

}
