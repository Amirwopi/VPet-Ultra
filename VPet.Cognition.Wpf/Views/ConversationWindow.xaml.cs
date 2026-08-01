using System.Windows;
using System.Windows.Input;
using VPet.Cognition.Wpf.ViewModels;

namespace VPet.Cognition.Wpf.Views;

/// <summary>
/// Code-behind for the conversation window.
/// </summary>
public partial class ConversationWindow
{
    public ConversationWindow(ConversationVM viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
        {
            if (DataContext is ConversationVM vm && vm.SendCommand.CanExecute(null))
            {
                vm.SendCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}