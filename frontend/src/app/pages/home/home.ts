import { Component, ElementRef, OnDestroy, ViewChild, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ChatService } from '../../services/chat.service';
import { ArtifactProposal, ChatMessage, ToolUsage, AgentDefinition, AGENTS } from '../../models/chat.models';

import hljs from 'highlight.js/lib/core';
import csharp from 'highlight.js/lib/languages/csharp';
import bash from 'highlight.js/lib/languages/bash';
import xml from 'highlight.js/lib/languages/xml';
import json from 'highlight.js/lib/languages/json';
import plaintext from 'highlight.js/lib/languages/plaintext';

hljs.registerLanguage('csharp', csharp);
hljs.registerLanguage('cs', csharp);
hljs.registerLanguage('bash', bash);
hljs.registerLanguage('shell', bash);
hljs.registerLanguage('xml', xml);
hljs.registerLanguage('json', json);
hljs.registerLanguage('plaintext', plaintext);

@Component({
  selector: 'app-home',
  imports: [FormsModule],
  templateUrl: './home.html',
  styleUrl: './home.scss'
})
export class Home implements OnDestroy {
  @ViewChild('messagesEl') messagesEl!: ElementRef<HTMLDivElement>;

  private readonly ASCII_FACES = [
    '(ง •̀_•́)ง', '(⌐■_■)', '¯\\_(ツ)_/¯', '(╯°□°）╯', '(ಠ_ಠ)',
    '(◕‿◕)', 'ʕ•ᴥ•ʔ', '(¬‿¬)', '(づ｡◕‿◕｡)づ', '(•_•)',
    '(ᵔᴥᵔ)', '(っ˘ω˘ς)', '(ó_ò)', '(≖_≖)', '(งツ)ว',
  ];

  agents = AGENTS;
  selectedAgent = signal<AgentDefinition>(AGENTS[0]);
  hoveredAgent = signal<AgentDefinition | null>(null);
  displayedAgent = signal<AgentDefinition>(AGENTS[0]);
  messages = signal<ChatMessage[]>([]);
  inputText = signal('');
  isStreaming = signal(false);
  currentFace = signal('');
  showArtifactPrompt = signal(false);
  artifactProposal = signal<ArtifactProposal | null>(null);

  hasMessages = computed(() => this.messages().length > 0);

  constructor(private chatService: ChatService) {}

  selectAgent(agent: AgentDefinition) { this.selectedAgent.set(agent); }

  showAgentInfo(agent: AgentDefinition) {
    this.displayedAgent.set(agent);
    this.hoveredAgent.set(agent);
  }

  hideAgentInfo() { this.hoveredAgent.set(null); }

  async send() {
    const text = this.inputText().trim();
    if (!text || this.isStreaming()) return;

    this.showArtifactPrompt.set(false);

    const history = this.messages().slice();
    this.messages.update(m => [...m, { role: 'user', content: text }]);
    this.inputText.set('');
    this.messages.update(m => [...m, { role: 'assistant', content: '', streaming: true }]);
    this.currentFace.set(this.ASCII_FACES[Math.floor(Math.random() * this.ASCII_FACES.length)]);
    this.isStreaming.set(true);
    setTimeout(() => this.scrollToBottom(), 60);

    try {
      const response = await this.chatService.sendMessage(this.selectedAgent().id, text, history);
      const msgIndex = this.messages().length - 1;
      await this.revealWordByWord(response.content, msgIndex, response.toolsUsed, response.estimatedTokens, response.artifactUrl, response.artifactProposed);

      if (response.artifactProposed && !response.artifactUrl) {
        this.artifactProposal.set(response.artifactProposed);
        this.showArtifactPrompt.set(true);
      }
    } catch {
      this.messages.update(msgs => {
        const updated = [...msgs];
        updated[updated.length - 1] = {
          ...updated[updated.length - 1],
          content: 'Sorry, something went wrong. Please try again.',
          streaming: false,
        };
        return updated;
      });
    } finally {
      this.isStreaming.set(false);
    }
  }

  private async revealWordByWord(
    content: string,
    msgIndex: number,
    toolsUsed?: ToolUsage[],
    estimatedTokens?: number,
    artifactUrl?: string,
    artifactProposed?: ArtifactProposal
  ) {
    const CHUNK = 4;
    const DELAY = 32;
    const tokens = content.split(/(\s+)/);
    let current = '';
    let wordCount = 0;

    for (let i = 0; i < tokens.length; i++) {
      if (!this.isStreaming()) break;
      current += tokens[i];
      if (!tokens[i].trim()) continue;

      wordCount++;
      if (wordCount % CHUNK === 0 || i === tokens.length - 1) {
        const snap = current;
        this.messages.update(msgs => {
          const updated = [...msgs];
          updated[msgIndex] = { ...updated[msgIndex], content: snap };
          return updated;
        });
        this.scrollToBottom();
        await new Promise<void>(r => setTimeout(r, DELAY));
      }
    }

    this.messages.update(msgs => {
      const updated = [...msgs];
      updated[msgIndex] = {
        ...updated[msgIndex],
        content,
        streaming: false,
        toolsUsed,
        estimatedTokens,
        artifactUrl,
        artifactProposed,
      };
      return updated;
    });
    setTimeout(() => this.scrollToBottom(), 60);
  }

  dismissArtifactPrompt() {
    this.showArtifactPrompt.set(false);
    this.artifactProposal.set(null);
  }

  requestArtifact() {
    const proposal = this.artifactProposal();
    this.dismissArtifactPrompt();
    const hint = proposal ? ` Name the archive ${proposal.archiveName}.` : '';
    this.inputText.set(`Please generate the downloadable artifact now.${hint}`);
    this.send();
  }

  stop() {
    this.chatService.abort();
    this.messages.update(msgs => {
      const updated = [...msgs];
      if (updated.length > 0)
        updated[updated.length - 1] = { ...updated[updated.length - 1], streaming: false };
      return updated;
    });
    this.isStreaming.set(false);
  }

  clear() {
    this.messages.set([]);
    this.showArtifactPrompt.set(false);
    this.artifactProposal.set(null);
  }

  onKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) { event.preventDefault(); this.send(); }
  }

  extractArchiveName(url: string): string {
    try {
      const path = new URL(url).pathname;
      return decodeURIComponent(path.split('/').pop() ?? 'artifact.zip');
    } catch {
      return 'artifact.zip';
    }
  }

  toolTokenEstimate(toolsUsed: ToolUsage[]): number {
    return toolsUsed.reduce((sum, t) => sum + t.calls * 180, 0);
  }

  private scrollToBottom() {
    const el = this.messagesEl?.nativeElement;
    if (el) el.scrollTo({ top: el.scrollHeight, behavior: 'smooth' });
  }

  ngOnDestroy() { this.chatService.abort(); }

  renderMarkdown(content: string): string {
    return content
      .replace(/```(\w*)\n([\s\S]*?)```/g, (_, lang, code) => {
        const trimmed = code.trimEnd();
        const normalized = lang.toLowerCase();
        let highlighted: string;
        try {
          if (normalized && hljs.getLanguage(normalized)) {
            highlighted = hljs.highlight(trimmed, { language: normalized }).value;
          } else {
            highlighted = hljs.highlightAuto(trimmed, ['csharp', 'json', 'bash', 'xml']).value;
          }
        } catch {
          highlighted = trimmed
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
        }
        return `<pre class="code-block"><div class="code-lang">${normalized || 'code'}</div><code class="hljs">${highlighted}</code></pre>`;
      })
      .replace(/`([^`]+)`/g, '<code>$1</code>')
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/\*(.+?)\*/g, '<em>$1</em>')
      .replace(/^### (.+)$/gm, '<h3>$1</h3>')
      .replace(/^## (.+)$/gm, '<h2>$1</h2>')
      .replace(/^# (.+)$/gm, '<h1>$1</h1>')
      .replace(/^\| (.+) \|$/gm, (_, row) =>
        `<tr>${row.split('|').map((c: string) => `<td>${c.trim()}</td>`).join('')}</tr>`)
      .replace(/^- (.+)$/gm, '<li>$1</li>')
      .replace(/\n\n/g, '</p><p>')
      .trim();
  }
}
