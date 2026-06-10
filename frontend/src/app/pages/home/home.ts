import { Component, ElementRef, OnDestroy, ViewChild, signal, computed, effect, NgZone } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SlicePipe } from '@angular/common';
import { ChatService } from '../../services/chat.service';
import { AppStateService } from '../../services/app-state.service';
import { ArtifactProposal, IssueProposal, PlanProposal, PrReviewProposal, CodeChangeProposal, PrProposal, ReviewPosted, ChatMessage, ToolUsage, AgentDefinition, AGENTS, Attachment } from '../../models/chat.models';

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

interface SavedConversation {
  id: string;
  agentId: string;
  preview: string;
  messages: ChatMessage[];
  timestamp: number;
}

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
    // Lenny weighted 5× — he earned it
    '( ͡° ͜ʖ ͡°)', '( ͡° ͜ʖ ͡°)', '( ͡° ͜ʖ ͡°)', '( ͡° ͜ʖ ͡°)', '( ͡° ͜ʖ ͡°)',
    // Lenny variants
    '( ͡~ ͜ʖ ͡°)', '( ͡o ͜ʖ ͡o)', '( ͡° ͜ʖ ͡-)', '(͡ ͡° ͜ ʖ ͡ ͡°)',
    // originals
    '(ง •̀_•́)ง', '(⌐■_■)', '¯\\_(ツ)_/¯', '(╯°□°）╯', '(ಠ_ಠ)',
    '(◕‿◕)', 'ʕ•ᴥ•ʔ', '(¬‿¬)', '(づ｡◕‿◕｡)づ', '(•_•)',
    '(ᵔᴥᵔ)', '(っ˘ω˘ς)', '(ó_ò)', '(≖_≖)', '(งツ)ว',
    // new additions
    '(▀̿Ĺ̯▀̿ ̿)', '(╬ ಠ益ಠ)', 'ᕙ(⇀‸↼‶)ᕗ', '(ノ°◡°)ノ︵ ┻━┻',
    '༼ つ ◕_◕ ༽つ', '(งಠ_ಠ)ง', 'ヽ(°〇°)ﾉ', '( ˘ ³˘)♥',
    '(づ￣ ³￣)づ', '乁( ◔ ౪◔)「', '(ಥ﹏ಥ)', '┬──┬ ノ(°–°ノ)',
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
  reviewSuccess = signal<{ url: string; prNumber: number; verdict: string } | null>(null);
  private reviewSuccessTimer: ReturnType<typeof setTimeout> | null = null;
  showCodeChangePrompt = signal(false);
  codeChangeProposal = signal<CodeChangeProposal | null>(null);
  showPrPrompt = signal(false);
  prProposal = signal<PrProposal | null>(null);
  attachments = signal<Attachment[]>([]);
  forgeStage = signal<'design' | 'code' | 'review'>('design');
  completedForgeStages = signal<string[]>([]);
  showForgeNextPrompt = signal(false);
  lastFailedInput = signal('');

  ensembleStep = signal<'idle' | 'design' | 'code' | 'review' | 'done'>('idle');
  showEnsembleAccept = signal(false);
  ensembleContext = signal<{ question: string; designContent?: string; codeContent?: string; branchName?: string } | null>(null);

  recentConversations = signal<SavedConversation[]>([]);

  runModalArtifactUrl = signal<string | null>(null);
  runPanelOs = signal<'unix' | 'windows'>(this.detectOs());

  hasMessages = computed(() => this.messages().length > 0);

  currentEnsembleStageLabel = computed(() => {
    switch (this.ensembleStep()) {
      case 'design': return 'Design stage complete';
      case 'code':   return 'Code stage complete — files queued';
      default: return '';
    }
  });

  nextEnsembleStageLabel = computed(() => {
    switch (this.ensembleStep()) {
      case 'design': return 'Continue → Code Stage';
      case 'code':   return 'Push Files & Review →';
      default: return 'Continue →';
    }
  });

  private chatResetSnapshot = 0;

  private static readonly COPY_ICON = `<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="9" y="9" width="13" height="13" rx="2"></rect><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path></svg>`;

  constructor(private chatService: ChatService, private appStateService: AppStateService, private ngZone: NgZone) {
    this.chatResetSnapshot = this.appStateService.chatReset();
    this.loadConversationsFromStorage();
    effect(() => {
      const v = this.appStateService.chatReset();
      if (v > this.chatResetSnapshot) {
        this.chatResetSnapshot = v;
        this.clear();
      }
    }, { allowSignalWrites: true });
  }

  selectAgent(agent: AgentDefinition) {
    if (this.selectedAgent().id === agent.id) return;
    if (this.hasMessages()) this.clear();
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
    const isFlow = this.selectedAgent().id === 'flow';
    this.messages.update(m => [...m, { role: 'assistant', content: '', streaming: true, agentId: isFlow ? 'design' : undefined }]);
    this.currentFace.set(this.ASCII_FACES[Math.floor(Math.random() * this.ASCII_FACES.length)]);
    this.isStreaming.set(true);
    setTimeout(() => this.scrollToBottom(), 60);

    try {
      if (isFlow) {
        this.ensembleStep.set('design');
        const msgIndex = this.messages().length - 1;
        const response = await this.chatService.sendEnsembleDesign(rawText, history);
        await this.revealWordByWord(response.content, msgIndex, response.toolsUsed, response.estimatedTokens,
          undefined, undefined, response.issueProposed, response.issueUrl, response.planProposed);
        this.ensembleContext.set({ question: rawText, designContent: response.content });
        if (response.issueProposed && !response.issueUrl) {
          this.issueProposal.set(response.issueProposed);
          this.showIssuePrompt.set(true);
        }
        this.showEnsembleAccept.set(true);
      } else {
        const response = await this.chatService.sendMessage(this.selectedAgent().id, apiText, history);
        const msgIndex = this.messages().length - 1;
        await this.revealWordByWord(response.content, msgIndex, response.toolsUsed, response.estimatedTokens, response.artifactUrl, response.artifactProposed, response.issueProposed, response.issueUrl, response.planProposed, response.reviewProposed, response.codeChangeProposed, response.prProposed, response.prUrl, response.reviewPosted, response.docsRead);

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
          } else if (this.forgeStage() === 'code' && (response.content?.trim().length || response.artifactProposed || response.artifactUrl || response.prProposed)) {
            this.showForgeNextPrompt.set(true);
          }
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
      this.saveToRecent();
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
    reviewPosted?: ReviewPosted,
    docsRead?: string[]
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
        reviewPosted,
        docsRead,
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

  async requestReview() {
    const proposal = this.reviewProposal();
    this.dismissReviewPrompt();
    try {
      const url = await this.chatService.confirmReview();
      this.messages.update(msgs => {
        const updated = [...msgs];
        const lastAssistant = [...updated].reverse().findIndex(m => m.role === 'assistant');
        if (lastAssistant !== -1) {
          const idx = updated.length - 1 - lastAssistant;
          updated[idx] = { ...updated[idx], reviewUrl: url };
        }
        return updated;
      });
      if (this.reviewSuccessTimer) clearTimeout(this.reviewSuccessTimer);
      this.reviewSuccess.set({ url, prNumber: proposal?.prNumber ?? 0, verdict: proposal?.verdict ?? '' });
      this.reviewSuccessTimer = setTimeout(() => this.reviewSuccess.set(null), 6000);
    } catch {
      this.inputText.set('Please submit the PR review now.');
      this.send();
    }
  }

  dismissReviewSuccess() {
    if (this.reviewSuccessTimer) clearTimeout(this.reviewSuccessTimer);
    this.reviewSuccess.set(null);
  }

  dismissCodeChangePrompt() {
    this.showCodeChangePrompt.set(false);
    this.codeChangeProposal.set(null);
  }

  async requestCodeChange() {
    this.dismissCodeChangePrompt();
    try {
      await this.chatService.confirmCodeChange();
    } catch {
      this.inputText.set('Please push the file change to the branch now.');
      this.send();
    }
  }

  dismissPrPrompt() {
    this.showPrPrompt.set(false);
    this.prProposal.set(null);
  }

  async requestPr() {
    const isForgeReview = this.selectedAgent().id === 'forge' && this.forgeStage() === 'review';
    this.dismissPrPrompt();
    if (isForgeReview) {
      this.inputText.set('Confirmed.');
      this.send();
      return;
    }
    try {
      const url = await this.chatService.confirmPr();
      this.messages.update(msgs => {
        const updated = [...msgs];
        const lastAssistant = [...updated].reverse().findIndex(m => m.role === 'assistant');
        if (lastAssistant !== -1) {
          const idx = updated.length - 1 - lastAssistant;
          updated[idx] = { ...updated[idx], prUrl: url };
        }
        return updated;
      });
    } catch {
      this.inputText.set('Please create the pull request now.');
      this.send();
    }
  }

  getAgentById(id?: string): AgentDefinition {
    if (!id) return this.selectedAgent();
    return AGENTS.find(a => a.id === id) ?? this.selectedAgent();
  }

  isDone(stage: 'design' | 'code' | 'review'): boolean {
    const order = ['design', 'code', 'review', 'done'];
    const cur = this.ensembleStep();
    return order.indexOf(cur) > order.indexOf(stage);
  }

  dismissEnsembleFlow() {
    this.showEnsembleAccept.set(false);
    this.ensembleStep.set('idle');
    this.ensembleContext.set(null);
  }

  async acceptEnsembleStage() {
    const step = this.ensembleStep();
    this.showEnsembleAccept.set(false);

    if (step === 'design') {
      const ctx = this.ensembleContext()!;
      this.ensembleStep.set('code');
      this.messages.update(m => [...m, { role: 'assistant', content: '', streaming: true, agentId: 'code' }]);
      const msgIndex = this.messages().length - 1;
      this.currentFace.set(this.ASCII_FACES[Math.floor(Math.random() * this.ASCII_FACES.length)]);
      this.isStreaming.set(true);
      setTimeout(() => this.scrollToBottom(), 60);

      try {
        const response = await this.chatService.sendEnsembleCode(ctx.question, ctx.designContent!);
        this.ensembleContext.update(c => c ? { ...c, codeContent: response.content, branchName: response.branchName } : null);
        await this.revealWordByWord(response.content, msgIndex, response.toolsUsed, response.estimatedTokens,
          undefined, undefined, undefined, undefined, undefined, undefined,
          response.codeChangeProposed, response.prProposed);
        this.showEnsembleAccept.set(true);
      } catch {
        this.messages.update(msgs => {
          const updated = [...msgs];
          updated[updated.length - 1] = { ...updated[updated.length - 1], content: 'Code stage failed. Please try again.', streaming: false };
          return updated;
        });
      } finally {
        this.isStreaming.set(false);
        this.saveToRecent();
      }

    } else if (step === 'code') {
      const ctx = this.ensembleContext()!;
      this.ensembleStep.set('review');
      this.messages.update(m => [...m, { role: 'assistant', content: '', streaming: true, agentId: 'review' }]);
      const msgIndex = this.messages().length - 1;
      this.currentFace.set(this.ASCII_FACES[Math.floor(Math.random() * this.ASCII_FACES.length)]);
      this.isStreaming.set(true);
      setTimeout(() => this.scrollToBottom(), 60);

      try {
        try { await this.chatService.confirmCodeChange(); } catch { /* no pending changes, proceed */ }
        const response = await this.chatService.sendEnsembleReview(
          ctx.question, ctx.designContent!, ctx.codeContent!, ctx.branchName!);
        await this.revealWordByWord(response.content, msgIndex, response.toolsUsed, response.estimatedTokens,
          undefined, undefined, undefined, undefined, undefined, response.reviewProposed,
          response.codeChangeProposed, response.prProposed, response.prUrl);
        this.ensembleStep.set('done');

        if (response.reviewProposed && !response.prUrl) {
          this.reviewProposal.set(response.reviewProposed);
          this.showReviewPrompt.set(true);
        }
        if (response.prProposed && !response.prUrl) {
          this.prProposal.set(response.prProposed);
          this.showPrPrompt.set(true);
        }
      } catch {
        this.messages.update(msgs => {
          const updated = [...msgs];
          updated[updated.length - 1] = { ...updated[updated.length - 1], content: 'Review stage failed. Please try again.', streaming: false };
          return updated;
        });
      } finally {
        this.isStreaming.set(false);
        this.saveToRecent();
      }
    }
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
    
    const msg = next === 'code' ? 'Design approved.' : 'Code complete.';
    
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
    this.reviewSuccess.set(null);
    this.showCodeChangePrompt.set(false);
    this.codeChangeProposal.set(null);
    this.showPrPrompt.set(false);
    this.prProposal.set(null);
    this.attachments.set([]);
    this.forgeStage.set('design');
    this.completedForgeStages.set([]);
    this.showForgeNextPrompt.set(false);
    this.ensembleStep.set('idle');
    this.showEnsembleAccept.set(false);
    this.ensembleContext.set(null);
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

  private detectOs(): 'unix' | 'windows' {
    const ua = navigator.userAgent || (navigator as any).platform || '';
    return /win/i.test(ua) ? 'windows' : 'unix';
  }

  openRunModal(artifactUrl: string) {
    this.runModalArtifactUrl.update(cur => cur === artifactUrl ? null : artifactUrl);
  }

  closeRunModal() {
    this.runModalArtifactUrl.set(null);
  }

  setRunPanelOs(os: 'unix' | 'windows') {
    this.runPanelOs.set(os);
  }

  runCommand(artifactUrl: string, os: 'unix' | 'windows'): string {
    const archiveName = this.extractArchiveName(artifactUrl);
    const origin = window.location.origin;
    if (os === 'windows') {
      return `& ([scriptblock]::Create((irm ${origin}/scripts/run-artifact.ps1))) -ArtifactUrl "${artifactUrl}" -ArchiveName "${archiveName}"`;
    }
    return `curl -fsSL ${origin}/scripts/run-artifact.sh | bash -s -- "${artifactUrl}" "${archiveName}"`;
  }

  copyRunCommand(text: string, event: Event) {
    const btn = (event.currentTarget as HTMLButtonElement);
    navigator.clipboard.writeText(text).then(() => {
      const original = btn.innerHTML;
      btn.textContent = 'Copied!';
      btn.classList.add('copied');
      setTimeout(() => {
        btn.innerHTML = original;
        btn.classList.remove('copied');
      }, 2000);
    }).catch(() => {});
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

  back() { this.clear(); }

  revertForgeStage() {
    const current = this.forgeStage();
    if (current === 'code') {
      this.forgeStage.set('design');
      this.completedForgeStages.update(s => s.filter(x => x !== 'design'));
    } else if (current === 'review') {
      this.forgeStage.set('code');
      this.completedForgeStages.update(s => s.filter(x => x !== 'code'));
    }
    this.showForgeNextPrompt.set(false);
  }

  loadConversation(conv: SavedConversation) {
    if (this.isStreaming()) return;
    this.clear();
    const agent = AGENTS.find(a => a.id === conv.agentId) ?? AGENTS[0];
    this.selectedAgent.set(agent);
    this.displayedAgent.set(agent);
    const cleanMessages = conv.messages.map(m => m.streaming ? { ...m, streaming: false } : m);
    this.messages.set(cleanMessages);
  }

  removeRecentConvo(id: string, event: Event) {
    event.stopPropagation();
    const updated = this.recentConversations().filter(c => c.id !== id);
    this.recentConversations.set(updated);
    try { localStorage.setItem('main-recent-convos', JSON.stringify(updated)); } catch { /**/ }
  }

  private loadConversationsFromStorage() {
    try {
      const raw = localStorage.getItem('main-recent-convos');
      if (raw) this.recentConversations.set(JSON.parse(raw));
    } catch { /**/ }
  }

  private saveToRecent() {
    const msgs = this.messages();
    if (msgs.length === 0) return;
    const firstUser = msgs.find(m => m.role === 'user');
    const preview = (firstUser?.content ?? '').trim().slice(0, 50);
    if (!preview) return;
    const existing = this.recentConversations();
    const cleanMessages = msgs.map(m => m.streaming ? { ...m, streaming: false } : m);
    let updated: SavedConversation[];
    if (existing.length > 0 && existing[0].preview === preview && existing[0].agentId === this.selectedAgent().id) {
      updated = [{ ...existing[0], messages: cleanMessages, timestamp: Date.now() }, ...existing.slice(1)];
    } else {
      const entry: SavedConversation = {
        id: crypto.randomUUID(),
        agentId: this.selectedAgent().id,
        preview,
        messages: cleanMessages,
        timestamp: Date.now(),
      };
      updated = [entry, ...existing].slice(0, 4);
    }
    this.recentConversations.set(updated);
    try { localStorage.setItem('main-recent-convos', JSON.stringify(updated)); } catch { /**/ }
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
