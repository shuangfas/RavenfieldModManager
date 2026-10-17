using System.Windows;
using System.Windows.Input;
using ModManagerGUI.ViewModels;

namespace ModManagerGUI.Views;

public partial class RenamePlaysetDialog : Wpf.Ui.Controls.FluentWindow {
    private readonly PlaysetViewModel _vm;

    public RenamePlaysetDialog(PlaysetViewModel vm) {
        InitializeComponent();
        _vm = vm;
        Loaded += (_, _) => NameBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e) => TryCreate();

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

    /// <summary>回车确认</summary>
    private void NameBox_KeyDown(object sender, KeyEventArgs e) {
        if (e.Key == Key.Enter)
            TryCreate();
    }

    private void TryCreate() {
        if (_vm.RenamePlayset(NameBox.Text)) {
            Close();
        }
        else {
            // 名称为空或与现有播放集重名
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}