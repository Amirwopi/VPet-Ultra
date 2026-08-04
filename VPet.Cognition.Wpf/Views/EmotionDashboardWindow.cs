using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.ValueObjects;

namespace VPet.Cognition.Wpf.Views;

/// <summary>
/// Window showing the companion's current emotion state as a 10-dimensional bar chart.
/// </summary>
public partial class EmotionDashboardWindow : Window
{
    private readonly IEmotionEngine _emotionEngine;
    private readonly System.Windows.Threading.DispatcherTimer _timer;

    public ObservableCollection<EmotionBar> Bars { get; } = new();

    public EmotionDashboardWindow(IEmotionEngine emotionEngine)
    {
        _emotionEngine = emotionEngine;
        InitializeComponent();
        DataContext = this;

        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _timer.Tick += async (_, _) => await UpdateAsync();
        _timer.Start();

        Loaded += async (_, _) => await UpdateAsync();
        Closed += (_, _) => _timer.Stop();
    }

    private async Task UpdateAsync()
    {
        try
        {
            var state = await _emotionEngine.GetAsync();
            var v = state.Vector;

            Bars.Clear();
            Bars.Add(new EmotionBar("Joy", v.Joy));
            Bars.Add(new EmotionBar("Sadness", v.Sadness));
            Bars.Add(new EmotionBar("Anger", v.Anger));
            Bars.Add(new EmotionBar("Curiosity", v.Curiosity));
            Bars.Add(new EmotionBar("Excitement", v.Excitement));
            Bars.Add(new EmotionBar("Fear", v.Fear));
            Bars.Add(new EmotionBar("Calmness", v.Calmness));
            Bars.Add(new EmotionBar("Surprise", v.Surprise));
            Bars.Add(new EmotionBar("Stress", v.Stress));
            Bars.Add(new EmotionBar("Frustration", v.Frustration));

            DominantText.Text = $"Dominant: {state.Dominant} | Arousal: {state.Arousal:F2} | Valence: {state.Valence:F2}";
        }
        catch { /* ignore */ }
    }

    private void InitializeComponent()
    {
        Title = "Emotion Dashboard";
        Width = 400;
        Height = 500;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));

        var grid = new Grid { Margin = new Thickness(10) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // Header
        DominantText = new TextBlock
        {
            Foreground = Brushes.White,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 10),
        };
        Grid.SetRow(DominantText, 0);
        grid.Children.Add(DominantText);

        // List of bars
        var listBox = new ListBox
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            ItemsSource = Bars,
            ItemTemplate = CreateBarTemplate(),
        };
        Grid.SetRow(listBox, 1);
        grid.Children.Add(listBox);

        Content = grid;
    }

    private DataTemplate CreateBarTemplate()
    {
        var template = new DataTemplate(typeof(EmotionBar));

        var stackPanel = new FrameworkElementFactory(typeof(StackPanel));
        stackPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        stackPanel.SetValue(StackPanel.MarginProperty, new Thickness(0, 2, 0, 2));

        // Label
        var label = new FrameworkElementFactory(typeof(TextBlock));
        label.SetValue(TextBlock.TextProperty, new Binding("Name"));
        label.SetValue(TextBlock.ForegroundProperty, Brushes.White);
        label.SetValue(TextBlock.WidthProperty, 100.0);
        label.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        stackPanel.AppendChild(label);

        // Bar
        var progressBar = new FrameworkElementFactory(typeof(ProgressBar));
        progressBar.SetValue(ProgressBar.ValueProperty, new Binding("Value"));
        progressBar.SetValue(ProgressBar.MaximumProperty, 1.0);
        progressBar.SetValue(ProgressBar.WidthProperty, 200.0);
        progressBar.SetValue(ProgressBar.HeightProperty, 20.0);
        progressBar.SetValue(ProgressBar.MarginProperty, new Thickness(5, 0, 0, 0));
        stackPanel.AppendChild(progressBar);

        // Value text
        var valueText = new FrameworkElementFactory(typeof(TextBlock));
        valueText.SetValue(TextBlock.TextProperty, new Binding("ValueText"));
        valueText.SetValue(TextBlock.ForegroundProperty, Brushes.LightGray);
        valueText.SetValue(TextBlock.WidthProperty, 50.0);
        valueText.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        valueText.SetValue(TextBlock.MarginProperty, new Thickness(5, 0, 0, 0));
        stackPanel.AppendChild(valueText);

        template.VisualTree = stackPanel;
        return template;
    }

    private TextBlock DominantText = null!;
}

public sealed class EmotionBar
{
    public string Name { get; }
    public double Value { get; }
    public string ValueText => Value.ToString("F2");
    public EmotionBar(string name, double value) { Name = name; Value = value; }
}