import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

const SESSION_URL = '/api/session';
const TURNSTILE_SCRIPT_URL = 'https://challenges.cloudflare.com/turnstile/v0/api.js';
const TOKEN_EXPIRY_BUFFER_MS = 60_000;
const DEV_TURNSTILE_TOKEN = 'dev-mode';

interface CachedToken {
  token: string;
  expiresAt: number;
}

interface SessionResponse {
  token: string;
  expiresAt: string;
}

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly http = inject(HttpClient);

  private cached: CachedToken | null = null;
  private pending: Promise<string> | null = null;
  private turnstileScriptPromise: Promise<void> | null = null;

  async getToken(forceRefresh = false): Promise<string> {
    if (!forceRefresh && this.cached && this.cached.expiresAt - Date.now() > TOKEN_EXPIRY_BUFFER_MS) {
      return this.cached.token;
    }

    if (this.pending) {
      return this.pending;
    }

    this.pending = this.fetchNewToken().finally(() => {
      this.pending = null;
    });
    return this.pending;
  }

  clearToken(): void {
    this.cached = null;
  }

  private async fetchNewToken(): Promise<string> {
    const turnstileToken = await this.getTurnstileToken();
    const response = await firstValueFrom(
      this.http.post<SessionResponse>(SESSION_URL, { turnstileToken })
    );
    this.cached = { token: response.token, expiresAt: new Date(response.expiresAt).getTime() };
    return response.token;
  }

  private async getTurnstileToken(): Promise<string> {
    const siteKey = (window as any).__env?.turnstileSiteKey;
    // Empty (local dev, no Cloudflare account) or the unsubstituted `ng serve`
    // placeholder — the backend skips Turnstile verification in both cases.
    if (!siteKey || siteKey.startsWith('${')) {
      return DEV_TURNSTILE_TOKEN;
    }

    await this.loadTurnstileScript();

    return new Promise<string>((resolve, reject) => {
      const container = document.createElement('div');
      container.style.display = 'none';
      document.body.appendChild(container);

      let widgetId: string | undefined;
      const cleanup = () => {
        if (widgetId !== undefined) {
          window.turnstile?.remove(widgetId);
        }
        container.remove();
      };

      widgetId = window.turnstile!.render(container, {
        sitekey: siteKey,
        size: 'invisible',
        callback: (token) => {
          cleanup();
          resolve(token);
        },
        'error-callback': (error) => {
          cleanup();
          reject(error);
        },
      });
    });
  }

  private loadTurnstileScript(): Promise<void> {
    if (window.turnstile) {
      return Promise.resolve();
    }

    if (!this.turnstileScriptPromise) {
      this.turnstileScriptPromise = new Promise<void>((resolve, reject) => {
        const script = document.createElement('script');
        script.src = TURNSTILE_SCRIPT_URL;
        script.async = true;
        script.defer = true;
        script.onload = () => resolve();
        script.onerror = () => reject(new Error('Failed to load Turnstile script'));
        document.head.appendChild(script);
      });
    }

    return this.turnstileScriptPromise;
  }
}
