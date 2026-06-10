# App Template — MAUI Desktop Chat App

A minimal but visually polished **.NET MAUI** chat application, targeting
**Windows and Mac Catalyst only** (no Android/iOS — MaIN.NET's native
dependencies have no mobile assets). Use this as the exact reference when a
user asks for a "desktop app", "GUI app", "MAUI app", or "Windows/Mac app".

---

## Prerequisites (one-time, mention this to the user)

Building a MAUI project requires the MAUI workload. If `dotnet build` fails
with an error about an unrecognized `Microsoft.NET.Sdk.Maui` SDK or unknown
target framework `net9.0-windows10.0.19041.0` / `net9.0-maccatalyst`, run:

```powershell
dotnet workload install maui-windows maui-maccatalyst
```

(or `dotnet workload install maui` to install everything, including mobile
workloads, which are not needed here).

## Running the app

MAUI projects multi-target, so `dotnet run` needs an explicit `-f`:

```powershell
# Windows
dotnet build -t:Run -f net9.0-windows10.0.19041.0

# macOS
dotnet build -t:Run -f net9.0-maccatalyst
```

---

## Files

### File: ChatDesktop/ChatDesktop.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Maui">
  <PropertyGroup>
    <TargetFrameworks>net9.0-windows10.0.19041.0;net9.0-maccatalyst</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <RootNamespace>ChatDesktop</RootNamespace>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <ApplicationTitle>ChatDesktop</ApplicationTitle>
    <ApplicationId>com.maindocs.chatdesktop</ApplicationId>
    <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
    <ApplicationVersion>1</ApplicationVersion>
    <WindowsPackageType>None</WindowsPackageType>
    <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">10.0.17763.0</SupportedOSPlatformVersion>
    <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'maccatalyst'">15.0</SupportedOSPlatformVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MaIN.NET" Version="*" />
  </ItemGroup>
</Project>
```

`<WindowsPackageType>None</WindowsPackageType>` builds an unpackaged Windows
app — no MSIX/certificate setup needed for `dotnet run`. No app icon, splash
screen, or font items are declared; they are optional and can be added later
(do not reference image/font files that don't exist in the project — that
fails the build).

### File: ChatDesktop/MauiProgram.cs

```csharp
using Microsoft.Extensions.Logging;

namespace ChatDesktop;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
```

### File: ChatDesktop/App.xaml

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<Application xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="ChatDesktop.App">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Resources/Styles.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

### File: ChatDesktop/App.xaml.cs

```csharp
namespace ChatDesktop;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }
}
```

### File: ChatDesktop/AppShell.xaml

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<Shell xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
       xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
       xmlns:local="clr-namespace:ChatDesktop"
       x:Class="ChatDesktop.AppShell"
       Title="ChatDesktop"
       FlyoutBehavior="Disabled">
    <TabBar>
        <ShellContent Title="Chat" ContentTemplate="{DataTemplate local:ChatPage}" Route="chat" />
        <ShellContent Title="Settings" ContentTemplate="{DataTemplate local:SettingsPage}" Route="settings" />
    </TabBar>
</Shell>
```

### File: ChatDesktop/AppShell.xaml.cs

```csharp
namespace ChatDesktop;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }
}
```

### File: ChatDesktop/Resources/Styles.xaml

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<ResourceDictionary xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                     xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">

    <Color x:Key="PageBackground">#121212</Color>
    <Color x:Key="SurfaceColor">#1E1E1E</Color>
    <Color x:Key="AccentColor">#7C4DFF</Color>
    <Color x:Key="UserBubbleColor">#7C4DFF</Color>
    <Color x:Key="AssistantBubbleColor">#2A2A2A</Color>
    <Color x:Key="TextPrimary">#FFFFFF</Color>
    <Color x:Key="TextSecondary">#AAAAAA</Color>
    <Color x:Key="WarningColor">#FFB74D</Color>

    <Style x:Key="AssistantBubble" TargetType="Border">
        <Setter Property="BackgroundColor" Value="{StaticResource AssistantBubbleColor}" />
        <Setter Property="Stroke" Value="Transparent" />
        <Setter Property="StrokeShape" Value="RoundRectangle 16,16,16,16" />
        <Setter Property="Padding" Value="12,8" />
        <Setter Property="Margin" Value="4,2,80,2" />
        <Setter Property="HorizontalOptions" Value="Start" />
    </Style>

    <Style x:Key="UserBubble" TargetType="Border" BasedOn="{StaticResource AssistantBubble}">
        <Setter Property="BackgroundColor" Value="{StaticResource UserBubbleColor}" />
        <Setter Property="Margin" Value="80,2,4,2" />
        <Setter Property="HorizontalOptions" Value="End" />
    </Style>

    <Style x:Key="PrimaryButton" TargetType="Button">
        <Setter Property="BackgroundColor" Value="{StaticResource AccentColor}" />
        <Setter Property="TextColor" Value="{StaticResource TextPrimary}" />
        <Setter Property="CornerRadius" Value="10" />
        <Setter Property="Padding" Value="16,10" />
        <Setter Property="FontAttributes" Value="Bold" />
    </Style>

</ResourceDictionary>
```

### File: ChatDesktop/ChatBubble.cs

```csharp
using System.ComponentModel;

namespace ChatDesktop;

public class ChatBubble : INotifyPropertyChanged
{
    private string _text = string.Empty;

    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        }
    }

    public bool IsUser { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
}
```

### File: ChatDesktop/MaINSetup.cs

```csharp
using MaIN.Core;
using MaIN.Domain.Models;
using Microsoft.Maui.Storage;

namespace ChatDesktop;

public static class MaINSetup
{
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (_initialized) return;

        var backend = Preferences.Get("BackendType", "Ollama");
        var apiKey = Preferences.Get("ApiKey", "");
        var ollamaUrl = Preferences.Get("OllamaUrl", "http://localhost:11434");

        MaINBootstrapper.Initialize(configureSettings: o =>
        {
            switch (backend)
            {
                case "OpenAi":
                    o.BackendType = BackendType.OpenAi;
                    o.OpenAiKey = apiKey;
                    break;
                case "Gemini":
                    o.BackendType = BackendType.Gemini;
                    o.GeminiKey = apiKey;
                    break;
                case "Anthropic":
                    o.BackendType = BackendType.Anthropic;
                    o.AnthropicKey = apiKey;
                    break;
                case "Self":
                    o.BackendType = BackendType.Self;
                    break;
                default:
                    o.BackendType = BackendType.Ollama;
                    o.OllamaKey = ollamaUrl;
                    break;
            }
        });

        _initialized = true;
    }

    public static string GetModelId()
    {
        var backend = Preferences.Get("BackendType", "Ollama");
        var modelName = Preferences.Get("ModelName", "");

        if (!string.IsNullOrWhiteSpace(modelName)) return modelName;

        return backend switch
        {
            "OpenAi" => Models.OpenAi.Gpt4oMini,
            "Gemini" => Models.Gemini.Gemini2_5Flash,
            "Anthropic" => Models.Anthropic.ClaudeHaiku4_5,
            "Self" => Models.Local.Llama3_2_3b,
            _ => "gemma3:4b",
        };
    }

    /// Call after Settings are saved so the next EnsureInitialized() picks up new values.
    public static void Reset() => _initialized = false;
}
```

### File: ChatDesktop/SettingsPage.xaml

```xml
<?xml version="1.0" encoding="UTF-8"?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="ChatDesktop.SettingsPage"
             Title="Settings"
             BackgroundColor="{StaticResource PageBackground}">
    <ScrollView>
        <VerticalStackLayout Padding="24" Spacing="14">

            <Label Text="Model Backend" TextColor="{StaticResource TextPrimary}" FontAttributes="Bold" FontSize="18" />
            <Picker x:Name="BackendPicker" TextColor="{StaticResource TextPrimary}" BackgroundColor="{StaticResource SurfaceColor}">
                <Picker.ItemsSource>
                    <x:Array Type="{x:Type x:String}">
                        <x:String>Ollama</x:String>
                        <x:String>OpenAi</x:String>
                        <x:String>Gemini</x:String>
                        <x:String>Anthropic</x:String>
                        <x:String>Self</x:String>
                    </x:Array>
                </Picker.ItemsSource>
            </Picker>

            <Label Text="API Key (OpenAi / Gemini / Anthropic only)" TextColor="{StaticResource TextSecondary}" />
            <Entry x:Name="ApiKeyEntry" Placeholder="sk-..." IsPassword="True" TextColor="{StaticResource TextPrimary}" BackgroundColor="{StaticResource SurfaceColor}" />

            <Label Text="Ollama Server URL (Ollama only)" TextColor="{StaticResource TextSecondary}" />
            <Entry x:Name="OllamaUrlEntry" Placeholder="http://localhost:11434" TextColor="{StaticResource TextPrimary}" BackgroundColor="{StaticResource SurfaceColor}" />

            <Label Text="Model Name (optional override, e.g. gemma3:4b)" TextColor="{StaticResource TextSecondary}" />
            <Entry x:Name="ModelNameEntry" Placeholder="leave blank for default" TextColor="{StaticResource TextPrimary}" BackgroundColor="{StaticResource SurfaceColor}" />

            <Button Text="Save" Clicked="OnSaveClicked" Style="{StaticResource PrimaryButton}" />

            <Label x:Name="SavedLabel" Text="Saved!" IsVisible="False" TextColor="{StaticResource AccentColor}" HorizontalOptions="Center" />

            <Label Text="Note: 'Self' (local model) currently runs on Windows only in this template, because the underlying native runtime has no Mac Catalyst build. On Mac, use Ollama or a cloud backend."
                   TextColor="{StaticResource TextSecondary}" FontSize="12" />

        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

### File: ChatDesktop/SettingsPage.xaml.cs

```csharp
using Microsoft.Maui.Storage;

namespace ChatDesktop;

public partial class SettingsPage : ContentPage
{
    private static readonly string[] Backends = { "Ollama", "OpenAi", "Gemini", "Anthropic", "Self" };

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var backend = Preferences.Get("BackendType", "Ollama");
        BackendPicker.SelectedIndex = Math.Max(0, Array.IndexOf(Backends, backend));
        ApiKeyEntry.Text = Preferences.Get("ApiKey", "");
        OllamaUrlEntry.Text = Preferences.Get("OllamaUrl", "http://localhost:11434");
        ModelNameEntry.Text = Preferences.Get("ModelName", "");
        SavedLabel.IsVisible = false;
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        var backend = Backends[Math.Max(0, BackendPicker.SelectedIndex)];

        Preferences.Set("BackendType", backend);
        Preferences.Set("ApiKey", ApiKeyEntry.Text ?? "");
        Preferences.Set("OllamaUrl", string.IsNullOrWhiteSpace(OllamaUrlEntry.Text)
            ? "http://localhost:11434"
            : OllamaUrlEntry.Text);
        Preferences.Set("ModelName", ModelNameEntry.Text ?? "");

        MaINSetup.Reset();
        SavedLabel.IsVisible = true;
    }
}
```

### File: ChatDesktop/ChatPage.xaml

```xml
<?xml version="1.0" encoding="UTF-8"?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:ChatDesktop"
             x:Class="ChatDesktop.ChatPage"
             Title="Chat"
             BackgroundColor="{StaticResource PageBackground}">
    <Grid RowDefinitions="Auto,*,Auto" Padding="12" RowSpacing="8">

        <Label x:Name="ConfigWarning"
               Grid.Row="0"
               Text="Configure a model backend in the Settings tab before chatting."
               TextColor="{StaticResource WarningColor}"
               BackgroundColor="{StaticResource SurfaceColor}"
               Padding="12"
               IsVisible="False" />

        <CollectionView x:Name="MessagesView" Grid.Row="1">
            <CollectionView.ItemTemplate>
                <DataTemplate x:DataType="local:ChatBubble">
                    <Grid Padding="4,2">
                        <Border Style="{StaticResource AssistantBubble}">
                            <Border.Triggers>
                                <DataTrigger TargetType="Border" Binding="{Binding IsUser}" Value="True">
                                    <Setter Property="Style" Value="{StaticResource UserBubble}" />
                                </DataTrigger>
                            </Border.Triggers>
                            <Label Text="{Binding Text}" TextColor="{StaticResource TextPrimary}" />
                        </Border>
                    </Grid>
                </DataTemplate>
            </CollectionView.ItemTemplate>
        </CollectionView>

        <Grid Grid.Row="2" ColumnDefinitions="*,Auto" ColumnSpacing="8">
            <Entry x:Name="InputEntry"
                   Grid.Column="0"
                   Placeholder="Type a message..."
                   TextColor="{StaticResource TextPrimary}"
                   BackgroundColor="{StaticResource SurfaceColor}"
                   Completed="OnSendClicked" />
            <Button x:Name="SendButton"
                    Grid.Column="1"
                    Text="Send"
                    Style="{StaticResource PrimaryButton}"
                    Clicked="OnSendClicked" />
        </Grid>

    </Grid>
</ContentPage>
```

### File: ChatDesktop/ChatPage.xaml.cs

```csharp
using System.Collections.ObjectModel;
using MaIN.Core.Hub;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace ChatDesktop;

public partial class ChatPage : ContentPage
{
    private readonly ObservableCollection<ChatBubble> _messages = new();

    public ChatPage()
    {
        InitializeComponent();
        MessagesView.ItemsSource = _messages;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var backend = Preferences.Get("BackendType", "Ollama");
        var apiKey = Preferences.Get("ApiKey", "");
        var ready = backend is "Ollama" or "Self" || !string.IsNullOrWhiteSpace(apiKey);

        ConfigWarning.IsVisible = !ready;
        SendButton.IsEnabled = ready;
        InputEntry.IsEnabled = ready;

        if (ready) MaINSetup.EnsureInitialized();
    }

    private async void OnSendClicked(object? sender, EventArgs e)
    {
        var text = InputEntry.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        InputEntry.Text = string.Empty;
        SendButton.IsEnabled = false;

        _messages.Add(new ChatBubble { Text = text, IsUser = true });
        var assistantBubble = new ChatBubble { IsUser = false };
        _messages.Add(assistantBubble);

        await AIHub.Chat()
            .WithModel(MaINSetup.GetModelId())
            .EnsureModelDownloaded()
            .WithMessage(text)
            .CompleteAsync(changeOfValue: token =>
            {
                if (token is null || token.Type.ToString() != "Message")
                    return Task.CompletedTask;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    assistantBubble.Text += token.Text;
                });

                return Task.CompletedTask;
            });

        SendButton.IsEnabled = true;
    }
}
```

---

## Why this shape

- **Two tabs (`AppShell`'s `TabBar`)**: Chat and Settings. No complex
  navigation/gating logic — `ChatPage.OnAppearing` simply checks whether
  enough config is present (`Ollama`/`Self` need nothing extra; cloud
  backends need `ApiKey`) and shows a warning banner + disables input if not.
- **`Preferences`** (`Microsoft.Maui.Storage`) is the built-in MAUI key/value
  store — no extra NuGet package, persists across app restarts. This is the
  "prompt user for config" mechanism for desktop.
- **`MaINSetup.EnsureInitialized()`** calls `MaINBootstrapper.Initialize(configureSettings: ...)`
  exactly once per process, reading the saved `Preferences`. `MaINSetup.Reset()`
  is called after Settings are saved so the *next* chat re-initializes with
  the new values. If your installed MaIN.NET version does not support calling
  `Initialize` more than once per process, restart the app after changing
  Settings instead.
- **Streaming**: `CompleteAsync(changeOfValue: token => ...)` fires for every
  token. `token.Type.ToString() != "Message"` filters out non-text tokens
  (reasoning/tool-call/special) **without depending on the exact enum
  namespace** — comparing via `ToString()` always compiles. UI updates are
  marshaled with `MainThread.BeginInvokeOnMainThread` because MAUI throws if
  you touch bound UI objects from a background thread — **this is the #1
  MAUI-specific gotcha**.
- **`ChatBubble` implements `INotifyPropertyChanged`** so the assistant's
  bubble text grows live in the `CollectionView` as tokens arrive.
- **Dark theme** lives entirely in `Resources/Styles.xaml` — concrete hex
  colors and `RoundRectangle` corner radii, merged once in `App.xaml`.

## Platform notes

- `BackendType.Self` (local GGUF via LLamaSharp) only has native binaries for
  Windows/Linux in MaIN.NET — **default to `Ollama` on Mac Catalyst**. The
  Settings page defaults to `Ollama` for this reason; `Self` is offered but
  should be treated as Windows-only.
- Building requires the MAUI workload (see Prerequisites above).
