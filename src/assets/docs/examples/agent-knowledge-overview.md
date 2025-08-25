# Knowledge Base Feature Overview

## Introduction

The Knowledge Base feature in MaIN.NET 0.4.1 enables agents to access and utilize external information sources intelligently. This feature allows agents to provide accurate, context-aware responses by leveraging persisted knowledge from files, web sources, and MCP (Model Context Protocol) servers.

## How Knowledge Base Works

### Core Concept
The knowledge base system creates an `index.json` file that contains a set of sources with assigned tags. When an agent receives a query, it can:

1. **Evaluate Query Relevance** - Determine if external knowledge is needed
2. **Tag Matching** - Find relevant knowledge sources based on tags
3. **Source Selection** - Choose appropriate knowledge items (1-4 tags maximum)
4. **Context Integration** - Use selected knowledge to enhance responses

### Knowledge Storage Structure
Knowledge is persisted as an index with the following structure:
- **Name**: Identifier for the knowledge item
- **Type**: File, URL, Text, or MCP
- **Value**: Actual content or reference
- **Tags**: Array of searchable keywords

## Knowledge Decision Process

The `AnswerCommandHandler` implements intelligent knowledge usage through three modes:

### KnowledgeUsage.UseMemory
Uses traditional chat memory without external knowledge sources.

### KnowledgeUsage.UseKnowledge
Intelligently decides whether to use knowledge based on query analysis:
- Evaluates if the question requires external knowledge
- Uses knowledge unless certain the question needs only basic facts (like "What is 2+2?" or "Capital of France?")
- When in doubt, defaults to using external knowledge

### KnowledgeUsage.AlwaysUseKnowledge
Always processes queries through the knowledge base system.

## Knowledge Types Supported

### File Sources (KnowledgeItemType.File)
- Local files (Markdown, text, etc.)
- Added to `ChatMemoryOptions.FilesData`
- Example: Company documentation, policies, manuals

### Web Sources (KnowledgeItemType.Url)
- Web pages and online resources
- Added to `ChatMemoryOptions.WebUrls`
- Example: Online tutorials, documentation sites

### Text Sources (KnowledgeItemType.Text)
- Direct text content
- Added to `ChatMemoryOptions.TextData`
- Example: Structured data, snippets

### MCP Sources (KnowledgeItemType.Mcp)
- Model Context Protocol servers
- Handled through `mcpService.Prompt()`
- Example: GitHub integration, filesystem access, web search

## Tag-Based Retrieval System

The system uses JSON-serialized tag matching:

1. **Index Creation**: Available knowledge sources with their tags
2. **Query Analysis**: LLM determines relevant tags for the user query
3. **Source Filtering**: Finds knowledge items where tags intersect with query tags or match item names
4. **Context Building**: Selected sources are integrated into the chat context

## KnowledgeBuilder API

The `KnowledgeBuilder` provides a fluent API for constructing knowledge bases:

### File Sources
```csharp
KnowledgeBuilder.Instance
    .AddFile("filename", "./path/to/file.md", tags: ["tag1", "tag2"])
```

### Web Sources
```csharp
KnowledgeBuilder.Instance
    .AddUrl("source_name", "https://example.com", tags: ["web", "tutorial"])
```

### MCP Sources
```csharp
KnowledgeBuilder.Instance
    .AddMcp(new Mcp
    {
        Name = "ServerName",
        Command = "npx",
        Arguments = ["server-package"],
        Backend = BackendType.OpenAi,
        Model = "gpt-4"
    }, ["mcp", "external"])
```

## Integration with Agents

Knowledge bases integrate seamlessly with MaIN.NET agents:

```csharp
var context = await AIHub.Agent()
    .WithModel("your-model")
    .WithKnowledge(KnowledgeBuilder.Instance
        .AddFile(...)
        .AddUrl(...)
        .AddMcp(...))
    .WithSteps(StepBuilder.Instance
        .AnswerUseKnowledge()
        .Build())
    .CreateAsync();
```

## Performance and Limitations

- **Tag Limit**: Returns 1-4 tags maximum per query to maintain focus
- **MCP Limitation**: Cannot combine responses from multiple MCP servers in one request
- **Grammar Control**: Uses structured grammars for decision-making and tag selection
- **Memory Integration**: Knowledge sources are added to chat memory for processing

## Notification System

The system provides progress notifications showing:
- Selected knowledge items (Name|Type format)
- Model being used for processing
- Real-time updates during knowledge processing

This knowledge base implementation provides a lightweight yet powerful way to enhance agent capabilities with external information sources while maintaining performance and accuracy.