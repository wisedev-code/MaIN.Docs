# ⚙️ StepBuilder Overview

The **`StepBuilder`** class provides a fluent interface for defining the **behavioral flow of an AI Agent**.
It allows developers to specify **how an agent should process input, use memory, access knowledge, or perform role-based actions** — all through a clear, chainable syntax.

This builder is primarily used when creating or configuring an Agent (e.g., via `AIHub.Agent()`), to define the ordered sequence of reasoning or operational steps the Agent should follow.

---

### 🧩 Namespace

```csharp
using MaIN.Domain.Entities.Agents.Knowledge;
using MaIN.Services.Services.Models.Commands;
```

---

### 🏗️ Class Definition

```csharp
public class StepBuilder
```

A utility class for constructing a list of predefined **agent execution steps**.
Each step defines a mode of operation that influences how the Agent interprets, searches, or responds to input.

---

### 🔹 Core Methods

#### **`Instance`**

```csharp
public static StepBuilder Instance => new();
```

Provides a new instance of `StepBuilder` for fluent chaining.
Used as an entry point for defining a step sequence:

```csharp
StepBuilder.Instance.Answer().Build();
```

---

#### **`Answer()`**

Marks a basic answering step — the Agent simply generates a direct response to the input.

---

#### **`AnswerUseMemory()`**

Enables the Agent to **access and use stored memory** when forming a response.

---

#### **`AnswerUseKnowledge(bool alwaysSearchMemory = false)`**

Allows the Agent to **search its knowledge base** to form an answer.
The optional `alwaysSearchMemory` flag ensures memory is always consulted alongside knowledge retrieval.

---

#### **`AnswerUseKnowledgeWithTags(params string[] tags)`**

Filters knowledge retrieval by **specific tags**, letting the Agent focus on a targeted domain of stored information.

Example:

```csharp
StepBuilder.Instance.AnswerUseKnowledgeWithTags("finance", "market");
```

---

#### **`AnswerUseKnowledgeWithType(KnowledgeItemType type)`**

Restricts knowledge usage to a particular **type** (e.g., documents, facts, summaries).

---

#### **`AnswerUseKnowledgeAndMemory()`**

Combines both **knowledge-based reasoning** and **memory recall** for more contextually rich responses.

---

#### **`AnswerUseKnowledgeAndMemoryWithTags(params string[] tags)`**

A hybrid mode using both memory and tagged knowledge retrieval.

---

#### **`Become(string role)`**

Instructs the Agent to **adopt a specific role or persona** before continuing execution.

Example:

```csharp
.Become("assistant_expert")
```

---

#### **`FetchData(FetchResponseType fetchResponseType = FetchResponseType.AS_Answer)`**

Defines a step where the Agent **fetches external data**, optionally as a system-level operation.

* `AS_Answer` → Fetches data to produce a normal response
* `AS_System` → Fetches data for internal processing or state updates

---

#### **`Mcp()`**

Adds an **MCP (Multi-Component Process)** step — typically used for complex, multi-agent or multi-step coordination workflows.

---

#### **`Redirect(string agentId, string output = "AS_Output", string mode = "REPLACE")`**

Redirects execution flow to another Agent.
Useful for **delegation or agent chaining**.

Example:

```csharp
.Redirect("knowledge_agent", "AS_Output", "MERGE");
```

---

#### **`Build()`**

Finalizes and returns the constructed list of steps.
This list is then passed to the Agent configuration pipeline.

Example:

```csharp
.WithSteps(StepBuilder.Instance
    .AnswerUseKnowledgeAndMemory()
    .Build())
```

---

### 🧠 Example Usage

```csharp
var steps = StepBuilder.Instance
    .AnswerUseKnowledgeAndMemoryWithTags("research", "analysis")
    .Become("data_researcher")
    .Build();

await AIHub.Agent()
    .WithModel("claude-sonnet-4-5-20250929")
    .WithSteps(steps)
    .CreateAsync(interactiveResponse: true);
```

---

### 🔍 Summary

| Category               | Purpose                                       |
| ---------------------- | --------------------------------------------- |
| **Answer**             | Simple or knowledge/memory-enhanced responses |
| **Knowledge & Memory** | Access stored or learned context              |
| **Role / Behavior**    | Define how the agent behaves or identifies    |
| **Data Fetching**      | Integrate external or internal data sources   |
| **Redirection**        | Delegate tasks between agents                 |
| **MCP**                | MCP server call                               |

---

The `StepBuilder` provides a **modular, declarative way to control agent logic**, making agent configurations expressive, reusable, and easy to reason about.
