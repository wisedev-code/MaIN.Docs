import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DocsService } from '../../services/docs.service';
import { DocSection } from '../../models/docs.models';
import { marked } from 'marked';

@Component({
  selector: 'app-docs',
  imports: [],
  templateUrl: './docs.html',
  styleUrl: './docs.scss'
})
export class Docs implements OnInit {
  sections = signal<DocSection[]>([]);
  content = signal<string>('');
  currentSlug = signal<string>('');
  searchQuery = signal<string>('');
  loading = signal(true);

  constructor(
    private docsService: DocsService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  async ngOnInit() {
    try {
      const manifest = await this.docsService.getManifest();
      this.sections.set(manifest.sections);
    } catch {
      this.sections.set(FALLBACK_MANIFEST.sections);
    }

    this.route.paramMap.subscribe(async params => {
      const slug = params.get('slug') ?? this.sections()[0]?.entries[0]?.slug ?? 'getting-started';
      this.currentSlug.set(slug);
      await this.loadDoc(slug);
    });
  }

  async loadDoc(slug: string) {
    this.loading.set(true);
    this.content.set('');
    try {
      const raw = await this.docsService.getDoc(slug);
      this.content.set(await marked(raw));
    } catch {
      this.content.set(`<p style="color:var(--text-muted)">Doc not found: <code>${slug}</code></p>`);
    }
    this.loading.set(false);
  }

  navigate(slug: string) {
    this.router.navigate(['/docs', slug]);
  }

  filteredSections() {
    const q = this.searchQuery().toLowerCase();
    if (!q) return this.sections();
    return this.sections().map(s => ({
      ...s,
      entries: s.entries.filter(e => e.title.toLowerCase().includes(q) || e.slug.includes(q))
    })).filter(s => s.entries.length > 0);
  }

  onSearchInput(event: Event) {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }
}

const FALLBACK_MANIFEST = {
  sections: [
    {
      title: 'Getting Started',
      entries: [
        { slug: 'getting-started', title: 'Introduction' },
        { slug: 'installation',    title: 'Installation' },
        { slug: 'quickstart',      title: 'Quick Start' },
      ]
    },
    {
      title: 'Core Concepts',
      entries: [
        { slug: 'backends',  title: 'Backends' },
        { slug: 'chat',      title: 'Chat' },
        { slug: 'agents',    title: 'Agents' },
        { slug: 'flows',     title: 'Flows' },
      ]
    },
    {
      title: 'Deployment',
      entries: [
        { slug: 'docker',    title: 'Docker' },
        { slug: 'inferpage', title: 'InferPage' },
      ]
    }
  ]
};
