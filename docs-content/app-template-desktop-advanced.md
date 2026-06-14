# App Template — Avalonia Desktop: IDE-Grade Controls

Read this alongside `app-template-desktop.md` when the user asks for an IDE, code editor,
file tree, multi-panel layout, or any app that needs developer-tool-level UI complexity.

**Required usings for all code in this doc:**
```csharp
using Avalonia.Controls;
using Avalonia.Layout;      // Orientation, HorizontalAlignment, VerticalAlignment
using Avalonia.Media;       // SolidColorBrush, Color, Brushes
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
```

**CRITICAL — `CornerRadius` vs `Thickness` (CS0029):**
When creating controls in code-behind, these two look identical but are different types:
- `CornerRadius = new CornerRadius(8)` ← correct for rounded corners
- `Padding = new Thickness(10, 6)` ← correct for padding/margin/border thickness

Assigning `new Thickness(8)` to `CornerRadius` causes CS0029 and will not compile.
Always use `new CornerRadius(value)` for `CornerRadius` properties.

---

## 1. Syntax-highlighted code editor (AvaloniaEdit)

`AvaloniaEdit` is a full-featured code editor control for Avalonia — the same engine
behind JetBrains Rider's editor.

**CRITICAL — package name:** There are two NuGet packages. You MUST use `Avalonia.AvaloniaEdit`
(the modern Avalonia 11-compatible package). Do NOT use the legacy `AvaloniaEdit` package —
it tops out at `0.10.12` and will fail to restore when requested at `11.*`.

Add to the `.csproj` (alongside the standard Avalonia packages):

```xml
<PackageReference Include="Avalonia.AvaloniaEdit" Version="11.*" />
```

**CRITICAL — `App.axaml` MUST include two things or the editor renders invisible text:**

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="MyIde.App"
             RequestedThemeVariant="Dark">
  <Application.Styles>
    <FluentTheme />
    <StyleInclude Source="avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml" />
  </Application.Styles>
  <!-- ... resources ... -->
</Application>
```

1. `RequestedThemeVariant="Dark"` on `<Application>` — without this, `FluentTheme` defaults
   to light mode. AvaloniaEdit's internal `TextView` inherits its foreground color from the theme,
   so in light mode the text is rendered near-black. On a dark `Background` (e.g. `#1E1E1E`) the
   text is completely invisible even though the file was loaded correctly (the status bar updates,
   but the editor appears empty).

2. `<StyleInclude Source="avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml" />` — without
   this line, AvaloniaEdit does not load its control styles. The `TextEditor` renders as a blank
   rectangle regardless of content.

Both lines are **required**. Omitting either one causes files to appear empty after selection.

Use it in XAML — add the namespace at the top of the `<Window>` element:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:aedit="using:AvaloniaEdit"
        x:Class="MyIde.MainWindow" ...>

  <aedit:TextEditor Name="CodeEditor"
                    FontFamily="Cascadia Code,Consolas,Monospace"
                    FontSize="14"
                    ShowLineNumbers="True"
                    SyntaxHighlighting="C#"
                    Background="#1E1E1E"
                    Foreground="#D4D4D4"
                    Padding="8" />
</Window>
```

Set or read content in code-behind:

```csharp
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;

// set content
CodeEditor.Text = File.ReadAllText(filePath);

// change syntax highlighting at runtime
CodeEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(".cs");

// get current content
var code = CodeEditor.Text;

// get selected text
var selected = CodeEditor.SelectedText;
```

Built-in `SyntaxHighlighting` values (case-insensitive string or via extension):
`"C#"`, `"XML"`, `"HTML"`, `"JavaScript"`, `"Python"`, `"SQL"`, `"JSON"`, `"CSS"`,
`"PowerShell"`, `"Boo"`, `"TeX"`, `"MarkDown"`.
Use `HighlightingManager.Instance.GetDefinitionByExtension(".ts")` etc. for others.

---

## 2. IDE-style resizable multi-panel layout (GridSplitter)

A typical IDE layout: file tree on the left, editor in the center, AI panel on the right,
output/terminal at the bottom.

```xml
<Grid RowDefinitions="Auto,*,6,180" ColumnDefinitions="220,6,*,6,280">

  <!-- Toolbar row spans all columns -->
  <Border Grid.Row="0" Grid.ColumnSpan="5" Background="#2D2D2D" Padding="8,6">
    <StackPanel Orientation="Horizontal" Spacing="8">
      <Button Content="▶ Run" Background="#0E7C0E" Foreground="White" CornerRadius="4" Padding="10,4" />
      <Button Content="Open File…" CornerRadius="4" Padding="10,4" />
    </StackPanel>
  </Border>

  <!-- File tree -->
  <Border Grid.Row="1" Grid.Column="0" Background="#252526">
    <TreeView Name="FileTree" />
  </Border>
  <GridSplitter Grid.Row="1" Grid.Column="1" ResizeDirection="Columns"
                Background="#3C3C3C" />

  <!-- Code editor (center) -->
  <aedit:TextEditor Grid.Row="1" Grid.Column="2" Name="CodeEditor"
                    FontFamily="Cascadia Code,Consolas,Monospace"
                    FontSize="13" ShowLineNumbers="True"
                    SyntaxHighlighting="C#"
                    Background="#1E1E1E" Foreground="#D4D4D4" />
  <GridSplitter Grid.Row="1" Grid.Column="3" ResizeDirection="Columns"
                Background="#3C3C3C" />

  <!-- AI chat panel (right) -->
  <Border Grid.Row="1" Grid.Column="4" Background="#1E1E1E" Padding="8">
    <Grid RowDefinitions="*,Auto">
      <ScrollViewer Grid.Row="0" Name="AiScroll">
        <ItemsControl Name="AiMessages" />
      </ScrollViewer>
      <TextBox Grid.Row="1" Name="AiInput" Watermark="Ask the AI…" Margin="0,6,0,0" />
    </Grid>
  </Border>

  <!-- GridSplitter between editor rows and output row -->
  <GridSplitter Grid.Row="2" Grid.ColumnSpan="5" ResizeDirection="Rows"
                Background="#3C3C3C" />

  <!-- Output / terminal panel (bottom) -->
  <Border Grid.Row="3" Grid.ColumnSpan="5" Background="#0C0C0C" Padding="8">
    <ScrollViewer>
      <TextBlock Name="OutputText" FontFamily="Cascadia Code,Consolas,Monospace"
                 FontSize="12" Foreground="#CCCCCC" TextWrapping="Wrap" />
    </ScrollViewer>
  </Border>

</Grid>
```

`GridSplitter` resizes on drag automatically — no extra code needed.
- `ResizeDirection="Columns"` → vertical bar that resizes the columns on either side
- `ResizeDirection="Rows"` → horizontal bar that resizes rows

**`GridSplitter` is in `Avalonia.Controls`** — no extra NuGet package needed.
**`Orientation` used inside `StackPanel` is from `Avalonia.Layout`** — add `using Avalonia.Layout;`.

---

## 3. File tree with TreeView + TreeDataTemplate

**CRITICAL:** In Avalonia 11, `HierarchicalDataTemplate` was removed. The correct element is
`TreeDataTemplate`. Using `HierarchicalDataTemplate` causes AVLN2000 at compile time.
`TreeDataTemplate` is in the standard `https://github.com/avaloniaui` namespace — no extra prefix needed.

```csharp
// Model
public record FileNode(string Name, string FullPath, bool IsDirectory, List<FileNode> Children);

// Build the tree from a root directory
FileNode BuildTree(string dir) => new(
    Path.GetFileName(dir), dir, true,
    [.. Directory.GetDirectories(dir).Select(BuildTree),
     .. Directory.GetFiles(dir).Select(f => new FileNode(Path.GetFileName(f), f, false, []))]);
```

```xml
<TreeView Name="FileTree">
  <TreeView.ItemTemplate>
    <TreeDataTemplate DataType="{x:Type local:FileNode}"
                      ItemsSource="{Binding Children}">
      <StackPanel Orientation="Horizontal" Spacing="6">
        <TextBlock Text="{Binding IsDirectory, Converter={x:Static local:BoolToFolderIcon.Instance}}"
                   Foreground="#C8A84B" />
        <TextBlock Text="{Binding Name}" Foreground="#CCCCCC" />
      </StackPanel>
    </TreeDataTemplate>
  </TreeView.ItemTemplate>
</TreeView>
```

Simple icon converter (no extra package):

```csharp
public class BoolToFolderIcon : IValueConverter
{
    public static readonly BoolToFolderIcon Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "📁" : "📄";
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

Wire up file-open on selection in code-behind:

```csharp
FileTree.SelectionChanged += (_, _) =>
{
    if (FileTree.SelectedItem is FileNode { IsDirectory: false } node)
    {
        CodeEditor.Text = File.ReadAllText(node.FullPath);
        CodeEditor.SyntaxHighlighting =
            HighlightingManager.Instance.GetDefinitionByExtension(
                Path.GetExtension(node.FullPath));
    }
};
```

---

## 4. Multi-file tabs (TabControl over the editor)

Replace the single `TextEditor` with a `TabControl` to support multiple open files:

```xml
<TabControl Name="EditorTabs" Grid.Row="1" Grid.Column="2">
  <!-- tabs are added dynamically in code-behind -->
</TabControl>
```

```csharp
void OpenFileInTab(string filePath)
{
    var editor = new TextEditor
    {
        Text = File.ReadAllText(filePath),
        FontFamily = new FontFamily("Cascadia Code,Consolas,Monospace"),
        FontSize = 13,
        ShowLineNumbers = true,
        SyntaxHighlighting = HighlightingManager.Instance
            .GetDefinitionByExtension(Path.GetExtension(filePath)),
        Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
        Foreground = new SolidColorBrush(Color.Parse("#D4D4D4")),
    };

    var tab = new TabItem
    {
        Header = Path.GetFileName(filePath),
        Content = editor,
    };
    EditorTabs.Items.Add(tab);
    EditorTabs.SelectedItem = tab;
}
```

Access the active editor:

```csharp
var activeEditor = (EditorTabs.SelectedItem as TabItem)?.Content as TextEditor;
```

---

## 5. AI code assistant integration

Wire the AI panel so it receives the selected code (or full editor content) as context:

```csharp
async void OnAiSend(object? sender, RoutedEventArgs e)
{
    var question = AiInput.Text?.Trim();
    if (string.IsNullOrEmpty(question)) return;
    AiInput.Text = "";

    var ctx = CodeEditor.SelectedText.Length > 0
        ? CodeEditor.SelectedText
        : CodeEditor.Text;

    var prompt = string.IsNullOrWhiteSpace(ctx)
        ? question
        : $"{question}\n\n```csharp\n{ctx}\n```";

    var reply = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.White };
    // add `reply` to AiMessages panel ...

    MaINSetup.EnsureInitialized();
    var s = MaINSetup.Load();
    _agent ??= await AIHub.Agent()
        .WithModel(s.ModelName)
        .WithInitialPrompt("You are an expert C# code assistant. Analyze code, suggest improvements, and explain concepts clearly.")
        .CreateAsync();

    await _agent.ProcessAsync(prompt, tokenCallback: token =>
    {
        if (!string.IsNullOrEmpty(token?.Text))
            Dispatcher.UIThread.Post(() => reply.Text += token.Text);
        return Task.CompletedTask;
    });
}
```

---

## 6. Status bar (bottom strip)

```xml
<Border Background="#007ACC" Padding="8,3">
  <Grid ColumnDefinitions="*,Auto,Auto">
    <TextBlock Grid.Column="0" Name="StatusText" Text="Ready" Foreground="White" FontSize="11" />
    <TextBlock Grid.Column="1" Name="CursorPos"  Text="Ln 1, Col 1" Foreground="White"
               FontSize="11" Margin="0,0,16,0" />
    <TextBlock Grid.Column="2" Name="LangLabel"  Text="C#" Foreground="White" FontSize="11" />
  </Grid>
</Border>
```

Update cursor position live:

```csharp
CodeEditor.TextArea.Caret.PositionChanged += (_, _) =>
{
    var loc = CodeEditor.TextArea.Caret.Location;
    CursorPos.Text = $"Ln {loc.Line}, Col {loc.Column}";
};
```

---

## 7. Complete MainWindow.axaml.cs skeleton

This is the most critical section. Agents that only read snippets above often produce IDEs where
clicking a file does nothing or the AI throws "no API key" errors. Use this full skeleton — it
ties every section above into one working file.

**CRITICAL pitfalls that break generated IDEs:**

- **MUST add `RequestedThemeVariant="Dark"` to `<Application>` in `App.axaml`.** Without it,
  `FluentTheme` uses light-mode text color (near-black). The editor background is dark, so text
  is invisible. Files appear to load (status bar updates, SelectionChanged fires correctly) but
  the editor looks empty. This is the single most common reason file content doesn't show up.
- **MUST add `<StyleInclude Source="avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml" />`
  inside `<Application.Styles>`.** Without it, AvaloniaEdit's control template never loads and
  the `TextEditor` renders as a blank box.
- **DO NOT call `MaINBootstrapper.Initialize()` directly with no arguments.** It initializes with
  no backend configured, so every AI call fails. Always use `MaINSetup.EnsureInitialized()` from
  `app-template-desktop.md` — that pattern reads the saved settings and wires the correct backend.
- **DO NOT call `.WithSystemPrompt()` on `AIHub.Chat()`.** `IChatMessageBuilder` has no such method
  and it will not compile (CS1061). `AIHub.Chat()` has no separate system-prompt slot — embed the
  instruction directly in `.WithMessage()`: `.WithMessage("You are an analyst.\n\n" + userPrompt)`.
  Only `AIHub.Agent()` has `.WithInitialPrompt()` for a persistent system prompt.
- **DO NOT forget `required` members when initializing model classes.** If a property is declared
  `required`, every object initializer must set it or CS9035 is thrown. Provide a default (e.g.
  `Link = ""`) for fields that are legitimately absent in some cases.
- **DO NOT invent `ModelType` or `BackendProvider` enums.** `AIHub.Chat().WithModel(ModelType.OpenAi, model)`
  is a hallucinated API and will not compile. The correct call is `.WithModel(model)` where `model`
  is a plain string (e.g. `"gpt-4.1-mini"`). Backend selection goes through `MaINSetup`.
- **DO NOT use `AIHub.Chat()` for the AI assistant panel.** `Chat()` is one-shot (no memory).
  Use `AIHub.Agent()` so the assistant remembers the conversation history.
- **MUST set `FileTree.ItemsSource` in the constructor.** Without this line the TreeView
  appears but never populates, so there is nothing to click.
- **MUST set `FileTree.SelectionChanged` in the constructor.** The XAML can define the TreeView
  but the selection handler must be wired in code-behind, not in XAML, to access the typed `FileNode`.
- **DO NOT pass `Environment.SpecialFolder.UserProfile` to `BuildTree`.** The home directory contains
  tens of thousands of files. `BuildTree` runs synchronously on the UI thread in the constructor, so
  traversing the home directory blocks the UI thread before the window can render — the app appears
  to launch but the window never shows. Always use `AppDomain.CurrentDomain.BaseDirectory` as the
  initial path. Users can open any folder via the Open button.
- **DO NOT mix a XAML `ItemsControl.ItemTemplate` DataTemplate with manually-created `Border` objects
  added to `Items` in code-behind.** The DataTemplate wraps every item — when a `Border` is an item,
  `{Binding}` resolves to `Border.ToString()` which renders as `"Avalonia.Controls.Border"` text, not
  the styled bubble. Either (a) remove the `ItemTemplate` from XAML and add fully-styled `Border`
  objects from code (like the skeleton below), or (b) keep the DataTemplate and add only plain strings
  or ViewModels to the collection, never Controls.

```csharp
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using MaIN.Core.Hub;
using MaIN.Core.Hub.Contexts.Interfaces.AgentContext;

namespace MyIde;

public partial class MainWindow : Window
{
    // Field declarations — agents must include these or references below won't compile
    private IAgentContextExecutor? _agent;
    private string? _currentFilePath;

    public MainWindow()
    {
        InitializeComponent();

        // Load settings UI
        var s = MaINSetup.Load();
        BackendCombo.SelectedIndex = s.BackendType switch
        {
            "Gemini"    => 1,
            "Anthropic" => 2,
            "Ollama"    => 3,
            _           => 0
        };
        ModelBox.Text  = s.ModelName;
        ApiKeyBox.Text = s.ApiKey;
        OllamaBox.Text = s.OllamaUrl;
        UpdateOllamaRow();

        // ── FILE TREE WIRING ──────────────────────────────────────────────────
        // CRITICAL: without these two lines the file tree shows nothing and
        // clicking files does nothing.
        // CRITICAL: use BaseDirectory, NOT UserProfile — traversing home blocks the UI thread.
        var root = AppDomain.CurrentDomain.BaseDirectory;
        FileTree.ItemsSource = new[] { BuildTree(root) };

        FileTree.SelectionChanged += (_, _) =>
        {
            if (FileTree.SelectedItem is FileNode { IsDirectory: false } node)
            {
                _currentFilePath = node.FullPath;
                CodeEditor.Text  = File.ReadAllText(node.FullPath);
                CodeEditor.SyntaxHighlighting =
                    HighlightingManager.Instance.GetDefinitionByExtension(
                        Path.GetExtension(node.FullPath));
                StatusText.Text = node.FullPath;
                LangLabel.Text  = Path.GetExtension(node.FullPath).TrimStart('.').ToUpperInvariant();
            }
        };

        // ── CURSOR POSITION ───────────────────────────────────────────────────
        CodeEditor.TextArea.Caret.PositionChanged += (_, _) =>
        {
            var loc = CodeEditor.TextArea.Caret.Location;
            CursorPos.Text = $"Ln {loc.Line}, Col {loc.Column}";
        };

        // ── OPEN WORKSPACE BUTTON ─────────────────────────────────────────────
        OpenBtn.Click += async (_, _) =>
        {
            var folders = await TopLevel.GetTopLevel(this)!.StorageProvider
                .OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
                    { Title = "Open Workspace" });
            if (folders.Count > 0)
            {
                FileTree.ItemsSource = new[] { BuildTree(folders[0].Path.LocalPath) };
                StatusText.Text = folders[0].Path.LocalPath;
            }
        };

        // ── AI INPUT ──────────────────────────────────────────────────────────
        AiInput.KeyDown += (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter) OnAiSend(null!, null!);
        };
    }

    // ── AI ASSISTANT ─────────────────────────────────────────────────────────
    private async void OnAiSend(object? sender, Avalonia.Interactivity.RoutedEventArgs? e)
    {
        var question = AiInput.Text?.Trim();
        if (string.IsNullOrEmpty(question)) return;
        AiInput.Text = "";

        // Include selected code (or full editor) as context
        var ctx = CodeEditor.SelectedText.Length > 0
            ? CodeEditor.SelectedText
            : CodeEditor.Text;
        var prompt = string.IsNullOrWhiteSpace(ctx)
            ? question
            : $"{question}\n\n```csharp\n{ctx}\n```";

        // Add reply bubble to AiMessages
        var reply = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.White, Margin = new Avalonia.Thickness(0, 4) };
        AiMessages.Items.Add(reply);
        AiScroll.ScrollToEnd();

        try
        {
            // CORRECT pattern: always call EnsureInitialized + Load, never MaINBootstrapper.Initialize()
            MaINSetup.EnsureInitialized();
            var s = MaINSetup.Load();

            // AIHub.Agent keeps conversation history — use this, not AIHub.Chat()
            _agent ??= await AIHub.Agent()
                .WithModel(s.ModelName)
                .WithInitialPrompt("You are an expert C# code assistant embedded in an IDE. " +
                                   "Analyze code, explain concepts, and suggest improvements.")
                .CreateAsync();

            await _agent.ProcessAsync(prompt, tokenCallback: token =>
            {
                if (!string.IsNullOrEmpty(token?.Text))
                    Dispatcher.UIThread.Post(() => { reply.Text += token.Text; AiScroll.ScrollToEnd(); });
                return Task.CompletedTask;
            });
        }
        catch (Exception ex)
        {
            reply.Text = $"Error: {ex.Message}";
        }
    }

    // ── SETTINGS SAVE ────────────────────────────────────────────────────────
    private void OnSave(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var backend = (BackendCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "OpenAi";
        MaINSetup.Save(new AppSettings(
            BackendType: backend,
            ModelName:   ModelBox.Text ?? "",
            ApiKey:      ApiKeyBox.Text ?? "",
            OllamaUrl:   OllamaBox.Text ?? "http://localhost:11434"));
        _agent = null; // recreate with new settings on next send
    }

    private void UpdateOllamaRow()
    {
        var isOllama = (BackendCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() == "Ollama";
        ApiKeyLabel.IsVisible = !isOllama;
        ApiKeyBox.IsVisible   = !isOllama;
        OllamaLabel.IsVisible = isOllama;
        OllamaBox.IsVisible   = isOllama;
    }

    private void OnBackendChanged(object? sender, SelectionChangedEventArgs e) => UpdateOllamaRow();
}
```

**These two types MUST be included in the project (agents frequently forget them):**

```csharp
// Place outside the MainWindow class, at the bottom of the file or in a separate file
public record FileNode(string Name, string FullPath, bool IsDirectory, List<FileNode> Children);

public class BoolToFolderIcon : Avalonia.Data.Converters.IValueConverter
{
    public static readonly BoolToFolderIcon Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => value is true ? "📁" : "📄";
    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}
```

`BoolToFolderIcon` is referenced in the XAML `TreeDataTemplate` via `{x:Static local:BoolToFolderIcon.Instance}`.
Omitting it causes AVLN2000 at compile time. Omitting `FileNode` causes CS0246.

**Named controls this skeleton depends on (define in XAML):**
- `FileTree` — TreeView wired with `TreeDataTemplate` (see section 3)
- `CodeEditor` — `aedit:TextEditor` (see section 1)
- `AiMessages` — `ItemsControl` inside `ScrollViewer Name="AiScroll"`
- `AiInput` — `TextBox` for AI prompt entry
- `OpenBtn` — `Button` to open a workspace folder
- `StatusText`, `CursorPos`, `LangLabel` — status bar TextBlocks (see section 6)
- `BackendCombo`, `ModelBox`, `ApiKeyBox`, `OllamaBox` — settings controls (from base template)
- `ApiKeyLabel`, `OllamaLabel`, `SavedLabel` — settings labels

Copy `MaINSetup.cs`, `ChatBubble.cs`, and `AppSettings` from `app-template-desktop.md` verbatim.

---

## Full .csproj for an IDE-style app

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>MyIde</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia"               Version="11.*" />
    <PackageReference Include="Avalonia.Desktop"       Version="11.*" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="11.*" />
    <PackageReference Include="Avalonia.AvaloniaEdit"  Version="11.*" />
    <PackageReference Include="MaIN.NET"               Version="*"    />
  </ItemGroup>
</Project>
```
