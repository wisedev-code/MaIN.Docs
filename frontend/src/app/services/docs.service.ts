import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { DocsManifest } from '../models/docs.models';

@Injectable({ providedIn: 'root' })
export class DocsService {
  constructor(private http: HttpClient) {}

  async getManifest(): Promise<DocsManifest> {
    return firstValueFrom(this.http.get<DocsManifest>('/docs/manifest.json'));
  }

  async getDoc(slug: string): Promise<string> {
    return firstValueFrom(this.http.get(`/docs/${slug}.md`, { responseType: 'text' }));
  }
}
