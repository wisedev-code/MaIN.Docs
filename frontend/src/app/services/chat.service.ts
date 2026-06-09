import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ArtifactProposal, IssueProposal, PlanProposal, PrReviewProposal, CodeChangeProposal, PrProposal, PresentedCodeFile, ReviewPosted, ChatMessage, ToolUsage } from '../models/chat.models';

const API_URL = '/api/chat/complete';
const CONFIRM_REVIEW_URL = '/api/confirm/review';
const CONFIRM_CODE_CHANGE_URL = '/api/confirm/code-change';
const CONFIRM_PR_URL = '/api/confirm/pr';
const API_KEY = (window as any).__env?.apiKey ?? 'change-me-before-deploy';

const ENSEMBLE_DESIGN_URL = '/api/ensemble/design';
const ENSEMBLE_CODE_URL = '/api/ensemble/code';
const ENSEMBLE_REVIEW_URL = '/api/ensemble/review';

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

interface EnsembleCodeApiResponse {
  text: string;
  toolsUsed: ToolUsage[];
  estimatedTokens: number;
  branchName: string;
  filesChanged: number;
  codeChangeProposed?: CodeChangeProposal;
  prProposed?: PrProposal;
}

export interface EnsembleCodeResponse {
  content: string;
  toolsUsed: ToolUsage[];
  estimatedTokens: number;
  branchName: string;
  filesChanged: number;
  codeChangeProposed?: CodeChangeProposal;
  prProposed?: PrProposal;
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

  private get authHeaders(): HttpHeaders {
    return new HttpHeaders({ 'Content-Type': 'application/json', 'X-Api-Key': API_KEY });
  }

  async confirmReview(): Promise<string> {
    const res = await firstValueFrom(this.http.post<{ url: string }>(CONFIRM_REVIEW_URL, {}, { headers: this.authHeaders }));
    return res.url;
  }

  async confirmCodeChange(): Promise<void> {
    await firstValueFrom(this.http.post(CONFIRM_CODE_CHANGE_URL, {}, { headers: this.authHeaders }));
  }

  async confirmPr(): Promise<string> {
    const res = await firstValueFrom(this.http.post<{ url: string }>(CONFIRM_PR_URL, {}, { headers: this.authHeaders }));
    return res.url;
  }

  async sendEnsembleDesign(message: string, history: ChatMessage[]): Promise<AgentResponse> {
    const body = { message, history: history.map(m => ({ role: m.role, content: m.content })) };
    const response = await firstValueFrom(
      this.http.post<ChatApiResponse>(ENSEMBLE_DESIGN_URL, body, { headers: this.authHeaders })
    );
    return { content: response.text, toolsUsed: response.toolsUsed, estimatedTokens: response.estimatedTokens,
      planProposed: response.planProposed, issueProposed: response.issueProposed, issueUrl: response.issueUrl };
  }

  async sendEnsembleCode(originalMessage: string, designContent: string): Promise<EnsembleCodeResponse> {
    const body = { originalMessage, designContent };
    const response = await firstValueFrom(
      this.http.post<EnsembleCodeApiResponse>(ENSEMBLE_CODE_URL, body, { headers: this.authHeaders })
    );
    return { content: response.text, toolsUsed: response.toolsUsed, estimatedTokens: response.estimatedTokens,
      branchName: response.branchName, filesChanged: response.filesChanged,
      codeChangeProposed: response.codeChangeProposed, prProposed: response.prProposed };
  }

  async sendEnsembleReview(originalMessage: string, designContent: string, codeContent: string, branchName: string): Promise<AgentResponse> {
    const body = { originalMessage, designContent, codeContent, branchName };
    const response = await firstValueFrom(
      this.http.post<ChatApiResponse>(ENSEMBLE_REVIEW_URL, body, { headers: this.authHeaders })
    );
    return { content: response.text, toolsUsed: response.toolsUsed, estimatedTokens: response.estimatedTokens,
      reviewProposed: response.reviewProposed, codeChangeProposed: response.codeChangeProposed,
      prProposed: response.prProposed, prUrl: response.prUrl };
  }
}
