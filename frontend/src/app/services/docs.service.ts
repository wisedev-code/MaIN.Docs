import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { DocsManifest } from '../models/docs.models';

@Injectable({ providedIn: 'root' })
export class DocsService {
  private manifestCache: Promise<DocsManifest> | null = null;
  private docCache = new Map<string, Promise<string>>();

  constructor(private http: HttpClient) {}

  async getManifest(): Promise<DocsManifest> {
    if (!this.manifestCache) {
      this.manifestCache = firstValueFrom(this.http.get<DocsManifest>('/docs/manifest.json'));
    }
    return this.manifestCache;
  }

  async getDoc(slug: string): Promise<string> {
    if (!this.docCache.has(slug)) {
      this.docCache.set(slug, firstValueFrom(this.http.get(`/docs/${slug}.md`, { responseType: 'text' })));
    }
    return this.docCache.get(slug)!;
  }
}
