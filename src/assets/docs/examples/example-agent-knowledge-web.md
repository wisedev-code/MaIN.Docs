# Agent with Knowledge Web Example

## Overview

This example creates a specialized Piano Learning Assistant that leverages multiple online knowledge sources. The agent provides expert instruction on piano techniques, scales, chords, and practice methods by accessing curated web resources in real-time.

## Code Example

```csharp
using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.AgentSource;

namespace Examples.Agents;

public class AgentWithKnowledgeWebExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Piano Learning Assistant with Focused Knowledge Sources");
        AIHub.Extensions.DisableLLamaLogs();
        
        var context = await AIHub.Agent()
            .WithModel("llama3.2:3b")
            .WithMemoryParams(new MemoryParams(){ContextSize = 2137})
            .WithInitialPrompt("""
                You are an expert piano instructor specializing in teaching specific pieces,
                techniques, and solving common playing problems. Help students learn exact
                fingerings, chord progressions, and troubleshoot technical issues with
                detailed, step-by-step guidance for both classical and popular music.
                """)
            .WithKnowledge(KnowledgeBuilder.Instance
                .AddUrl("piano_scales_major", "https://www.pianoscales.org/major.html",
                    tags: ["scale_fingerings", "c_major_scale", "d_major_scale", "fingering_patterns"])
                .AddUrl("piano_chord_database", "https://www.pianochord.org/",
                    tags: ["chord_fingerings", "cmaj7_chord", "chord_inversions", "left_hand_chords"])
                .AddUrl("fundamentals_practice_book", "https://fundamentals-of-piano-practice.readthedocs.io/",
                    tags: ["memorization_techniques", "mental_play_method", "practice_efficiency", "difficult_passages"])
                .AddUrl("hanon_exercises", "https://www.hanon-online.com/",
                    tags: ["hanon_exercises", "finger_independence", "daily_technical_work", "exercise_1_through_20"])
                .AddUrl("sheet_music_reading",
                    "https://www.simplifyingtheory.com/how-to-read-sheet-music-for-beginners/",
                    tags: ["bass_clef_reading", "treble_clef_notes", "note_identification", "staff_reading_speed"])
                .AddUrl("piano_fundamentals", "https://music2me.com/en/magazine/learn-piano-in-13-steps",
                    tags: ["proper_posture", "finger_numbering", "hand_position", "keyboard_orientation"])
                .AddUrl("theory_lessons", "https://www.8notes.com/theory/",
                    tags: ["interval_identification", "key_signatures", "circle_of_fifths", "time_signatures"])
                .AddUrl("piano_terms", "https://www.libertyparkmusic.com/musical-terms-learning-piano/",
                    tags: ["dynamics_markings", "tempo_markings", "articulation_symbols", "expression_terms"]))
            .WithSteps(StepBuilder.Instance
                .AnswerUseKnowledge()
                .Build())
            .CreateAsync();
            
        var result = await context
            .ProcessAsync("I want to learn the C major scale. What's the exact fingering pattern for both hands?" + "I want short and concrete answer");
        Console.WriteLine(result.Message.Content);
    }
}
```

## Key Components

### Model Configuration
- **Model**: `llama3.2:3b` - Efficient local model suitable for educational content
- **Memory Context**: 2137 tokens context size for handling detailed musical information
- **Specialized Prompt**: Positions the agent as an expert piano instructor

### Knowledge Sources

The example demonstrates eight carefully curated web-based knowledge sources:

#### 1. Piano Scales Major (`piano_scales_major`)
- **URL**: https://www.pianoscales.org/major.html
- **Tags**: `["scale_fingerings", "c_major_scale", "d_major_scale", "fingering_patterns"]`
- **Purpose**: Specific fingering patterns for major scales

#### 2. Piano Chord Database (`piano_chord_database`)
- **URL**: https://www.pianochord.org/
- **Tags**: `["chord_fingerings", "cmaj7_chord", "chord_inversions", "left_hand_chords"]`
- **Purpose**: Comprehensive chord fingerings and inversions

#### 3. Fundamentals Practice Book (`fundamentals_practice_book`)
- **URL**: https://fundamentals-of-piano-practice.readthedocs.io/
- **Tags**: `["memorization_techniques", "mental_play_method", "practice_efficiency", "difficult_passages"]`
- **Purpose**: Advanced practice methods and techniques

#### 4. Hanon Exercises (`hanon_exercises`)
- **URL**: https://www.hanon-online.com/
- **Tags**: `["hanon_exercises", "finger_independence", "daily_technical_work", "exercise_1_through_20"]`
- **Purpose**: Technical exercises for finger development

#### 5. Sheet Music Reading (`sheet_music_reading`)
- **URL**: https://www.simplifyingtheory.com/how-to-read-sheet-music-for-beginners/
- **Tags**: `["bass_clef_reading", "treble_clef_notes", "note_identification", "staff_reading_speed"]`
- **Purpose**: Music reading fundamentals

#### 6. Piano Fundamentals (`piano_fundamentals`)
- **URL**: https://music2me.com/en/magazine/learn-piano-in-13-steps
- **Tags**: `["proper_posture", "finger_numbering", "hand_position", "keyboard_orientation"]`
- **Purpose**: Basic piano setup and posture

#### 7. Theory Lessons (`theory_lessons`)
- **URL**: https://www.8notes.com/theory/
- **Tags**: `["interval_identification", "key_signatures", "circle_of_fifths", "time_signatures"]`
- **Purpose**: Music theory concepts

#### 8. Piano Terms (`piano_terms`)
- **URL**: https://www.libertyparkmusic.com/musical-terms-learning-piano/
- **Tags**: `["dynamics_markings", "tempo_markings", "articulation_symbols", "expression_terms"]`
- **Purpose**: Musical terminology and symbols

## Example Query Flow

When a user asks: **"I want to learn the C major scale. What's the exact fingering pattern for both hands?"**

1. **Query Analysis**: System identifies relevant tags like "c_major_scale", "scale_fingerings", "fingering_patterns"
2. **Source Matching**: Matches to `piano_scales_major` source based on tags
3. **Web Content Retrieval**: Fetches current content from pianoscales.org
4. **Expert Response**: Provides specific fingering patterns with step-by-step guidance

## Advanced Features

### Memory Parameters
```csharp
.WithMemoryParams(new MemoryParams(){ContextSize = 2137})
```
- Custom context size to handle detailed musical information
- Ensures sufficient memory for complex musical concepts

### Response Optimization
The query includes: `"I want short and concrete answer"`
- Demonstrates how to guide response format
- Encourages focused, actionable answers

## Use Cases

This web-based knowledge pattern is ideal for:

### Educational Applications
- Subject-specific tutoring systems
- Skills training assistants
- Academic research helpers
- Certification exam preparation

### Professional Development
- Industry-specific guidance
- Best practices repositories
- Current standards and regulations
- Professional certification support

### Hobby and Interest Areas
- Specialized skill development
- Community knowledge sharing
- Project-specific guidance
- Learning path recommendations

## Best Practices

### Source Selection
- **Currency**: Web sources provide up-to-date information
- **Authority**: Choose reputable, expert sources
- **Specificity**: Select sources that match your domain precisely
- **Diversity**: Include multiple perspectives and approaches

### Tag Strategy
- **Granular Tags**: Use specific terms like "c_major_scale" rather than just "scales"
- **Synonyms**: Include alternative terms users might use
- **Progressive Difficulty**: Tags can indicate skill levels
- **Cross-References**: Related concepts should share some tags

### Performance Considerations
- **Source Reliability**: Ensure web sources have good uptime
- **Content Stability**: Prefer sources with stable URLs and content structure
- **Load Time**: Consider the time needed to fetch web content
- **Fallback Options**: Have alternative sources for critical information

## Error Handling

### Network Issues
Consider handling network connectivity problems:
```csharp
// Implement retry logic for web requests
// Have offline fallback content for critical information
// Provide graceful degradation when sources are unavailable
```

### Content Changes
Web sources may change structure or content:
- Monitor source availability
- Implement content validation
- Have backup sources for critical information

## Integration Tips

### Dynamic Content Updates
Web-based knowledge automatically stays current, but consider:
- Caching strategies for frequently accessed content
- Content freshness validation
- Source monitoring for structural changes

### Multi-Language Support
For international applications:
- Include sources in multiple languages
- Use language-specific tags
- Consider cultural differences in teaching approaches

This web-based knowledge example demonstrates how to create domain experts that leverage the vast, current information available online while maintaining focused, accurate responses through intelligent tag-based source selection.