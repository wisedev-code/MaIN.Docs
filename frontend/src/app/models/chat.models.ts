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
  kind: 'api' | 'console' | 'desktop';
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

export interface ReviewPosted {
  prNumber: number;
  verdict: string;
  summary: string;
  commentCount: number;
  url: string;
}

export interface ProposedFile {
  path: string;
  content: string;
  language: string;
}

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  streaming?: boolean;
  toolsUsed?: ToolUsage[];
  estimatedTokens?: number;
  artifactUrl?: string;
  artifactKind?: 'api' | 'console' | 'desktop';
  artifactProposed?: ArtifactProposal;
  issueProposed?: IssueProposal;
  issueUrl?: string;
  planProposed?: PlanProposal;
  reviewProposed?: PrReviewProposal;
  reviewUrl?: string;
  codeChangeProposed?: CodeChangeProposal;
  prProposed?: PrProposal;
  prUrl?: string;
  reviewPosted?: ReviewPosted;
  isError?: boolean;
  attachments?: Attachment[];
  agentId?: string;
  branchName?: string;
  docsRead?: string[];
  filesProposed?: ProposedFile[];
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
  special?: boolean;
  beta?: boolean;
  capabilities: AgentCapability[];
  tools: string[];
  bestFor: string[];
  tip: string;
  examplePrompts: string[];
}

export const AGENTS: AgentDefinition[] = [
  {
    id: 'chatty',
    name: 'Chatty',
    tagline: 'Framework facts & documentation',
    description: 'The sharpest way to explore MaIN.NET. Answers questions from the docs with directness and a bit of attitude. Best for quick lookups and architectural sanity checks.',
    icon: '&',
    capabilities: [
      { label: 'Doc lookup',      description: 'Searches the MaIN.NET docs and returns the exact section that answers your question.' },
      { label: 'Quick Q&A',       description: 'Direct, no-fluff answers about framework concepts, APIs, and configuration.' },
      { label: 'Surgical snippets', description: 'Short, focused C# snippets that illustrate a single API call or pattern.' },
    ],
    tools: ['Documentation search'],
    bestFor: ['Learning APIs', 'Sanity checks', 'Quick facts'],
    tip: 'Ask short, specific questions — "What does WithKnowledge() do?" gets a faster, sharper answer than "explain the framework".',
    examplePrompts: [
      'What does MaIN.NET do better than Semantic Kernel or AutoGen?',
      'Give me a 5-line snippet to start a streaming chat with MaIN.NET',
      'Why would I reach for MaIN.NET instead of rolling my own agent loop?',
    ],
  },
  {
    id: 'code',
    name: 'Code',
    tagline: 'Generate & explain MaIN.NET code',
    description: 'Produces complete, runnable C# code examples. Covers setup, chat, streaming, agents, flows, and backend configuration.',
    icon: '#',
    capabilities: [
      { label: 'Code generation',  description: 'Writes complete, compilable C# files — not just fragments — for console, API, or desktop projects.' },
      { label: 'Setup & config',   description: 'Wires up MaINBootstrapper / AddMaIN, backend selection, and runtime configuration prompts.' },
      { label: 'Streaming',        description: 'Implements token-by-token streaming responses with the correct callback signatures.' },
      { label: 'Agents & flows',   description: 'Builds AgentContext pipelines and StepBuilder flows (.Answer, .FetchData, .Become, .Redirect).' },
    ],
    tools: ['Documentation search', 'Generate artifact', 'Web search', 'Code build'],
    bestFor: ['Getting started', 'Backend integration', 'Streaming setup', 'Agent wiring'],
    tip: 'Say what you want to build, not just "show me an example" — e.g. "build me a console chat app using Ollama" gets a downloadable project, not a snippet.',
    examplePrompts: [
      'Build me a small Avalonia desktop chat app wired to a local Ollama model',
      'Show me how to stream tokens from an AgentContext into a UI',
      'How do I chain WithSteps and FetchData into a multi-stage flow?',
    ],
  },
  {
    id: 'design',
    name: 'Design',
    tagline: 'Architect AI systems with MaIN.NET',
    description: 'Helps design multi-agent systems, flow pipelines, and backend selection strategies. Reasons about tradeoffs and scalability.',
    icon: 'Δ',
    capabilities: [
      { label: 'System design',    description: 'Sketches multi-agent architectures, flow pipelines, and data/knowledge wiring for your use case.' },
      { label: 'Tech selection',   description: 'Weighs backend options (Self/Ollama/OpenAI/Gemini/etc.) against cost, latency, and privacy needs.' },
      { label: 'Best practices',   description: 'Flags anti-patterns and points to the production-proven MaIN.NET patterns from the docs and repo.' },
      { label: 'Pattern research', description: 'Pulls real examples from the MaIN.NET GitHub repo to ground recommendations in actual code.' },
    ],
    tools: ['GitHub', 'Research', 'Doc search', 'Dotnet skills'],
    bestFor: ['Multi-agent systems', 'Production architecture', 'Backend strategy', 'Skill planning'],
    tip: 'Bring your constraints — "I need this to run fully offline" or "low latency, cost matters" — and Design will pick backends and patterns around them.',
    examplePrompts: [
      'Propose a way to add a built-in vector-memory module to MaIN.NET',
      'Walk me through how AgentContext, flows, and backends fit together',
      'Design a multi-agent pipeline for a support-ticket triage system',
    ],
  },
  {
    id: 'review',
    name: 'Review',
    tagline: 'Audit, debug & harden your MaIN.NET code',
    description: 'Audits MaIN.NET usage for correctness, performance, and best practices. Spots misconfigurations and suggests improvements.',
    icon: '~',
    capabilities: [
      { label: 'Code audit',    description: 'Reviews MaIN.NET usage against verified API signatures and flags incorrect or outdated calls.' },
      { label: 'Debugging',     description: 'Diagnoses error messages, stack traces, and unexpected behavior tied to MaIN.NET or its config.' },
      { label: 'Performance',   description: 'Spots inefficient knowledge/RAG setups, blocking calls, and avoidable token waste.' },
      { label: 'Security',      description: 'Checks for unsafe config handling, exposed secrets, and missing input validation.' },
    ],
    tools: ['GitHub', 'Doc search', 'Static analysis', 'Dotnet skills'],
    bestFor: ['Debugging issues', 'Code quality', 'Docker / deployment', 'Configuration errors'],
    tip: 'Paste the error message and the surrounding code (or a GitHub PR link) — Review works best with concrete context, not "why doesn\'t this work?".',
    examplePrompts: [
      'Review the newest PR on MaIN.NET — is it safe to merge?',
      'List the current open PRs on MaIN.NET and rank their strengths',
      'Audit this AgentContext setup for thread-safety issues',
    ],
  },
  {
    id: 'forge',
    name: 'Forge',
    tagline: 'Code, design, and review — unified',
    description: 'The all-in-one agent. Generates code, architects systems, plans implementations, reviews PRs, and takes GitHub actions — all in a single conversation.',
    icon: '⬡',
    special: true,
    beta: true,
    capabilities: [
      { label: 'Code generation', description: 'Everything the Code agent can do, in the same conversation as design and review.' },
      { label: 'System design',   description: 'Everything the Design agent can do — architecture, tech selection, pattern research.' },
      { label: 'PR review',       description: 'Everything the Review agent can do — audits, debugging, and GitHub PR feedback.' },
      { label: 'GitHub actions',  description: 'Can open issues, propose code changes, and create pull requests directly.' },
    ],
    tools: ['All tools', 'GitHub', 'Artifacts', 'Doc search'],
    bestFor: ['Complex tasks', 'End-to-end workflows', 'When unsure which agent', 'Mixed code + review'],
    tip: 'Use this when a task spans multiple stages — e.g. "design this, build it, then review the PR" — without switching agents or repeating context.',
    examplePrompts: [
      'Design, build, and review a new caching layer for MaIN.NET\'s knowledge retrieval — then open a PR',
      'Extend MaIN.NET with a plugin system: plan it, implement it, and review the result end to end',
    ],
  },
];
