# Agent with Knowledge File Example

## Overview

This example demonstrates how to create an agent that can access and query local file-based knowledge sources. The agent acts as a company assistant for "TechVibe Solutions," helping employees find answers about company information stored in multiple markdown files.

## Code Example

```csharp
using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Entities.Agents.AgentSource;

namespace Examples.Agents;

public class AgentWithKnowledgeFileExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Agent with knowledge base example");
        AIHub.Extensions.DisableLLamaLogs();
        
        var context = await AIHub.Agent()
            .WithModel("gemma3:4b")
            .WithInitialPrompt("""
                You are a helpful assistant that answers questions about a company. Try to
                help employees find answers to their questions. Company you work for is TechVibe Solutions.
                """)
            .WithKnowledge(KnowledgeBuilder.Instance
                .AddFile("people.md", "./Files/Knowledge/people.md", 
                    tags: ["workers", "employees", "company"])
                .AddFile("organization.md", "./Files/Knowledge/organization.md",
                    tags:["company structure", "company policy", "company culture", "company overview"])
                .AddFile("events.md", "./Files/Knowledge/events.md",
                    tags: ["company events", "company calendar", "company agenda"])
                .AddFile("office_layout.md", "./Files/Knowledge/office_layout.md",
                    tags: ["company layout", "company facilities", "company environment", "office items", "supplies"]))
            .WithSteps(StepBuilder.Instance
                .AnswerUseKnowledge()
                .Build())
            .CreateAsync();
        
        var result = await context
            .ProcessAsync("Hey! Where I can find some printer paper?");
        Console.WriteLine(result.Message.Content);
    }
}
```

## Key Components

### Model Configuration
- **Model**: `gemma3:4b` - A lightweight local model suitable for company assistance tasks
- **Initial Prompt**: Establishes the agent's role as a TechVibe Solutions company assistant

### Knowledge Sources

The example demonstrates four different file-based knowledge sources:

#### 1. People Directory (`people.md`)
- **Purpose**: Employee information and contact details
- **Tags**: `["workers", "employees", "company"]`
- **Use Cases**: Finding colleagues, contact information, organizational structure

#### 2. Organization Information (`organization.md`)
- **Purpose**: Company structure, policies, and culture documentation
- **Tags**: `["company structure", "company policy", "company culture", "company overview"]`
- **Use Cases**: Understanding company hierarchy, policies, cultural guidelines

#### 3. Events Calendar (`events.md`)
- **Purpose**: Company events and calendar information
- **Tags**: `["company events", "company calendar", "company agenda"]`
- **Use Cases**: Upcoming meetings, company events, important dates

#### 4. Office Layout (`office_layout.md`)
- **Purpose**: Physical office information, facilities, and supplies
- **Tags**: `["company layout", "company facilities", "company environment", "office items", "supplies"]`
- **Use Cases**: Finding office resources, navigation, facility information

### Step Configuration
- Uses `AnswerUseKnowledge()` which intelligently determines when to access knowledge sources
- The system evaluates each query to decide if external knowledge is needed

## Example Query Flow

When a user asks: **"Hey! Where I can find some printer paper?"**

1. **Query Analysis**: The system analyzes the question and identifies relevant tags
2. **Knowledge Matching**: Matches tags like "office items" and "supplies" to `office_layout.md`
3. **Content Retrieval**: Loads the relevant file content into memory
4. **Response Generation**: Uses the file content to provide specific location information

## File Structure Requirements

The example expects the following file structure:
```
./Files/Knowledge/
├── people.md
├── organization.md
├── events.md
└── office_layout.md
```

## Use Cases

This pattern is ideal for:

### Corporate Knowledge Bases
- Employee handbooks
- Policy documentation
- Organizational charts
- Office information

### Documentation Systems
- Technical documentation
- User manuals
- FAQ collections
- Standard operating procedures

### Educational Content
- Course materials
- Reference guides
- Learning resources
- Curriculum information

## Best Practices

### Tag Selection
- Use specific, descriptive tags that match how users might ask questions
- Include both broad categories (`"company"`) and specific terms (`"supplies"`)
- Consider synonyms and alternative ways users might phrase queries

### File Organization
- Keep files focused on specific topics
- Use clear, descriptive filenames
- Maintain consistent formatting within files
- Regular updates to keep information current

### Model Selection
- Local models like `gemma3:4b` work well for company-specific knowledge
- Consider model size vs. response quality based on your needs
- Test with your specific knowledge content to ensure good performance

## Integration Tips

### Error Handling
Consider adding error handling for missing files:
```csharp
// Verify files exist before creating agent
var filePaths = new[] { 
    "./Files/Knowledge/people.md",
    "./Files/Knowledge/organization.md",
    // ... other files
};

foreach (var path in filePaths)
{
    if (!File.Exists(path))
        throw new FileNotFoundException($"Knowledge file not found: {path}");
}
```

### Dynamic Content
For frequently updated content, consider:
- Implementing file watchers to detect changes
- Periodic knowledge base rebuilding
- Version control integration for content management

This example provides a solid foundation for building file-based knowledge systems that can scale to handle extensive corporate or domain-specific information repositories.