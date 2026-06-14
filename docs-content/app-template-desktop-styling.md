# App Template — Avalonia Desktop: Visual Styling

Read this alongside `app-template-desktop.md` when the user asks for a "visually stunning",
"beautiful", "modern", "polished", or domain-styled desktop app.

The default `FluentTheme` on its own looks flat and generic. Layer the techniques below on
top of `FluentTheme` (don't replace it). **Pick a palette that fits the app's actual domain**
— a weather app, a recipe app, and a budgeting app should NOT end up with the same blue/gray look.

---

## 1. App-wide accent palette in App.axaml

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

Pick 2 colors that fit the app's domain instead of always reusing indigo/pink:
- Sky/cyan → weather, travel
- Emerald/teal → finance, health
- Amber/orange → productivity, food
- Violet/fuchsia → creative tools, music

---

## 2. Gradient header / hero panel

```xml
<Border Background="{StaticResource HeroGradient}" CornerRadius="0,0,16,16" Padding="24,18">
  <TextBlock Text="Aura Weather" FontSize="22" FontWeight="Bold" Foreground="White" />
</Border>
```

---

## 3. "Glass" cards

```xml
<Border Background="#1AFFFFFF" CornerRadius="16" Padding="18" BoxShadow="0 4 24 0 #40000000">
  <!-- card content -->
</Border>
```

`#1AFFFFFF` is white at ~10% opacity (ARGB hex, alpha first) — a translucent overlay
that reads as "glass" on both dark and light window backgrounds. Increase the alpha
(e.g. `#33FFFFFF`) for more contrast against a busy background.

---

## 4. Window background

Give the window a deliberate base color so gradient/glass elements have something to sit on:

```xml
<Window ... Background="#0F172A">
```

Or use a two-stop radial gradient as the background for depth:

```xml
<Window.Background>
  <RadialGradientBrush Center="50%,0%" GradientOrigin="50%,0%" RadiusX="80%" RadiusY="60%">
    <GradientStop Color="#1E1B4B" Offset="0" />
    <GradientStop Color="#0F172A" Offset="1" />
  </RadialGradientBrush>
</Window.Background>
```

---

## 5. Accent buttons

```xml
<Button Content="Refresh" Background="{StaticResource AccentBrush}" Foreground="White"
        CornerRadius="8" Padding="14,8" />
```

For a ghost/outline button:

```xml
<Button Content="Cancel" Background="Transparent" Foreground="{StaticResource AccentBrush}"
        BorderBrush="{StaticResource AccentBrush}" BorderThickness="1"
        CornerRadius="8" Padding="14,8" />
```

---

## 6. Cards / list items with hover effect

Use `PointerEntered` / `PointerExited` in code-behind to swap the `Background` brush:

```csharp
border.PointerEntered += (_, _) => border.Background = new SolidColorBrush(Color.Parse("#1AFFFFFF"));
border.PointerExited  += (_, _) => border.Background = Brushes.Transparent;
```

Or set `Classes` and define styles in XAML for cleaner separation.

---

## 7. Typography

Use `FontWeight="Bold"` / `"SemiBold"` for headings. Increase `FontSize` for hero text.
`Foreground="#94A3B8"` (slate-400) works well as secondary/muted text on dark backgrounds.

```xml
<TextBlock Text="Dashboard" FontSize="26" FontWeight="Bold" Foreground="White" />
<TextBlock Text="Last updated 2 min ago" FontSize="12" Foreground="#94A3B8" />
```

---

## 8. Vary the layout — don't clone the chat template

The base template's `TabControl` (Chat + Settings) is the right shape when the app's
core feature is an AI chat. If the main feature is something else, build the layout
that fits:

- **Dashboard**: `WrapPanel` or `UniformGrid` of `Border` "glass" cards
- **Master/detail**: two-column `Grid`, list on left, detail on right
- **Single-page tool**: full-window `Grid` with a result area and a toolbar
- **Settings access**: put MaIN.NET config behind a small gear `Button` that opens
  a second `Window` with `new SettingsWindow().ShowDialog(this)`, rather than forcing
  every app into a two-tab shape

---

## 9. Animations (simple, no extra packages)

Avalonia has built-in transitions. Add a fade on a panel by toggling `Opacity`:

```xml
<Border Name="ResultPanel" Opacity="0">
  <Border.Transitions>
    <Transitions>
      <DoubleTransition Property="Opacity" Duration="0:0:0.3" />
    </Transitions>
  </Border.Transitions>
  <!-- content -->
</Border>
```

```csharp
ResultPanel.Opacity = 1; // fades in
```

---

## Palette quick reference

| Domain | Stop 1 | Stop 2 | Window bg |
|---|---|---|---|
| Weather / sky | `#0EA5E9` | `#6366F1` | `#0C1A2E` |
| Finance / growth | `#10B981` | `#0EA5E9` | `#0A1F14` |
| Health / calm | `#14B8A6` | `#6366F1` | `#0D1B2A` |
| Productivity | `#F59E0B` | `#EF4444` | `#1C1005` |
| Creative / music | `#A855F7` | `#EC4899` | `#1A0B2E` |
| Dark neutral | `#6366F1` | `#8B5CF6` | `#0F172A` |
