# 🛠️ AIHub Extensions

The `Extensions` abstract class provides utility methods to manage system behaviors like logging. These extensions help create cleaner user experiences by suppressing verbose diagnostic information when not needed.

## ⚙️ Available Extensions

### DisableLLamaLogs()

```csharp
public static void DisableLLamaLogs()
```

**Purpose:** Suppresses all LLaMA model logging output to the console.

**When to use:** 
- In production environments where log noise should be minimized
- In interactive applications where model logs would disrupt the user experience
- When implementing custom logging solutions that don't need raw LLaMA output

**Contract:** Once called, all native LLaMA log messages will be silenced for the duration of the application.

---

### DisableNotificationsLogs()

```csharp
public static void DisableNotificationsLogs()
```

**Purpose:** Disables the notification service's logging capabilities.

**When to use:**
- In applications where notification messages would create unnecessary noise
- When implementing a custom notification handling system
- In scenarios where performance is prioritized over verbose logging

**Contract:** After calling this method, the notification service will no longer output log messages.

## 📋 Example Usage

```csharp
// Disable both logging systems before initializing your AI models
AIHub.Extensions.DisableLLamaLogs();
AIHub.Extensions.DisableNotificationsLogs();

// Now create your context with cleaner console output
var context = await AIHub.Agent()
    .WithModel("gemma3:4b")
    .CreateAsync(interactiveResponse: true);
```

## 🔍 Important Notes

- These methods affect the global logging state and will impact all instances in the application
- Disabling logs can make debugging more difficult - consider enabling logs during development
- These settings persist for the lifetime of the application unless explicitly reversed