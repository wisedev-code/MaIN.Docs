import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ChatMessage } from '../models/chat.models';

const API_URL = '/api/chat/complete';
const API_KEY = (window as any).__env?.apiKey ?? 'change-me-before-deploy';

interface ChatApiResponse {
  text: string;
}

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);
  private abortController: AbortController | null = null;

  abort() {
    this.abortController?.abort();
    this.abortController = null;
  }

  async sendMessage(agentId: string, message: string, history: ChatMessage[]): Promise<string> {
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
    return response.text;
  }
}
