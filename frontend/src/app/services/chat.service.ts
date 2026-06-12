import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ArtifactProposal, IssueProposal, PlanProposal, PrReviewProposal, CodeChangeProposal, PrProposal, ReviewPosted, ChatMessage, ToolUsage, ProposedFile } from '../models/chat.models';

// Empty (docker-compose, nginx proxies /api/) or the unsubstituted `ng serve`
// placeholder both resolve to relative URLs. Only set to an absolute origin
// when the SPA is hosted separately from the backend (e.g. Azure Static Web Apps).
const API_BASE = (() => {
  const raw = (window as any).__env?.apiBaseUrl;
  if (!raw || raw.startsWith('${')) return '';
  return raw.replace(/\/+$/, '');
})();

const API_URL = `${API_BASE}/api/chat/complete`;
const CONFIRM_REVIEW_URL = `${API_BASE}/api/confirm/review`;
const CONFIRM_CODE_CHANGE_URL = `${API_BASE}/api/confirm/code-change`;
const CONFIRM_PR_URL = `${API_BASE}/api/confirm/pr`;

const ENSEMBLE_DESIGN_URL = `${API_BASE}/api/ensemble/design`;
const ENSEMBLE_CODE_URL = `${API_BASE}/api/ensemble/code`;
const ENSEMBLE_REVIEW_URL = `${API_BASE}/api/ensemble/review`;
const ARTIFACT_GENERATE_URL = `${API_BASE}/api/artifact/generate`;

export interface CapacityStatus {
  tier: number;
  level: string;
  tokensUsed: number;
  tokenLimit?: number | null;
  tokensRemaining?: number | null;
  resetsAtUtc?: string | null;
}

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
  reviewPosted?: ReviewPosted;
  docsRead?: string[];
  capacity?: string;
  capacityDetails?: CapacityStatus;
  filesProposed?: ProposedFile[];
}

interface EnsembleCodeApiResponse {
  text: string;
  toolsUsed: ToolUsage[];
  estimatedTokens: number;
  branchName: string;
  filesChanged: number;
  codeChangeProposed?: CodeChangeProposal;
  prProposed?: PrProposal;
  capacity?: string;
  capacityDetails?: CapacityStatus;
}

export interface EnsembleCodeResponse {
  content: string;
  toolsUsed: ToolUsage[];
  estimatedTokens: number;
  branchName: string;
  filesChanged: number;
  codeChangeProposed?: CodeChangeProposal;
  prProposed?: PrProposal;
  capacity?: string;
  capacityDetails?: CapacityStatus;
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
  reviewPosted?: ReviewPosted;
  docsRead?: string[];
  capacity?: string;
  capacityDetails?: CapacityStatus;
  filesProposed?: ProposedFile[];
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

    const docsAlreadyRead = [...new Set(history.flatMap(m => m.docsRead ?? []))];

    const body = {
      agentId,
      message,
      history: history.map(m => ({ role: m.role, content: m.content })),
      ...(docsAlreadyRead.length > 0 ? { docsAlreadyRead } : {}),
    };

    const headers = new HttpHeaders({ 'Content-Type': 'application/json' });

    const response = await firstValueFrom(
      this.http.post<ChatApiResponse>(API_URL, body, { headers })
    );

    this.abortController = null;
    return { content: response.text, toolsUsed: response.toolsUsed, estimatedTokens: response.estimatedTokens, artifactUrl: response.artifactUrl, artifactProposed: response.artifactProposed, issueProposed: response.issueProposed, issueUrl: response.issueUrl, planProposed: response.planProposed, reviewProposed: response.reviewProposed, codeChangeProposed: response.codeChangeProposed, prProposed: response.prProposed, prUrl: response.prUrl, reviewPosted: response.reviewPosted, docsRead: response.docsRead, capacity: response.capacity, capacityDetails: response.capacityDetails, filesProposed: response.filesProposed };
  }

  private get authHeaders(): HttpHeaders {
    return new HttpHeaders({ 'Content-Type': 'application/json' });
  }

  async generateArtifact(archiveName: string, files: ProposedFile[]): Promise<string> {
    const body = { archiveName, files: files.map(f => ({ path: f.path, content: f.content })) };
    const res = await firstValueFrom(
      this.http.post<{ url: string }>(ARTIFACT_GENERATE_URL, body, { headers: this.authHeaders })
    );
    return res.url;
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
      planProposed: response.planProposed, issueProposed: response.issueProposed, issueUrl: response.issueUrl,
      capacity: response.capacity, capacityDetails: response.capacityDetails };
  }

  async sendEnsembleCode(originalMessage: string, designContent: string): Promise<EnsembleCodeResponse> {
    const body = { originalMessage, designContent };
    const response = await firstValueFrom(
      this.http.post<EnsembleCodeApiResponse>(ENSEMBLE_CODE_URL, body, { headers: this.authHeaders })
    );
    return { content: response.text, toolsUsed: response.toolsUsed, estimatedTokens: response.estimatedTokens,
      branchName: response.branchName, filesChanged: response.filesChanged,
      codeChangeProposed: response.codeChangeProposed, prProposed: response.prProposed,
      capacity: response.capacity, capacityDetails: response.capacityDetails };
  }

  async sendEnsembleReview(originalMessage: string, designContent: string, codeContent: string, branchName: string): Promise<AgentResponse> {
    const body = { originalMessage, designContent, codeContent, branchName };
    const response = await firstValueFrom(
      this.http.post<ChatApiResponse>(ENSEMBLE_REVIEW_URL, body, { headers: this.authHeaders })
    );
    return { content: response.text, toolsUsed: response.toolsUsed, estimatedTokens: response.estimatedTokens,
      reviewProposed: response.reviewProposed, codeChangeProposed: response.codeChangeProposed,
      prProposed: response.prProposed, prUrl: response.prUrl, capacity: response.capacity, capacityDetails: response.capacityDetails };
  }
}
