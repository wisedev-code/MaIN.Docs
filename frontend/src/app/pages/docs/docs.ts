import { Component, OnInit, computed, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DocsService } from '../../services/docs.service';
import { DocSection } from '../../models/docs.models';
import { marked } from 'marked';

const NAVBAR_HEIGHT = 68;

interface HeadingEntry {
  slug: string;
  pageTitle: string;
  heading: string;
  anchor: string;
}

// Plain-text anchor: no HTML tags (we use textContent / backtick-stripped markdown text).
// Must stay in sync with the same function in navbar.ts.
function toAnchor(text: string): string {
  return text
    .toLowerCase()
    .replace(/[^a-z0-9\s-]/g, '')
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-')
    .replace(/^-|-$/g, '');
}

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

  private headingIndex = signal<HeadingEntry[]>([]);

  searchResults = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    if (!q) return [];
    return this.headingIndex()
      .filter(h =>
        h.heading.toLowerCase().includes(q) ||
        h.pageTitle.toLowerCase().includes(q)
      )
      .slice(0, 12);
  });

  constructor(
    private docsService: DocsService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  async ngOnInit() {
    try {
      const manifest = await this.docsService.getManifest();
      this.sections.set(manifest.sections);

      const allPages = manifest.sections.flatMap(s =>
        s.entries.map(e => ({ slug: e.slug, title: e.title }))
      );
      for (const page of allPages) {
        this.docsService.getDoc(page.slug).then(md => {
          const entries = this.extractHeadings(md, page.slug, page.title);
          this.headingIndex.update(prev => [...prev, ...entries]);
        }).catch(() => {});
      }
    } catch {
      this.sections.set(FALLBACK_MANIFEST.sections);
    }

    this.route.paramMap.subscribe(async params => {
      const slug = params.get('slug') ?? this.sections()[0]?.entries[0]?.slug ?? 'getting-started';
      const fragment = this.route.snapshot.fragment;
      this.currentSlug.set(slug);
      await this.loadDoc(slug, fragment);
    });

    this.route.fragment.subscribe(fragment => {
      if (fragment && !this.loading()) {
        this.injectHeadingIds();
        this.scrollToFragment(fragment);
      }
    });
  }

  async loadDoc(slug: string, fragment: string | null = null) {
    this.loading.set(true);
    this.content.set('');
    try {
      const raw = await this.docsService.getDoc(slug);
      this.content.set(await marked(raw) as string);

      // Defer until after Angular flushes [innerHTML] to the DOM.
      // requestAnimationFrame fires after the next paint — the DOM is guaranteed ready.
      requestAnimationFrame(() => {
        this.injectHeadingIds();
        if (fragment) this.scrollToFragment(fragment);
      });
    } catch {
      this.content.set(`<p style="color:var(--text-muted)">Doc not found: <code>${slug}</code></p>`);
    }
    this.loading.set(false);
  }

  // Adds id= attributes to every h2/h3 inside .markdown-body using their
  // textContent so we don't depend on marked's exact HTML output format.
  private injectHeadingIds() {
    const article = document.querySelector('.markdown-body');
    if (!article) return;
    article.querySelectorAll('h2, h3').forEach(el => {
      if (!el.id) {
        el.id = toAnchor(el.textContent ?? '');
      }
    });
  }

  private scrollToFragment(id: string) {
    const el = document.getElementById(id);
    if (el) {
      const top = el.getBoundingClientRect().top + window.scrollY - NAVBAR_HEIGHT;
      window.scrollTo({ top, behavior: 'smooth' });
    }
  }

  private extractHeadings(markdown: string, slug: string, pageTitle: string): HeadingEntry[] {
    const entries: HeadingEntry[] = [];
    let inFence = false;
    for (const line of markdown.split('\n')) {
      if (line.startsWith('```')) { inFence = !inFence; continue; }
      if (inFence) continue;
      const m = line.match(/^#{2,3}\s+(.+)$/);
      if (m) {
        const raw = m[1];
        const heading = raw.replace(/`/g, '').trim();
        // Strip backticks so the anchor matches el.textContent (which has no backticks)
        const anchor = toAnchor(heading);
        entries.push({ slug, pageTitle, heading, anchor });
      }
    }
    return entries;
  }

  selectHeading(entry: HeadingEntry) {
    this.searchQuery.set('');
    if (entry.slug === this.currentSlug()) {
      this.injectHeadingIds();
      this.scrollToFragment(entry.anchor);
    } else {
      this.router.navigate(['/docs', entry.slug], { fragment: entry.anchor });
    }
  }

  navigate(slug: string) {
    this.searchQuery.set('');
    this.router.navigate(['/docs', slug]);
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
        { slug: 'docker', title: 'Docker' },
      ]
    }
  ]
};
