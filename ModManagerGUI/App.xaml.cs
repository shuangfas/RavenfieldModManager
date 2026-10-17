using System.Windows;
using System.Windows.Threading;
using ModManagerGUI.Base;
using MessageBox = Wpf.Ui.Controls.MessageBox;

namespace ModManagerGUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (e.Exception is UnavailableException ex)
        {
            e.Handled = true;                       // 不再冒泡到"未处理异常"崩溃
            HandleUnavailable(ex.Message);
            return;
        }
        // 其他异常：按你原来的方式处理（日志 + 崩溃提示）
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (e.Exception.InnerException is UnavailableException ex)
        {
            e.SetObserved();
            Dispatcher.Invoke(() => HandleUnavailable(ex.Message));
        }
    }
    private void HandleUnavailable(string message)
    {
        var messageBox = new MessageBox();
        messageBox.Title = "Error";
        messageBox.Content = message;
        messageBox.CloseButtonText = "Exit";
        var result =  messageBox.ShowDialogAsync();
    }
}
