using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;

namespace VPet.Cognition.Wpf.Views;

/// <summary>
/// Window showing the companion's personality state: Big Five traits, adaptive traits,
/// relationship, communication style, and preferences.
/// </summary>
public partial class PersonalityWindow : Window
{
    private readonly IPersonalityEngine _personalityEngine;

    public ObservableCollection<TraitBar> CoreTraits { get; } = new();
    public ObservableCollection<TraitBar> AdaptiveTraits { get; } = new();
    public ObservableCollection<PreferenceBar> Preferences { get; } = new();

    public PersonalityWindow(IPersonalityEngine personalityEngine)
    {
        _personalityEngine = personalityEngine;
        InitializeComponent();
        DataContext = this;

        Loaded += async (_, _) => await UpdateAsync();
    }

    private async Task UpdateAsync()
    {
        try
        {
            var p = await _personalityEngine.GetAsync();

            // Identity
            NameText.Text = $"Name: {p.Identity.Name}";
            DescText.Text = $"Description: {p.Identity.Description}";
            EvolutionText.Text = $"Evolution count: {p.EvolutionCount} | Last evolved: {p.LastEvolvedAt:HH:mm:ss}";

            // Core traits (Big Five)
            CoreTraits.Clear();
            CoreTraits.Add(new TraitBar("Openness", p.CoreTraits.Openness));
            CoreTraits.Add(new TraitBar("Conscientiousness", p.CoreTraits.Conscientiousness));
            CoreTraits.Add(new TraitBar("Extraversion", p.CoreTraits.Extraversion));
            CoreTraits.Add(new TraitBar("Agreeableness", p.CoreTraits.Agreeableness));
            CoreTraits.Add(new TraitBar("Neuroticism", p.CoreTraits.Neuroticism));

            // Adaptive traits
            AdaptiveTraits.Clear();
            AdaptiveTraits.Add(new TraitBar("Confidence", p.AdaptiveTraits.Confidence));
            AdaptiveTraits.Add(new TraitBar("Curiosity", p.AdaptiveTraits.Curiosity));
            AdaptiveTraits.Add(new TraitBar("Creativity", p.AdaptiveTraits.Creativity));
            AdaptiveTraits.Add(new TraitBar("Playfulness", p.AdaptiveTraits.Playfulness));
            AdaptiveTraits.Add(new TraitBar("Assertiveness", p.AdaptiveTraits.Assertiveness));
            AdaptiveTraits.Add(new TraitBar("Patience", p.AdaptiveTraits.Patience));
            AdaptiveTraits.Add(new TraitBar("Warmth", p.AdaptiveTraits.Warmth));

            // Relationship
            TrustText.Text = $"Trust: {p.Relationship.Trust:F2}";
            FamText.Text = $"Familiarity: {p.Relationship.Familiarity:F2}";
            AffText.Text = $"Affection: {p.Relationship.Affection:F2}";

            // Preferences
            Preferences.Clear();
            foreach (var (key, value) in p.Preferences)
                Preferences.Add(new PreferenceBar(key, value));
        }
        catch { /* ignore */ }
    }

    private void InitializeComponent()
    {
        Title = "Personality";
        Width = 500;
        Height = 600;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));

        var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var stackPanel = new StackPanel { Margin = new Thickness(10) };

        // Identity section
        NameText = CreateTextBlock(16, true);
        DescText = CreateTextBlock(12, false);
        EvolutionText = CreateTextBlock(11, false);
        stackPanel.Children.Add(NameText);
        stackPanel.Children.Add(DescText);
        stackPanel.Children.Add(EvolutionText);
        stackPanel.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 10) });

        // Core traits
        stackPanel.Children.Add(CreateHeader("Core Traits (Big Five)"));
        var coreList = CreateTraitList(CoreTraits);
        stackPanel.Children.Add(coreList);

        // Adaptive traits
        stackPanel.Children.Add(CreateHeader("Adaptive Traits"));
        var adaptiveList = CreateTraitList(AdaptiveTraits);
        stackPanel.Children.Add(adaptiveList);

        // Relationship
        stackPanel.Children.Add(CreateHeader("Relationship"));
        TrustText = CreateTextBlock(12, false);
        FamText = CreateTextBlock(12, false);
        AffText = CreateTextBlock(12, false);
        stackPanel.Children.Add(TrustText);
        stackPanel.Children.Add(FamText);
        stackPanel.Children.Add(AffText);

        // Preferences
        stackPanel.Children.Add(CreateHeader("Preferences"));
        var prefList = CreateTraitList(Preferences);
        stackPanel.Children.Add(prefList);

        scrollViewer.Content = stackPanel;
        Content = scrollViewer;
    }

    private TextBlock CreateHeader(string text)
    {
        var tb = new TextBlock
        {
            Text = text,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 180, 255)),
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 10, 0, 5),
        };
        return tb;
    }

    private TextBlock CreateTextBlock(int fontSize, bool bold)
    {
        return new TextBlock
        {
            Foreground = Brushes.White,
            FontSize = fontSize,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Margin = new Thickness(0, 2, 0, 2),
        };
    }

    private ListBox CreateTraitList(object itemsSource)
    {
        var list = new ListBox
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            ItemsSource = (System.Collections.IEnumerable)itemsSource,
            ItemTemplate = CreateBarTemplate(),
        };
        return list;
    }

    private DataTemplate CreateBarTemplate()
    {
        var template = new DataTemplate();
        var sp = new FrameworkElementFactory(typeof(StackPanel));
        sp.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        sp.SetValue(StackPanel.MarginProperty, new Thickness(0, 1, 0, 1));

        var label = new FrameworkElementFactory(typeof(TextBlock));
        label.SetValue(TextBlock.TextProperty, new Binding("Name"));
        label.SetValue(TextBlock.ForegroundProperty, Brushes.White);
        label.SetValue(TextBlock.WidthProperty, 130.0);
        label.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        sp.AppendChild(label);

        var bar = new FrameworkElementFactory(typeof(ProgressBar));
        bar.SetValue(ProgressBar.ValueProperty, new Binding("Value"));
        bar.SetValue(ProgressBar.MaximumProperty, 1.0);
        bar.SetValue(ProgressBar.WidthProperty, 200.0);
        bar.SetValue(ProgressBar.HeightProperty, 18.0);
        sp.AppendChild(bar);

        var val = new FrameworkElementFactory(typeof(TextBlock));
        val.SetValue(TextBlock.TextProperty, new Binding("ValueText"));
        val.SetValue(TextBlock.ForegroundProperty, Brushes.LightGray);
        val.SetValue(TextBlock.WidthProperty, 50.0);
        val.SetValue(TextBlock.MarginProperty, new Thickness(5, 0, 0, 0));
        val.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        sp.AppendChild(val);

        template.VisualTree = sp;
        return template;
    }

    private TextBlock NameText = null!;
    private TextBlock DescText = null!;
    private TextBlock EvolutionText = null!;
    private TextBlock TrustText = null!;
    private TextBlock FamText = null!;
    private TextBlock AffText = null!;
}

public sealed class TraitBar
{
    public string Name { get; }
    public double Value { get; }
    public string ValueText => Value.ToString("F2");
    public TraitBar(string name, double value) { Name = name; Value = value; }
}

public sealed class PreferenceBar
{
    public string Name { get; }
    public double Value { get; }
    public string ValueText => Value.ToString("F2");
    public PreferenceBar(string name, double value) { Name = name; Value = value; }
}