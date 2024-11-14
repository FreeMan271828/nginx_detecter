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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Windows.Threading;

namespace NginxDetector;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private DispatcherTimer _timer;
    private double interval = 0.5;
    
    public MainWindow()
    {
        InitializeComponent();
        if (!IsRunningAsAdministrator())
        {
            MessageBox.Show("Please run the application as an administrator.", "Administrator Rights Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            Application.Current.Shutdown();
        }
        else
        {
            LoadNginxThreads();
            StartTimer(); // 初始化计时器
        }
    }
    
    private void StartTimer()
    {
        _timer = new DispatcherTimer(); // 创建计时器
        _timer.Interval = TimeSpan.FromSeconds( interval); // 设置间隔为1秒
        _timer.Tick += Timer_Tick; // 绑定Tick事件
        _timer.Start(); // 启动计时器
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        LoadNginxThreads(); // 每次计时器触发时调用LoadNginxThreads方法
    }
    
    private void RestartAsAdministrator()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = Application.ResourceAssembly.Location,
            UseShellExecute = true,
            Verb = "runas"
        };

        try
        {
            Process.Start(startInfo);
        }
        catch
        {
            MessageBox.Show("The application could not be restarted as an administrator.", "Administrator Rights Required", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
        }
    }
    
    private bool IsRunningAsAdministrator()
    {
        WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
    
    private void LoadNginxThreads()
    {
        var nginxThreads = GetNginxThreads();
        nginxThreadsGrid.ItemsSource = nginxThreads;
    }
    
    private void DeleteThread_Click(object sender, RoutedEventArgs e)
    {
        // 获取触发事件的按钮
        var btn = sender as Button;
        if(btn == null) {}
        // 从按钮的Tag属性中获取线程的PID
        var threadId = Convert.ToInt32(btn.Tag);
        // 调用方法删除线程
        DeleteNginxThread(threadId);
    }

    private void DeleteNginxThread(int threadId)
    {
        try
        {
            // 构建用于终止进程的系统命令
            string command = $"taskkill /F /PID {threadId}";
            // 执行命令
            Process.Start("cmd.exe", $"/c {command}");
        }
        catch (Exception ex)
        {
            // 处理异常，例如打印错误消息
            MessageBox.Show($"Error deleting thread: {ex.Message}");
        }
    }

    public List<NginxThreadInfo> GetNginxThreads()
    {
        var nginxThreads = new List<NginxThreadInfo>();
        var processes = Process.GetProcesses();

        foreach (var process in processes)
        {
            // 检查进程名称是否包含“nginx”
            if (process.ProcessName.ToLower().Contains("nginx")&&
                !process.ProcessName.Equals("NginxDetector"))
            {
                try
                {
                    var threadInfo = new NginxThreadInfo
                    {
                        Name = process.ProcessName,
                        Pid = process.Id,
                        Status = process.Responding ? "Running" : "Not Responding",
                        Description = process.MainModule.FileVersionInfo.FileDescription
                    };
                    nginxThreads.Add(threadInfo);
                }
                catch (Exception ex)
                {
                    // 处理异常，例如访问被拒绝
                    Console.WriteLine($"Error accessing process {process.Id}: {ex.Message}");
                }
            }
        }

        return nginxThreads;
    }

}