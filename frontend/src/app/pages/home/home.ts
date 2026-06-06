import { Component, ElementRef, OnDestroy, ViewChild, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ChatService } from '../../services/chat.service';
import { ChatMessage, AgentDefinition, AGENTS } from '../../models/chat.models';

@Component({
  selector: 'app-home',
  imports: [FormsModule],
  templateUrl: './home.html',
  styleUrl: './home.scss'
})
export class Home implements OnDestroy {
  @ViewChild('messagesEl') messagesEl!: ElementRef<HTMLDivElement>;

  agents = AGENTS;
  selectedAgent = signal<AgentDefinition>(AGENTS[0]);
  hoveredAgent = signal<AgentDefinition | null>(null);
  displayedAgent = signal<AgentDefinition>(AGENTS[0]);
  messages = signal<ChatMessage[]>([]);
  inputText = signal('');
  isStreaming = signal(false);

  hasMessages = computed(() => this.messages().length > 0);

  constructor(private chatService: ChatService) {}

  selectAgent(agent: AgentDefinition) {
    this.selectedAgent.set(agent);
  }

  showAgentInfo(agent: AgentDefinition) {
    this.displayedAgent.set(agent);
    this.hoveredAgent.set(agent);
  }

  hideAgentInfo() {
    this.hoveredAgent.set(null);
    // displayedAgent intentionally kept — panel fades out with content still visible
  }

  async send() {
    const text = this.inputText().trim();
    if (!text || this.isStreaming()) return;

    const history = this.messages().slice();
    this.messages.update(m => [...m, { role: 'user', content: text }]);
    this.inputText.set('');
    this.messages.update(m => [...m, { role: 'assistant', content: '', streaming: true }]);
    this.isStreaming.set(true);
    setTimeout(() => this.scrollToBottom(), 50);

    try {
      const response = await this.chatService.sendMessage(
        this.selectedAgent().id, text, history
      );
      this.messages.update(msgs => {
        const updated = [...msgs];
        updated[updated.length - 1] = {
          ...updated[updated.length - 1],
          content: response,
          streaming: false,
        };
        return updated;
      });
      this.scrollToBottom();
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

  clear() { this.messages.set([]); }

  onKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  private scrollToBottom() {
    const el = this.messagesEl?.nativeElement;
    if (el) el.scrollTop = el.scrollHeight;
  }

  ngOnDestroy() { this.chatService.abort(); }

  renderMarkdown(content: string): string {
    return content
      .replace(/```(\w*)\n([\s\S]*?)```/g, '<pre><code class="language-$1">$2</code></pre>')
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
