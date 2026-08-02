using System.Windows;
using VPet.Cognition.Wpf.ViewModels;

namespace VPet.Cognition.Wpf.Views;

/// <summary>
/// Code-behind for the cognition settings window.
/// </summary>
public partial class CognitionSettingsWindow
{
    public CognitionSettingsWindow(CognitionSettingsVM viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is CognitionSettingsVM vm)
        {
            await vm.TestConnectionPublicAsync();
        }
    }
}