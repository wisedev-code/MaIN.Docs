# App Template — Avalonia Desktop

A complete, runnable Avalonia desktop application wired to MaIN.NET. Use this as
the reference when a user asks for a "desktop app", "GUI app", or selects the
desktop project kind.

No special SDK workload required — pure NuGet packages. `dotnet run` works on
Windows, macOS, and Linux with no extra setup.

---

## Running

```bash
dotnet run
```

The window opens with **Chat** and **Settings** tabs. Open **Settings** first,
pick a backend, enter your API key (or Ollama URL), and click **Save**. Then
switch to Chat.

---

## Files

### File: ChatDesktop/ChatDesktop.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>ChatDesktop</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia"               Version="11.*" />
    <PackageReference Include="Avalonia.Desktop"       Version="11.*" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="11.*" />
    <PackageReference Include="MaIN.NET"               Version="*"    />
  </ItemGroup>
</Project>
```

### File: ChatDesktop/Program.cs

```csharp
using Avalonia;
using ChatDesktop;

AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .StartWithClassicDesktopLifetime(args);
```

### File: ChatDesktop/App.axaml

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="ChatDesktop.App">
  <Application.Styles>
    <FluentTheme />
  </Application.Styles>
</Application>
```

### File: ChatDesktop/App.axaml.cs

```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace ChatDesktop;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }
}
```

### File: ChatDesktop/MaINSetup.cs

```csharp
using System.Text.Json;
using MaIN.Core;
using MaIN.Domain.Configuration;
using MaIN.Domain.Models;

namespace ChatDesktop;

public record AppSettings(
    string BackendType = "OpenAi",
    string ModelName   = "",
    string ApiKey      = "",
    string OllamaUrl   = "http://localhost:11434");

public static class MaINSetup
{
    private static bool _initialized;

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChatDesktop", "settings.json");

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsPath)) return new AppSettings();
        try { return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new AppSettings(); }
        catch { return new AppSettings(); }
    }

    public static void Save(AppSettings s)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath,
            JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
        _initialized = false;
    }

    public static void EnsureInitialized()
    {
        if (_initialized) return;
        var s = Load();
        MaINBootstrapper.Initialize(configureSettings: cfg =>
        {
            cfg.BackendType = s.BackendType switch
            {
                "Gemini"    => BackendType.Gemini,
                "Anthropic" => BackendType.Anthropic,
                "Ollama"    => BackendType.Ollama,
                _           => BackendType.OpenAi
            };
            cfg.OpenAiKey    = s.ApiKey;
            cfg.GeminiKey    = s.ApiKey;
            cfg.AnthropicKey = s.ApiKey;
            cfg.OllamaKey    = s.OllamaUrl;
        });
        _initialized = true;
    }
}
```

### File: ChatDesktop/ChatBubble.cs

```csharp
using System.ComponentModel;
using Avalonia.Layout;
using Avalonia.Media;

namespace ChatDesktop;

public class ChatBubble : INotifyPropertyChanged
{
    private static readonly IBrush UserBg      = new SolidColorBrush(Color.Parse("#2563eb"));
    private static readonly IBrush AssistantBg = new SolidColorBrush(Color.Parse("#374151"));

    private string _text = "";

    public string Text
    {
        get => _text;
        set { _text = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text))); }
    }

    public bool IsUser { get; init; }

    public HorizontalAlignment Align    => IsUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    public IBrush              BubbleBg => IsUser ? UserBg : AssistantBg;

    public event PropertyChangedEventHandler? PropertyChanged;
}
```

### File: ChatDesktop/MainWindow.axaml

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:ChatDesktop"
        x:Class="ChatDesktop.MainWindow"
        Title="MaIN Chat" Width="720" Height="560"
        MinWidth="420" MinHeight="340">

  <TabControl Margin="8">

    <!-- CHAT TAB -->
    <TabItem Header="Chat">
      <Grid RowDefinitions="*,Auto" Margin="4">

        <ScrollViewer Grid.Row="0" Name="ChatScroll" Margin="0,0,0,8">
          <ItemsControl Name="MessageList">
            <ItemsControl.ItemTemplate>
              <DataTemplate DataType="{x:Type local:ChatBubble}">
                <Border HorizontalAlignment="{Binding Align}"
                        Background="{Binding BubbleBg}"
                        CornerRadius="8" Padding="10,6" Margin="0,3"
                        MaxWidth="500">
                  <TextBlock Text="{Binding Text}" TextWrapping="Wrap"
                             Foreground="White" />
                </Border>
              </DataTemplate>
            </ItemsControl.ItemTemplate>
          </ItemsControl>
        </ScrollViewer>

        <Grid Grid.Row="1" ColumnDefinitions="*,Auto">
          <TextBox Name="InputBox" Grid.Column="0"
                   Watermark="Type a message and press Enter…"
                   Margin="0,0,8,0" />
          <Button Name="SendBtn" Grid.Column="1"
                  Content="Send" Click="OnSend" MinWidth="72" />
        </Grid>

      </Grid>
    </TabItem>

    <!-- SETTINGS TAB -->
    <TabItem Header="Settings">
      <StackPanel Margin="20" Spacing="8" MaxWidth="380">

        <TextBlock Text="Backend" FontWeight="SemiBold" />
        <ComboBox Name="BackendCombo" MinWidth="180" HorizontalAlignment="Left"
                  SelectionChanged="OnBackendChanged">
          <ComboBoxItem Content="OpenAi" />
          <ComboBoxItem Content="Gemini" />
          <ComboBoxItem Content="Anthropic" />
          <ComboBoxItem Content="Ollama" />
        </ComboBox>

        <TextBlock Text="Model" FontWeight="SemiBold" />
        <TextBox Name="ModelBox" Watermark="e.g. gpt-4.1-mini, gemini-2.0-flash" />

        <TextBlock Name="ApiKeyLabel" Text="API Key" FontWeight="SemiBold" />
        <TextBox Name="ApiKeyBox" PasswordChar="•"
                 Watermark="sk-… / AIza… / sk-ant-…" />

        <TextBlock Name="OllamaLabel" Text="Ollama URL" FontWeight="SemiBold"
                   IsVisible="False" />
        <TextBox Name="OllamaBox" Watermark="http://localhost:11434"
                 IsVisible="False" />

        <Button Content="Save" Click="OnSave" MinWidth="80" Margin="0,6,0,0" />
        <TextBlock Name="SavedLabel" Text="Settings saved."
                   IsVisible="False" Foreground="Green" />

      </StackPanel>
    </TabItem>

  </TabControl>
</Window>
```

### File: ChatDesktop/MainWindow.axaml.cs

> **COMPILE RULE — never add `using MaIN.Domain.Entities;` here.**
> `Message` and `MessageType` are internal server-side types and are not
> exported by the MaIN.NET NuGet package. They will cause CS0246/CS0103.
> The consumer-facing `ProcessAsync(string text, tokenCallback: ...)` overload
> takes a plain string — no `Message` object needed.

```csharp
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Threading;
using MaIN.Core.Hub;
using MaIN.Core.Hub.Contexts.Interfaces.AgentContext;

namespace ChatDesktop;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<ChatBubble> _messages = [];
    private IAgentContextExecutor? _agent;

    public MainWindow()
    {
        InitializeComponent();
        MessageList.ItemsSource = _messages;

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

        InputBox.KeyDown += (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter) OnSend(null!, null!);
        };
    }

    private void OnBackendChanged(object? sender, SelectionChangedEventArgs e) =>
        UpdateOllamaRow();

    private void UpdateOllamaRow()
    {
        var isOllama = (BackendCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() == "Ollama";
        ApiKeyLabel.IsVisible = !isOllama;
        ApiKeyBox.IsVisible   = !isOllama;
        OllamaLabel.IsVisible = isOllama;
        OllamaBox.IsVisible   = isOllama;
    }

    private async void OnSend(object? sender, Avalonia.Interactivity.RoutedEventArgs? e)
    {
        var text = InputBox.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        InputBox.Text     = "";
        SendBtn.IsEnabled = false;

        _messages.Add(new ChatBubble { Text = text, IsUser = true });
        var reply = new ChatBubble { IsUser = false };
        _messages.Add(reply);
        ChatScroll.ScrollToEnd();

        try
        {
            MaINSetup.EnsureInitialized();
            var s = MaINSetup.Load();

            _agent ??= await AIHub.Agent()
                .WithModel(s.ModelName)
                .WithInitialPrompt("You are a helpful assistant.")
                .CreateAsync();

            await _agent.ProcessAsync(text, tokenCallback: token =>
            {
                if (string.IsNullOrEmpty(token?.Text)) return Task.CompletedTask;
                Dispatcher.UIThread.Post(() =>
                {
                    reply.Text += token.Text;
                    ChatScroll.ScrollToEnd();
                });
                return Task.CompletedTask;
            });
        }
        catch (Exception ex)
        {
            reply.Text = $"Error: {ex.Message}";
        }
        finally
        {
            SendBtn.IsEnabled = true;
        }
    }

    private void OnSave(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var backend = (BackendCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "OpenAi";
        MaINSetup.Save(new AppSettings(
            BackendType: backend,
            ModelName:   ModelBox.Text ?? "",
            ApiKey:      ApiKeyBox.Text ?? "",
            OllamaUrl:   OllamaBox.Text ?? "http://localhost:11434"));
        _agent               = null;  // recreate with new settings on next send
        SavedLabel.IsVisible = true;
    }
}
```

---

## Why this shape

- **`AppBuilder.Configure<App>().UsePlatformDetect().StartWithClassicDesktopLifetime(args)`**
  is the standard Avalonia desktop entry point. `UsePlatformDetect()` auto-selects
  Win32 / AppKit / X11 / Wayland at runtime — no platform-specific flags needed.

- **`MaINBootstrapper.Initialize()` must NOT be in `Program.cs`**: Avalonia's
  `AppBuilder` hasn't started the platform layer yet at that point. Call it
  lazily inside `MaINSetup.EnsureInitialized()`, which runs before the first
  `AIHub.*` call in `OnSend`.

- **`ProcessAsync(string text, tokenCallback: ...)` is the ONLY correct call form
  in a consumer project.** Never use the `IEnumerable<Message>` overload — `Message`
  and `MessageType` live in `MaIN.Domain.Entities` which is NOT exported by the
  `MaIN.NET` NuGet package. Using them causes CS0246/CS0103 at compile time.

- **Streaming tokens arrive on a background thread.** Always marshal back with
  `Dispatcher.UIThread.Post(() => ...)` before mutating any bound property.
  This is the Avalonia equivalent of MAUI's `MainThread.BeginInvokeOnMainThread`.

- **`ChatBubble` implements `INotifyPropertyChanged`** so Avalonia automatically
  re-renders the `TextBlock` as each streaming token appends to `Text` — no
  external reactive package required.

- **Settings stored in `{AppData}/ChatDesktop/settings.json`** (cross-platform via
  `Environment.SpecialFolder.ApplicationData`) so they survive `dotnet run`
  restarts without modification.

- **`_agent = null` on Save** forces a fresh `AgentContext` with the new settings
  on the next send. The new context also resets conversation history — intentional
  after a config change.

- **Avalonia packages use `Version="11.*"`** so `Avalonia`, `Avalonia.Desktop`,
  and `Avalonia.Themes.Fluent` always resolve to the same major version at restore
  time. Mixing major versions causes runtime crashes.

## Reading files (PDFs, documents, etc.) — do NOT use Knowledge/RAG APIs

If the app needs to read, summarize, or answer questions about a file the user
picks (PDF, text, etc.), do **not** reach for `KnowledgeBuilder`, `WithKnowledge`,
`.AddFile()`, `AnswerUseKnowledge()`, or any other RAG/"knowledge" API. Their
exact signatures (e.g. `AddFile(path, description, tags)` takes a *required*
`string[] tags` parameter) are easy to get wrong, and there is no verified
template for them — using them is the #1 cause of build failures (CS7036 etc.)
in generated desktop apps.

Instead:
1. Extract the file's text yourself with plain .NET — `File.ReadAllText` for
   text files, or a small well-known package like `PdfPig`/`UglyToad.PdfPig`
   for PDFs.
2. Pass that text directly into `agent.ProcessAsync($"...the user's
   request...\n\n{extractedText}", tokenCallback: ...)`.

This keeps the whole pipeline to APIs that are guaranteed to compile: plain
.NET file I/O + the same `agent.ProcessAsync` overload already used in
`OnSend` above.

### File pickers (Avalonia)

To let the user choose a file, use Avalonia's `StorageProvider` — and don't
forget the `using`:

```csharp
using Avalonia.Platform.Storage;
```

```csharp
var files = await TopLevel.GetTopLevel(this)!.StorageProvider.OpenFilePickerAsync(
    new FilePickerOpenOptions
    {
        Title = "Select a file",
        AllowMultiple = false,
        FileTypeFilter = [ new FilePickerFileType("PDF files") { Patterns = ["*.pdf"] } ]
    });

if (files.Count > 0)
{
    var path = files[0].Path.LocalPath;
    // ... extract text from `path`, then call agent.ProcessAsync(...)
}
```

`FilePickerFileType` and `FilePickerOpenOptions` (with its `FileTypeFilter`
property) live in `Avalonia.Platform.Storage`. There is no standalone type
called `FileTypeFilter` — using that name as a type causes
`CS0246: cannot find type or namespace 'FileTypeFilter'`.

## Common AVLN2000 / XAML compile errors — avoid these

Avalonia's XAML compiler (AVLN2000 "Unable to resolve property X on type Y") is much
stricter about which properties exist on which controls than WPF. These are the most
common hallucinated combinations — check for them before calling `show_file`:

- **`Padding` does NOT exist on `Panel`-derived elements** — `StackPanel`, `Grid`,
  `WrapPanel`, `DockPanel`, `Canvas` have no `Padding` property. Only `Border`,
  `Decorator`, and `ContentControl`-derived elements (`Button`, `TextBox`,
  `ContentControl`, `ScrollViewer`, `TabItem`, etc.) have `Padding`.
  - To pad the contents of a `StackPanel`/`Grid`, wrap it in `<Border Padding="20">...</Border>`,
    or set `Margin` on the panel itself.
- **`Spacing` only exists on `StackPanel` and `WrapPanel`** — not `Grid` or `DockPanel`.
  Use `Margin` on individual children for spacing in those.
- **`CornerRadius` exists on `Border` and a few controls** (`Button`, `TextBox`, etc. —
  anything templated on a rounded `ContentPresenter`/`Border`).
- **`BoxShadow` exists ONLY on `Border`.** It is NOT a property of `Button`, `TextBox`,
  `Image`, or any other control — `<Button BoxShadow="...">` is an AVLN2000 error. If a
  button (or any other control) needs a shadow, wrap it in a `<Border BoxShadow="...">`,
  or skip the shadow on that element entirely.
- Rule of thumb: if you're adding `Padding`, `BoxShadow`, or a `Background` for visual
  effect to a `StackPanel`/`Grid`/`WrapPanel`/`DockPanel`/`Button`/other non-`Border`
  control, wrap that element in a `Border` and set the property on the `Border` instead.
  `CornerRadius` is the only one of these that's safe directly on `Button`/`TextBox`.

## Modern visual style — gradients & glass panels

The default `FluentTheme` on its own looks flat and generic, and reusing the exact
layout/palette from this template for every app is why generated apps look "boring and
all the same". Layer these simple, verified techniques on top of `FluentTheme` (don't
replace it) and pick a palette that fits the app's actual topic — a weather app, a
recipe app, and a budgeting app should NOT end up with the same blue/gray look.

### 1. App-wide accent palette in App.axaml

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="ChatDesktop.App">
  <Application.Styles>
    <FluentTheme />
  </Application.Styles>
  <Application.Resources>
    <SolidColorBrush x:Key="AccentBrush">#6366F1</SolidColorBrush>
    <LinearGradientBrush x:Key="HeroGradient" StartPoint="0%,0%" EndPoint="100%,100%">
      <GradientStop Color="#6366F1" Offset="0" />
      <GradientStop Color="#EC4899" Offset="1" />
    </LinearGradientBrush>
  </Application.Resources>
</Application>
```

Pick 2 colors that fit the app's domain instead of always reusing indigo/pink —
e.g. sky/cyan for weather, emerald/teal for finance or health, amber/orange for
productivity, violet/fuchsia for creative tools.

### 2. Gradient header / hero panel

```xml
<Border Background="{StaticResource HeroGradient}" CornerRadius="0,0,16,16" Padding="24,18">
  <TextBlock Text="Aura Weather" FontSize="22" FontWeight="Bold" Foreground="White" />
</Border>
```

### 3. "Glass" cards

```xml
<Border Background="#1AFFFFFF" CornerRadius="16" Padding="18" BoxShadow="0 4 24 0 #40000000">
  <!-- card content -->
</Border>
```

`#1AFFFFFF` is white at ~10% opacity (ARGB hex, alpha first) — a translucent overlay
that reads as "glass" on both dark and light window backgrounds. Increase the alpha
(e.g. `#33FFFFFF`) for more contrast against a busy background.

### 4. Window background

Give the window a deliberate base color instead of the default flat gray, so the
gradient/glass elements have something to sit on top of:

```xml
<Window ... Background="#0F172A">
```

### 5. Accent buttons

```xml
<Button Content="Refresh" Background="{StaticResource AccentBrush}" Foreground="White"
        CornerRadius="8" Padding="14,8" />
```

### Vary the layout too, not just the colors

This template's TabControl (Chat + Settings) is the right shape when the app's core
feature genuinely is an AI chat. If the app's main feature is something else (a
dashboard, a list + detail view, a single-page tool), build the layout that fits that
feature — a `Grid` dashboard of `Border` "glass" cards, a master/detail `Grid` with two
columns, etc. — and put the MaIN.NET config (Settings) behind a small gear `Button`
that opens a second `Window`, rather than forcing every app into the same two-tab shape.

## Advanced UI — IDE-grade controls

These patterns let you build rich, complex applications like programming IDEs,
code editors, and multi-panel developer tools. Layer them on top of the base template.

### Syntax-highlighted code editor (AvaloniaEdit)

`AvaloniaEdit` is a full-featured code editor control for Avalonia — the same engine
behind JetBrains Rider's editor.

**CRITICAL — package name:** There are two NuGet packages. You MUST use `Avalonia.AvaloniaEdit`
(the modern Avalonia 11-compatible package). Do NOT use the legacy `AvaloniaEdit` package —
it tops out at `0.10.12` and will fail to restore when requested at `11.*`.

Add it to the `.csproj`:

```xml
<PackageReference Include="Avalonia.AvaloniaEdit" Version="11.*" />
```

Use it in XAML (add the namespace):

```xml
<Window xmlns:aedit="using:AvaloniaEdit">
  ...
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
```

Built-in `SyntaxHighlighting` values (case-insensitive string or via extension):
`"C#"`, `"XML"`, `"HTML"`, `"JavaScript"`, `"Python"`, `"SQL"`, `"JSON"`, `"CSS"`,
`"PowerShell"`, `"Boo"`, `"TeX"`, `"MarkDown"`.
Use `HighlightingManager.Instance.GetDefinitionByExtension(".ts")` etc. for others.

### IDE-style resizable multi-panel layout (GridSplitter)

A typical IDE layout: file tree on the left, editor in the center, AI panel on the right,
output/terminal at the bottom. Use `Grid` + `GridSplitter`:

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

`GridSplitter` resizes on drag automatically — no extra code needed. Set
`ResizeDirection="Columns"` for vertical splitters and `ResizeDirection="Rows"`
for horizontal ones.

### File tree with TreeView + HierarchicalDataTemplate

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
    <HierarchicalDataTemplate DataType="{x:Type local:FileNode}"
                              ItemsSource="{Binding Children}">
      <StackPanel Orientation="Horizontal" Spacing="6">
        <TextBlock Text="{Binding IsDirectory, Converter={x:Static local:BoolToFolderIcon.Instance}}"
                   Foreground="#C8A84B" />
        <TextBlock Text="{Binding Name}" Foreground="#CCCCCC" />
      </StackPanel>
    </HierarchicalDataTemplate>
  </TreeView.ItemTemplate>
</TreeView>
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

### Multi-file tabs (TabControl over the editor)

To support multiple open files, replace the single `TextEditor` with a `TabControl`:

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

### AI code assistant integration pattern

Wire the AI panel to the current editor selection so the AI sees the code in context:

```csharp
async void OnAiSend(object? sender, RoutedEventArgs e)
{
    var question = AiInput.Text?.Trim();
    if (string.IsNullOrEmpty(question)) return;
    AiInput.Text = "";

    // Pass selected code or full editor content as context
    var ctx = CodeEditor.SelectedText.Length > 0
        ? CodeEditor.SelectedText
        : CodeEditor.Text;

    var prompt = string.IsNullOrWhiteSpace(ctx)
        ? question
        : $"{question}\n\n```csharp\n{ctx}\n```";

    var reply = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.White };
    // add to AiMessages panel...

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

### Status bar (bottom strip)

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

Update cursor position from the editor's `TextArea.Caret.PositionChanged` event:

```csharp
CodeEditor.TextArea.Caret.PositionChanged += (_, _) =>
{
    var loc = CodeEditor.TextArea.Caret.Location;
    CursorPos.Text = $"Ln {loc.Line}, Col {loc.Column}";
};
```

## Customizing

- **Non-chat UI**: replace the Chat `TabItem` content with whatever controls suit
  your use case. The `AppBuilder` entry, `MaINSetup` bootstrap, and
  `Dispatcher.UIThread.Post` threading pattern apply to any Avalonia window.
- **Additional windows**: create `Window` subclasses and open them from
  `MainWindow` with `new OtherWindow().Show()`.
- **Tools, multi-step pipelines**: see `agents.md` (`WithTools`, `WithSteps`)
  — the same `AIHub.Agent()` wiring works inside any async event handler.
  Avoid `WithKnowledge`/RAG in this template — see "Reading files" above.
- **More backends**: add `ComboBoxItem` entries in the Settings tab and a matching
  `switch` branch in `MaINSetup.EnsureInitialized()`.
