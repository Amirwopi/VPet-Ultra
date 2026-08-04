using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;
using VPet.Cognition.MemoryStore;

namespace VPet.Cognition.Wpf.Views;

/// <summary>
/// Window showing stored memories with search and filtering.
/// </summary>
public partial class MemoryInspectorWindow : Window
{
    private readonly IMemoryStore _memoryStore;
    private readonly MemoryService _memoryService;

    public ObservableCollection<MemoryItem> Memories { get; } = new();

    public MemoryInspectorWindow(IMemoryStore memoryStore, MemoryService memoryService)
    {
        _memoryStore = memoryStore;
        _memoryService = memoryService;
        InitializeComponent();
        DataContext = this;

        Loaded += async (_, _) => await LoadAsync();
    }

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            _ = LoadAsync();
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            Memories.Clear();

            var query = new MemoryQuery
            {
                TopK = 100,
                Text = string.IsNullOrEmpty(_searchText) ? null : _searchText,
            };

            var memories = await _memoryStore.RetrieveAsync(query);
            foreach (var mem in memories)
            {
                Memories.Add(new MemoryItem
                {
                    Type = mem.Type.ToString(),
                    Content = mem.Content.Length > 80 ? mem.Content[..80] + "..." : mem.Content,
                    Importance = mem.Importance.Value,
                    Source = mem.Source ?? "",
                    CreatedAt = mem.CreatedAt.ToString("MM/dd HH:mm"),
                });
            }

            CountText.Text = $"{Memories.Count} memories";
        }
        catch { /* ignore */ }
    }

    private void InitializeComponent()
    {
        Title = "Memory Inspector";
        Width = 700;
        Height = 500;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));

        var grid = new Grid { Margin = new Thickness(10) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // Search bar
        var searchPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
        var searchLabel = new TextBlock
        {
            Text = "Search: ",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
        };
        searchPanel.Children.Add(searchLabel);

        var searchBox = new TextBox
        {
            Width = 300,
            Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
            Foreground = Brushes.White,
        };
        searchBox.TextChanged += (_, _) => SearchText = searchBox.Text;
        searchPanel.Children.Add(searchBox);

        var refreshBtn = new Button
        {
            Content = "Refresh",
            Margin = new Thickness(10, 0, 0, 0),
            Padding = new Thickness(10, 2, 10, 2),
        };
        refreshBtn.Click += async (_, _) => await LoadAsync();
        searchPanel.Children.Add(refreshBtn);

        Grid.SetRow(searchPanel, 0);
        grid.Children.Add(searchPanel);

        // Count
        CountText = new TextBlock
        {
            Foreground = Brushes.LightGray,
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 5),
        };
        Grid.SetRow(CountText, 1);
        grid.Children.Add(CountText);

        // Memory list
        var listView = new ListView
        {
            Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
            Foreground = Brushes.White,
            ItemsSource = Memories,
        };
        var gridView = new GridView();
        gridView.Columns.Add(new GridViewColumn { Header = "Type", DisplayMemberBinding = new Binding("Type"), Width = 80 });
        gridView.Columns.Add(new GridViewColumn { Header = "Content", DisplayMemberBinding = new Binding("Content"), Width = 350 });
        gridView.Columns.Add(new GridViewColumn { Header = "Imp.", DisplayMemberBinding = new Binding("ImportanceText"), Width = 50 });
        gridView.Columns.Add(new GridViewColumn { Header = "Source", DisplayMemberBinding = new Binding("Source"), Width = 80 });
        gridView.Columns.Add(new GridViewColumn { Header = "Created", DisplayMemberBinding = new Binding("CreatedAt"), Width = 80 });
        listView.View = gridView;

        Grid.SetRow(listView, 2);
        grid.Children.Add(listView);

        Content = grid;
    }

    private TextBlock CountText = null!;
}

public sealed class MemoryItem
{
    public string Type { get; set; } = "";
    public string Content { get; set; } = "";
    public double Importance { get; set; }
    public string ImportanceText => Importance.ToString("F2");
    public string Source { get; set; } = "";
    public string CreatedAt { get; set; } = "";
}