# 🧠 OpenAI Integration with MaIN Framework

This documentation provides a quick guide for integrating OpenAI into your MaIN-based application. The integration process is simple and ensures that everything in the MaIN framework works seamlessly with OpenAI as the backend, without requiring any additional modifications to existing functionality.

## 🚀 Quick Start

Integrating OpenAI with MaIN requires minimal configuration. All you need to do is specify your OpenAI API key and set the backend type to OpenAI. Once this is done, you can use the full functionality of the MaIN framework, and everything will work as expected, without the need for further adjustments.

### 📝 Code Example

#### Using `appsettings.json`

To configure OpenAI in your `appsettings.json`, add the following section:

```json
{
  "MaIN": {
    "BackendType": "OpenAi",
    "OpenAiKey": "<YOUR_OPENAI_KEY>"
  }
}
```

#### Simple Console Initialization

Alternatively, you can set the backend type and OpenAI key programmatically during initialization using `MaINBootstrapper.Initialize`:

```csharp
MaINBootstrapper.Initialize(configureSettings: (options) =>
{
    options.BackendType = BackendType.OpenAi;
    options.OpenAiKey = "<YOUR_OPENAI_KEY>";
});
```

#### Using ServiceBuilder for API-based Use Cases

If you're setting up MaIN in an API or web-based application (e.g., ASP.NET Core), you can configure it using `ServiceBuilder`:

```csharp
services.AddMaIN(configuration, (options) =>
{
    options.BackendType = BackendType.OpenAi;
    options.OpenAiKey = "<YOUR_OPENAI_KEY>";
});
```

### 📦 Using Environment Variables

If you prefer not to store your OpenAI key in your `appsettings.json` or directly in the code, you can set it using an environment variable. This will automatically be detected by the MaIN framework.

For example, you can set the `OPENAI_API_KEY` environment variable in different platforms:

- **On Windows (Command Prompt):**

  ```bash
  set OPENAI_API_KEY=<YOUR_OPENAI_KEY>
  ```

- **On Windows (PowerShell):**

  ```bash
  $env:OPENAI_API_KEY="<YOUR_OPENAI_KEY>"
  ```

- **On macOS/Linux:**

  ```bash
  export OPENAI_API_KEY=<YOUR_OPENAI_KEY>
  ```

This way, you can securely store your API key in the environment without the need for hardcoding it.

---

## 🔹 What’s Included with OpenAI Integration

Once you configure OpenAI as the backend, **everything in the MaIN framework works the same way**. This includes all the core functionality such as chat, agents, and data retrieval tasks, which continue to operate without needing any special changes to the logic or structure.

- **No additional configuration is required** to use OpenAI with any MaIN-based feature.
- Whether you're interacting with chat models, agents, or external data sources, the behavior remains consistent.
- The integration allows MaIN to work seamlessly with OpenAI, enabling you to use the full power of the framework without worrying about backend-specific configurations.

---

## 🔧 Features
- **Simple Setup**: All you need to do is specify your OpenAI key and set the backend type to OpenAI, either via `appsettings.json`, environment variables, or programmatically.
- **Environment Variable Support**: Supports storing your OpenAI key securely as an environment variable, making it easy to configure on different platforms.
- **Seamless Operation**: Once OpenAI is configured, everything within the MaIN framework works identically with OpenAI, with no need for adjustments or special handling for different tasks.

This integration ensures that your application remains flexible and easy to manage, allowing you to focus on using the features of MaIN while leveraging OpenAI’s powerful capabilities.