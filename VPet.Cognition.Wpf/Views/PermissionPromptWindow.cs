using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VPet.Cognition.Domain.Entities;

namespace VPet.Cognition.Wpf.Views;

/// <summary>
/// Dialog window for prompting the user to grant or deny a tool permission.
/// </summary>
public partial class PermissionPromptWindow : Window
{
    public PermissionResult Result { get; private set; } = PermissionResult.Pending;

    private readonly ToolSchema _schema;
    private readonly string _argsPreview;

    public PermissionPromptWindow(ToolSchema schema, string argsPreview)
    {
        _schema = schema;
        _argsPreview = argsPreview;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Title = "Permission Required";
        Width = 450;
        Height = 250;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));

        var grid = new Grid { Margin = new Thickness(15) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Title
        var title = new TextBlock
        {
            Text = $"Tool: {_schema.Name}",
            Foreground = new SolidColorBrush(Color.FromRgb(255, 180, 100)),
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 5),
        };
        Grid.SetRow(title, 0);
        grid.Children.Add(title);

        // Description
        var desc = new TextBlock
        {
            Text = _schema.Description,
            Foreground = Brushes.White,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10),
        };
        Grid.SetRow(desc, 1);
        grid.Children.Add(desc);

        // Arguments preview
        var argsScroll = new ScrollViewer
        {
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            Margin = new Thickness(0, 0, 0, 10),
        };
        var argsText = new TextBlock
        {
            Text = $"Arguments:\n{_argsPreview}",
            Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
        };
        argsScroll.Content = argsText;
        Grid.SetRow(argsScroll, 2);
        grid.Children.Add(argsScroll);

        // Buttons
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
        };

        var denyBtn = new Button
        {
            Content = "Deny",
            Padding = new Thickness(20, 5, 20, 5),
            Margin = new Thickness(0, 0, 10, 0),
            Background = new SolidColorBrush(Color.FromRgb(180, 60, 60)),
            Foreground = Brushes.White,
        };
        denyBtn.Click += (_, _) => { Result = PermissionResult.Denied; DialogResult = false; };
        buttonPanel.Children.Add(denyBtn);

        var allowOnceBtn = new Button
        {
            Content = "Allow Once",
            Padding = new Thickness(15, 5, 15, 5),
            Margin = new Thickness(0, 0, 10, 0),
        };
        allowOnceBtn.Click += (_, _) => { Result = PermissionResult.AllowedOnce; DialogResult = true; };
        buttonPanel.Children.Add(allowOnceBtn);

        var allowAlwaysBtn = new Button
        {
            Content = "Allow Always",
            Padding = new Thickness(15, 5, 15, 5),
            Background = new SolidColorBrush(Color.FromRgb(60, 160, 60)),
            Foreground = Brushes.White,
        };
        allowAlwaysBtn.Click += (_, _) => { Result = PermissionResult.AllowedAlways; DialogResult = true; };
        buttonPanel.Children.Add(allowAlwaysBtn);

        Grid.SetRow(buttonPanel, 3);
        grid.Children.Add(buttonPanel);

        Content = grid;
    }
}

public enum PermissionResult
{
    Pending,
    AllowedOnce,
    AllowedAlways,
    Denied,
}