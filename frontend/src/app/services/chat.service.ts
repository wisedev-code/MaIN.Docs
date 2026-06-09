import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ArtifactProposal, IssueProposal, PlanProposal, PrReviewProposal, CodeChangeProposal, PrProposal, PresentedCodeFile, ReviewPosted, ChatMessage, ToolUsage } from '../models/chat.models';

const API_URL = '/api/chat/complete';
const API_KEY = (window as any).__env?.apiKey ?? 'change-me-before-deploy';

interface ChatApiResponse {
  text: string;
  toolsUsed: ToolUsage[];
  estimatedTokens: number;
  artifactUrl?: string;
  artifactProposed?: ArtifactProposal;
  issueProposed?: IssueProposal;
  issueUrl?: string;
  planProposed?: PlanProposal;
  reviewProposed?: PrReviewProposal;
  codeChangeProposed?: CodeChangeProposal;
  prProposed?: PrProposal;
  prUrl?: string;
  presentedCode?: PresentedCodeFile[];
  reviewPosted?: ReviewPosted;
}

export interface AgentResponse {
  content: string;
  toolsUsed: ToolUsage[];
  estimatedTokens: number;
  artifactUrl?: string;
  artifactProposed?: ArtifactProposal;
  issueProposed?: IssueProposal;
  issueUrl?: string;
  planProposed?: PlanProposal;
  reviewProposed?: PrReviewProposal;
  codeChangeProposed?: CodeChangeProposal;
  prProposed?: PrProposal;
  prUrl?: string;
  presentedCode?: PresentedCodeFile[];
  reviewPosted?: ReviewPosted;
}

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);
  private abortController: AbortController | null = null;

  abort() {
    this.abortController?.abort();
    this.abortController = null;
  }

  async sendMessage(agentId: string, message: string, history: ChatMessage[]): Promise<AgentResponse> {
    this.abortController = new AbortController();

    const body = {
      agentId,
      message,
      history: history.map(m => ({ role: m.role, content: m.content })),
    };

    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'X-Api-Key': API_KEY,
    });

    const response = await firstValueFrom(
      this.http.post<ChatApiResponse>(API_URL, body, { headers })
    );

    this.abortController = null;
    return { content: response.text, toolsUsed: response.toolsUsed, estimatedTokens: response.estimatedTokens, artifactUrl: response.artifactUrl, artifactProposed: response.artifactProposed, issueProposed: response.issueProposed, issueUrl: response.issueUrl, planProposed: response.planProposed, reviewProposed: response.reviewProposed, codeChangeProposed: response.codeChangeProposed, prProposed: response.prProposed, prUrl: response.prUrl, presentedCode: response.presentedCode, reviewPosted: response.reviewPosted };
  }
}
