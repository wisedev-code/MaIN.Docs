export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  streaming?: boolean;
}

export interface AgentCapability {
  label: string;
  description: string;
}

export interface AgentDefinition {
  id: string;
  name: string;
  tagline: string;
  description: string;
  icon: string;
  capabilities: AgentCapability[];
  tools: string[];
  bestFor: string[];
}

export const AGENTS: AgentDefinition[] = [
  {
    id: 'code',
    name: 'Code',
    tagline: 'Generate & explain MaIN.NET code',
    description: 'Produces complete, runnable C# code examples. Covers setup, chat, streaming, agents, flows, and backend configuration.',
    icon: '#',
    capabilities: [
      { label: 'Code generation',  description: '' },
      { label: 'Setup & config',   description: '' },
      { label: 'Streaming',        description: '' },
      { label: 'Agents & flows',   description: '' },
    ],
    tools: ['Documentation search', 'Generate artifact', 'Web search', 'Code build'],
    bestFor: ['Getting started', 'Backend integration', 'Streaming setup', 'Agent wiring'],
  },
  {
    id: 'design',
    name: 'Design',
    tagline: 'Architect AI systems with MaIN.NET',
    description: 'Helps design multi-agent systems, flow pipelines, and backend selection strategies. Reasons about tradeoffs and scalability.',
    icon: 'Δ',
    capabilities: [
      { label: 'System design',    description: '' },
      { label: 'Tech selection',   description: '' },
      { label: 'Best practices',   description: '' },
      { label: 'Pattern research', description: '' },
    ],
    tools: ['GitHub', 'Research', 'Doc search', 'Dotnet skills'],
    bestFor: ['Multi-agent systems', 'Production architecture', 'Backend strategy', 'Skill planning'],
  },
  {
    id: 'review',
    name: 'Review',
    tagline: 'Audit, debug & harden your MaIN.NET code',
    description: 'Audits MaIN.NET usage for correctness, performance, and best practices. Spots misconfigurations and suggests improvements.',
    icon: '~',
    capabilities: [
      { label: 'Code audit',    description: '' },
      { label: 'Debugging',     description: '' },
      { label: 'Performance',   description: '' },
      { label: 'Security',      description: '' },
    ],
    tools: ['GitHub', 'Doc search', 'Static analysis', 'Dotnet skills'],
    bestFor: ['Debugging issues', 'Code quality', 'Docker / deployment', 'Configuration errors'],
  },
];
