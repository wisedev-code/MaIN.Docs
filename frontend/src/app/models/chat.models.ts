export interface Attachment {
  id: string;
  name: string;
  type: 'image' | 'file';
  dataUrl: string;
  mimeType: string;
}

export interface ToolUsage {
  name: string;
  calls: number;
}

export interface ArtifactProposal {
  archiveName: string;
  description: string;
}

export interface IssueProposal {
  title: string;
  body: string;
}

export interface PlanStep {
  title: string;
  description: string;
  codeSnippet?: string;
  language?: string;
}

export interface PlanProposal {
  title: string;
  context: string;
  steps: PlanStep[];
}

export interface PrReviewProposal {
  prNumber: number;
  verdict: string;
  summary: string;
  commentCount: number;
}

export interface CodeChangeProposal {
  branch: string;
  filePath: string;
  commitMessage: string;
  rationale: string;
  preview: string;
}

export interface PrProposal {
  title: string;
  body: string;
  headBranch: string;
  baseBranch: string;
}

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  streaming?: boolean;
  toolsUsed?: ToolUsage[];
  estimatedTokens?: number;
  artifactUrl?: string;
  artifactProposed?: ArtifactProposal;
  issueProposed?: IssueProposal;
  issueUrl?: string;
  planProposed?: PlanProposal;
  reviewProposed?: PrReviewProposal;
  reviewUrl?: string;
  codeChangeProposed?: CodeChangeProposal;
  prProposed?: PrProposal;
  prUrl?: string;
  attachments?: Attachment[];
  agentId?: string;
  branchName?: string;
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
  {
    id: 'flow',
    name: 'Flow',
    tagline: 'Design → Code → Review pipeline',
    description: 'Chains all three agents into a guided end-to-end pipeline. Design builds the plan, Code implements it, Review audits and opens a PR.',
    icon: '⟡',
    capabilities: [
      { label: 'Architecture plan', description: '' },
      { label: 'Code generation',   description: '' },
      { label: 'Code review',       description: '' },
      { label: 'Pull request',      description: '' },
    ],
    tools: ['All agents combined'],
    bestFor: ['Feature implementation', 'End-to-end changes', 'Multi-file modifications', 'PR workflow'],
  },
];
