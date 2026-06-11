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
    <TargetFramework>net9.0</TargetFramework>
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
