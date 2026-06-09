import { Component, ElementRef, OnDestroy, ViewChild, signal, computed, effect, NgZone } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SlicePipe } from '@angular/common';
import { ChatService } from '../../services/chat.service';
import { AppStateService } from '../../services/app-state.service';
import { ArtifactProposal, IssueProposal, PlanProposal, PrReviewProposal, CodeChangeProposal, PrProposal, PresentedCodeFile, ReviewPosted, ChatMessage, ToolUsage, AgentDefinition, AGENTS, Attachment } from '../../models/chat.models';

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
  imports: [FormsModule, SlicePipe],
  templateUrl: './home.html',
  styleUrl: './home.scss'
})
export class Home implements OnDestroy {
  @ViewChild('messagesEl') messagesEl!: ElementRef<HTMLDivElement>;
  @ViewChild('textareaEl') textareaEl!: ElementRef<HTMLTextAreaElement>;
  @ViewChild('fileInputEl') fileInputEl!: ElementRef<HTMLInputElement>;

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
  showIssuePrompt = signal(false);
  issueProposal = signal<IssueProposal | null>(null);
  showReviewPrompt = signal(false);
  reviewProposal = signal<PrReviewProposal | null>(null);
  showCodeChangePrompt = signal(false);
  codeChangeProposal = signal<CodeChangeProposal | null>(null);
  showPrPrompt = signal(false);
  prProposal = signal<PrProposal | null>(null);
  attachments = signal<Attachment[]>([]);
  forgeStage = signal<'design' | 'code' | 'review'>('design');
  completedForgeStages = signal<string[]>([]);
  showForgeNextPrompt = signal(false);
  lastFailedInput = signal('');

  hasMessages = computed(() => this.messages().length > 0);

  private chatResetSnapshot = 0;

  private static readonly COPY_ICON = `<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="9" y="9" width="13" height="13" rx="2"></rect><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path></svg>`;

  constructor(private chatService: ChatService, private appStateService: AppStateService, private ngZone: NgZone) {
    this.chatResetSnapshot = this.appStateService.chatReset();
    effect(() => {
      const v = this.appStateService.chatReset();
      if (v > this.chatResetSnapshot) {
        this.chatResetSnapshot = v;
        this.clear();
      }
    }, { allowSignalWrites: true });
  }

  selectAgent(agent: AgentDefinition) {
    if (this.selectedAgent().id === 'forge' && agent.id !== 'forge') {
      this.forgeStage.set('design');
      this.completedForgeStages.set([]);
      this.showForgeNextPrompt.set(false);
    }
    this.selectedAgent.set(agent);
  }

  showAgentInfo(agent: AgentDefinition) {
    this.displayedAgent.set(agent);
    this.hoveredAgent.set(agent);
  }

  hideAgentInfo() { this.hoveredAgent.set(null); }

  async send() {
    const rawText = this.inputText().trim();
    if (!rawText && this.attachments().length === 0) return;
    if (this.isStreaming()) return;

    this.showArtifactPrompt.set(false);
    this.showIssuePrompt.set(false);
    this.showReviewPrompt.set(false);
    this.showCodeChangePrompt.set(false);
    this.showPrPrompt.set(false);
    this.showForgeNextPrompt.set(false);

    // Stage prefix silently added for Forge — chat shows clean text, API gets tagged text
    const stagePrefix = this.selectedAgent().id === 'forge'
      ? `[${this.forgeStage().toUpperCase()} STAGE] `
      : '';
    const apiText = stagePrefix + rawText;

    this.lastFailedInput.set(rawText);
    const currentAttachments = this.attachments();
    const history = this.messages().slice();
    this.messages.update(m => [...m, { role: 'user', content: rawText, attachments: currentAttachments }]);
    this.inputText.set('');
    this.attachments.set([]);
    this.resetTextareaHeight();
    this.messages.update(m => [...m, { role: 'assistant', content: '', streaming: true }]);
    this.currentFace.set(this.ASCII_FACES[Math.floor(Math.random() * this.ASCII_FACES.length)]);
    this.isStreaming.set(true);
    setTimeout(() => this.scrollToBottom(), 60);

    try {
      const response = await this.chatService.sendMessage(this.selectedAgent().id, apiText, history);
      const msgIndex = this.messages().length - 1;
      await this.revealWordByWord(response.content, msgIndex, response.toolsUsed, response.estimatedTokens, response.artifactUrl, response.artifactProposed, response.issueProposed, response.issueUrl, response.planProposed, response.reviewProposed, response.codeChangeProposed, response.prProposed, response.prUrl, response.presentedCode, response.reviewPosted);

      if (response.artifactProposed && !response.artifactUrl) {
        this.artifactProposal.set(response.artifactProposed);
        this.showArtifactPrompt.set(true);
      }
      if (response.issueProposed && !response.issueUrl) {
        this.issueProposal.set(response.issueProposed);
        this.showIssuePrompt.set(true);
      }
      if (response.reviewProposed && !response.prUrl) {
        this.reviewProposal.set(response.reviewProposed);
        this.showReviewPrompt.set(true);
      }
      if (response.codeChangeProposed) {
        this.codeChangeProposal.set(response.codeChangeProposed);
        this.showCodeChangePrompt.set(true);
      }
      if (response.prProposed && !response.prUrl) {
        this.prProposal.set(response.prProposed);
        this.showPrPrompt.set(true);
      }
      if (this.selectedAgent().id === 'forge') {
        if (this.forgeStage() === 'design' && response.planProposed) {
          this.showForgeNextPrompt.set(true);
        } else if (this.forgeStage() === 'code' && (response.presentedCode?.length || response.artifactProposed || response.artifactUrl || response.prProposed)) {
          this.showForgeNextPrompt.set(true);
        }
      }
    } catch {
      this.messages.update(msgs => {
        const updated = [...msgs];
        updated[updated.length - 1] = {
          ...updated[updated.length - 1],
          content: '',
          streaming: false,
          isError: true,
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
    artifactProposed?: ArtifactProposal,
    issueProposed?: IssueProposal,
    issueUrl?: string,
    planProposed?: PlanProposal,
    reviewProposed?: PrReviewProposal,
    codeChangeProposed?: CodeChangeProposal,
    prProposed?: PrProposal,
    prUrl?: string,
    presentedCode?: PresentedCodeFile[],
    reviewPosted?: ReviewPosted
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
        issueProposed,
        issueUrl,
        planProposed,
        reviewProposed,
        codeChangeProposed,
        prProposed,
        prUrl,
        presentedCode,
        reviewPosted,
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

  dismissIssuePrompt() {
    this.showIssuePrompt.set(false);
    this.issueProposal.set(null);
  }

  requestIssue() {
    const proposal = this.issueProposal();
    this.dismissIssuePrompt();
    const hint = proposal ? ` Title: "${proposal.title}".` : '';
    this.inputText.set(`Please create the GitHub issue now.${hint}`);
    this.send();
  }

  dismissReviewPrompt() {
    this.showReviewPrompt.set(false);
    this.reviewProposal.set(null);
  }

  requestReview() {
    const proposal = this.reviewProposal();
    this.dismissReviewPrompt();
    const hint = proposal ? ` PR #${proposal.prNumber}, verdict: ${proposal.verdict}.` : '';
    this.inputText.set(`Please submit the PR review now.${hint}`);
    this.send();
  }

  dismissCodeChangePrompt() {
    this.showCodeChangePrompt.set(false);
    this.codeChangeProposal.set(null);
  }

  requestCodeChange() {
    const proposal = this.codeChangeProposal();
    this.dismissCodeChangePrompt();
    const hint = proposal ? ` File: ${proposal.filePath} on branch ${proposal.branch}.` : '';
    this.inputText.set(`Please push the file change to the branch now.${hint}`);
    this.send();
  }

  dismissPrPrompt() {
    this.showPrPrompt.set(false);
    this.prProposal.set(null);
  }

  requestPr() {
    const proposal = this.prProposal();
    this.dismissPrPrompt();
    const hint = proposal ? ` Title: "${proposal.title}", from ${proposal.headBranch} to ${proposal.baseBranch}.` : '';
    this.inputText.set(`Please create the pull request now.${hint}`);
    this.send();
  }

  dismissCombinedPrompt() {
    this.showCodeChangePrompt.set(false);
    this.codeChangeProposal.set(null);
    this.showPrPrompt.set(false);
    this.prProposal.set(null);
  }

  requestCombined() {
    const code = this.codeChangeProposal();
    const pr = this.prProposal();
    this.dismissCombinedPrompt();
    const branchHint = code ? ` Branch: ${code.branch}.` : '';
    const prHint = pr ? ` PR: "${pr.title}" from ${pr.headBranch} to ${pr.baseBranch}.` : '';
    this.inputText.set(`Confirmed. Call push_file_to_branch for EVERY file from your propose_code_change calls in this conversation, then call create_pull_request.${branchHint}${prHint} Do not call propose_code_change or propose_pull_request again.`);
    this.send();
  }

  advanceForgeStage() {
    const current = this.forgeStage();
    this.completedForgeStages.update(s => [...s, current]);
    this.showForgeNextPrompt.set(false);
    const next = current === 'design' ? 'code' : 'review';
    this.forgeStage.set(next);
    
    let msg = '';
    if (next === 'code') {
      msg = 'The design plan is approved. Proceed to CODE STAGE. Use present_code to show all files — do NOT call propose_code_change or propose_pull_request here, those are REVIEW STAGE tools.';
    } else {
      msg = 'The code is ready. Proceed to REVIEW STAGE: verify API usage against the docs, then call propose_code_change for every file and propose_pull_request once. Do not call present_code.';
    }
    
    this.inputText.set(msg);
    this.send();
  }

  dismissForgeNextPrompt() { this.showForgeNextPrompt.set(false); }

  retryLastMessage() {
    const text = this.lastFailedInput();
    if (!text || this.isStreaming()) return;
    this.messages.update(msgs => msgs.slice(0, -2));
    this.lastFailedInput.set('');
    this.inputText.set(text);
    this.send();
  }

  isForgeStageCompleted(stage: string): boolean { return this.completedForgeStages().includes(stage); }

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
    this.showIssuePrompt.set(false);
    this.issueProposal.set(null);
    this.showReviewPrompt.set(false);
    this.reviewProposal.set(null);
    this.showCodeChangePrompt.set(false);
    this.codeChangeProposal.set(null);
    this.showPrPrompt.set(false);
    this.prProposal.set(null);
    this.attachments.set([]);
    this.forgeStage.set('design');
    this.completedForgeStages.set([]);
    this.showForgeNextPrompt.set(false);
    this.resetTextareaHeight();
  }

  onPaste(event: ClipboardEvent) {
    const items = event.clipboardData?.items;
    if (!items) return;
    for (const item of Array.from(items)) {
      if (item.type.startsWith('image/')) {
        event.preventDefault();
        const file = item.getAsFile();
        if (file) this.readFile(file);
      }
    }
  }

  openFileDialog() { this.fileInputEl?.nativeElement.click(); }

  onFileSelect(event: Event) {
    const files = (event.target as HTMLInputElement).files;
    if (!files) return;
    Array.from(files).forEach(f => this.readFile(f));
    (event.target as HTMLInputElement).value = '';
  }

  private readFile(file: File) {
    const reader = new FileReader();
    reader.onload = (e) => {
      const dataUrl = e.target?.result as string;
      this.ngZone.run(() => {
        this.attachments.update(prev => [...prev, {
          id: crypto.randomUUID(),
          name: file.name,
          type: file.type.startsWith('image/') ? 'image' : 'file',
          dataUrl,
          mimeType: file.type,
        }]);
      });
    };
    reader.readAsDataURL(file);
  }

  removeAttachment(id: string) {
    this.attachments.update(prev => prev.filter(a => a.id !== id));
  }

  autoResize(event: Event) {
    const el = event.target as HTMLTextAreaElement;
    el.style.height = 'auto';
    el.style.height = Math.min(el.scrollHeight, 240) + 'px';
  }

  private resetTextareaHeight() {
    const el = this.textareaEl?.nativeElement;
    if (el) el.style.height = 'auto';
  }

  renderStepCode(code: string, language?: string): string {
    const lang = (language ?? '').toLowerCase();
    let highlighted: string;
    try {
      if (lang && hljs.getLanguage(lang)) {
        highlighted = hljs.highlight(code, { language: lang }).value;
      } else {
        highlighted = hljs.highlightAuto(code, ['csharp', 'json', 'bash', 'xml']).value;
      }
    } catch {
      highlighted = code.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }
    return `<div class="code-block code-block-step"><div class="code-header"><span class="code-lang">${lang || 'code'}</span><button class="copy-btn" title="Copy code">${Home.COPY_ICON}</button></div><pre><code class="hljs">${highlighted}</code></pre></div>`;
  }

  renderPresentedCode(content: string, language: string): string {
    const lang = language.toLowerCase();
    let highlighted: string;
    try {
      if (lang && hljs.getLanguage(lang)) {
        highlighted = hljs.highlight(content, { language: lang }).value;
      } else {
        highlighted = hljs.highlightAuto(content, ['csharp', 'json', 'bash', 'xml']).value;
      }
    } catch {
      highlighted = content.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }
    return `<pre><code class="hljs">${highlighted}</code></pre>`;
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
    return toolsUsed.reduce((sum, t) => sum + t.calls * 540, 0);
  }

  private scrollToBottom() {
    const el = this.messagesEl?.nativeElement;
    if (el) el.scrollTo({ top: el.scrollHeight, behavior: 'smooth' });
  }

  onMessagesClick(event: MouseEvent) {
    const btn = (event.target as Element).closest<HTMLButtonElement>('.copy-btn');
    if (!btn) return;
    const code = btn.closest('.code-block')?.querySelector('code');
    if (code) {
      const original = btn.innerHTML;
      navigator.clipboard.writeText(code.textContent ?? '').then(() => {
        btn.textContent = 'Copied!';
        btn.classList.add('copied');
        setTimeout(() => {
          btn.innerHTML = original;
          btn.classList.remove('copied');
        }, 2000);
      }).catch(() => {});
    }
  }

  ngOnDestroy() { this.chatService.abort(); }

  renderMarkdown(content: string, artifactUrl?: string): string {
    // Strip the raw artifact download link from the text — the card already shows it.
    let text = content;
    if (artifactUrl) {
      text = text.replace(/\[.*?\]\(https?:\/\/[^\)]+\)/g, '').replace(/https?:\/\/\S+\.zip\S*/g, '');
    }

    return text
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
        return `<div class="code-block"><div class="code-header"><span class="code-lang">${normalized || 'code'}</span><button class="copy-btn" title="Copy code">${Home.COPY_ICON}</button></div><pre><code class="hljs">${highlighted}</code></pre></div>`;
      })
      .replace(/`([^`]+)`/g, '<code>$1</code>')
      .replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2" target="_blank" rel="noopener noreferrer">$1</a>')
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/\*(.+?)\*/g, '<em>$1</em>')
      .replace(/^#### (.+)$/gm, '<h4>$1</h4>')
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
