using System.Windows;
using System.Windows.Controls;
using ModManagerGUI.Services;

namespace ModManagerGUI.Views;

public partial class SettingsPage : UserControl {
    public SettingsPage() {
        InitializeComponent();
    }

    private void LanguageButton_Click(object sender, RoutedEventArgs e) {
        MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
        LocalizationService.Toggle();
        mainWindow._modsViewModel.OnLanguageChanged();
    }
}