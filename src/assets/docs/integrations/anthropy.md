# 🧠 Anthropic Integration with MaIN Framework

This documentation provides a quick guide for integrating Anthropic into your MaIN-based application. The integration process is simple and ensures that everything in the MaIN framework works seamlessly with Anthropic as the backend, without requiring any additional modifications to existing functionality.

## 🚀 Quick Start

Integrating Anthropic with MaIN requires minimal configuration. All you need to do is specify your Anthropic API key and set the backend type to Anthropic. Once this is done, you can use the full functionality of the MaIN framework, and everything will work as expected, without the need for further adjustments.

### 📝 Code Example

#### Using `appsettings.json`

To configure Anthropic in your `appsettings.json`, add the following section:

```json
{
  "MaIN": {
    "BackendType": "Anthropic",
    "AnthropicKey": "<YOUR_ANTHROPIC_KEY>"
  }
}
```

#### Simple Console Initialization

Alternatively, you can set the backend type and Anthropic key programmatically during initialization using `MaINBootstrapper.Initialize`:

```csharp
MaINBootstrapper.Initialize(configureSettings: (options) =>
{
    options.BackendType = BackendType.Anthropic;
    options.AnthropicKey = "<YOUR_ANTHROPIC_KEY>";
});
```

#### Using ServiceBuilder for API-based Use Cases

If you're setting up MaIN in an API or web-based application (e.g., ASP.NET Core), you can configure it using `ServiceBuilder`:

```csharp
services.AddMaIN(configuration, (options) =>
{
    options.BackendType = BackendType.Anthropic;
    options.AnthropicKey = "<YOUR_ANTHROPIC_KEY>";
});
```

### 📦 Using Environment Variables

If you prefer not to store your Anthropic key in your `appsettings.json` or directly in the code, you can set it using an environment variable. This will automatically be detected by the MaIN framework.

For example, you can set the `ANTHROPIC_API_KEY` environment variable in different platforms:

- **On Windows (Command Prompt):**

  ```bash
  set ANTHROPIC_API_KEY=<YOUR_ANTHROPIC_KEY>
  ```

- **On Windows (PowerShell):**

  ```bash
  $env:ANTHROPIC_API_KEY="<YOUR_ANTHROPIC_KEY>"
  ```

- **On macOS/Linux:**

  ```bash
  export ANTHROPIC_API_KEY=<YOUR_ANTHROPIC_KEY>
  ```

This way, you can securely store your API key in the environment without the need for hardcoding it.

---

## 🔹 What’s Included with Anthropic Integration

Once you configure Anthropic as the backend, **everything in the MaIN framework works the same way**. This includes all the core functionality such as chat, agents, and data retrieval tasks, which continue to operate without needing any special changes to the logic or structure.

- **No additional configuration is required** to use Anthropic with any MaIN-based feature.
- Whether you're interacting with chat models, agents, or external data sources, the behavior remains consistent.
- The integration allows MaIN to work seamlessly with Anthropic, enabling you to use the full power of the framework without worrying about backend-specific configurations.

---

## ⚠️ Important Considerations

When using Anthropic models with the MaIN framework, please note these current limitations:

- Image Generation: Anthropic models do not support direct image generation. Using features that rely on generating images will throw a `NotSupportedException`.
- Embeddings: Anthropic models do not support generating embeddings (e.g., for file processing that requires text vectorization). Any MaIN functionality dependent on embeddings will result in a `NotSupportedException`.

Please ensure your application's design accounts for these limitations to prevent errors. If these functionalities are crucial for your application, you might want to consider using a different backend, such as OpenAI or Gemini.

---

## 🔧 Features

- **Simple Setup**: All you need to do is specify your Anthropic key and set the backend type to Anthropic, either via `appsettings.json`, environment variables, or programmatically.
- **Environment Variable Support**: Supports storing your Anthropic key securely as an environment variable, making it easy to configure on different platforms.
- **Seamless Operation**: Once Anthropic is configured, everything within the MaIN framework works identically with Anthropic, with no need for adjustments or special handling for different tasks.

This integration ensures that your application remains flexible and easy to manage, allowing you to focus on using the features of MaIN while leveraging Anthropic’s powerful capabilities.
