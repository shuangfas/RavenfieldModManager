using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ModManagerGUI.Services;

namespace ModManagerGUI.Views;

public partial class SettingsPage : UserControl {
    public SettingsPage() {
        InitializeComponent();
        // 回显已保存的开关状态（MainWindow 构造时已先执行 CommonConfig.Load）
        HtmlRenderToggle.IsChecked = MainWindow.CommonConfig.Current.EnableHtmlRendering;
    }

    private void LanguageButton_Click(object sender, RoutedEventArgs e) {
        MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
        LocalizationService.Toggle();
        mainWindow._modsViewModel.OnLanguageChanged();
    }

    private void ToggleButton_OnChecked(object sender, RoutedEventArgs e) {
        if (sender is ToggleButton toggleButton) {
            MainWindow.CommonConfig.Update(p =>p.EnableHtmlRendering = toggleButton.IsChecked == true);
            // 同步到模组页：切换 HtmlPanel / TextBlock 并重新生成描述
            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow._modsViewModel.EnableHtmlRendering = MainWindow.CommonConfig.Current.EnableHtmlRendering;
        }
    }
}