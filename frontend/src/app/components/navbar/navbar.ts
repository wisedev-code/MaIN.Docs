import { Component, OnInit, computed, signal, HostListener, ElementRef } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { DocsService } from '../../services/docs.service';
import { AppStateService } from '../../services/app-state.service';

interface SearchEntry {
  slug: string;
  title: string;
  section: string;
  heading?: string;
  anchor?: string;
}

// Must match toAnchor() in docs.ts — lowercase, keep only alnum/space/hyphen.
// Input is already plain text (backticks stripped from markdown, no HTML tags).
function toAnchor(text: string): string {
  return text
    .toLowerCase()
    .replace(/[^a-z0-9\s-]/g, '')
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-')
    .replace(/^-|-$/g, '');
}

@Component({
  selector: 'app-navbar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss'
})
export class Navbar implements OnInit {
  isDark = signal(true);
  query = signal('');
  isOpen = signal(false);
  private allEntries = signal<SearchEntry[]>([]);

  results = computed(() => {
    const q = this.query().toLowerCase().trim();
    if (!q) return [];
    return this.allEntries()
      .filter(e =>
        e.title.toLowerCase().includes(q) ||
        e.slug.includes(q) ||
        (e.heading && e.heading.toLowerCase().includes(q))
      )
      .slice(0, 8);
  });

  constructor(
    private docsService: DocsService,
    private router: Router,
    private el: ElementRef,
    private appStateService: AppStateService
  ) {}

  onBrandClick() {
    this.appStateService.requestChatReset();
    this.router.navigate(['/']);
  }

  async ngOnInit() {
    const saved = localStorage.getItem('theme');
    const dark = saved !== 'light';
    this.isDark.set(dark);
    document.documentElement.setAttribute('data-theme', dark ? 'dark' : 'light');

    try {
      const manifest = await this.docsService.getManifest();
      const pages: SearchEntry[] = manifest.sections.flatMap(s =>
        s.entries.map(e => ({ slug: e.slug, title: e.title, section: s.title }))
      );
      this.allEntries.set(pages);

      for (const page of pages) {
        this.docsService.getDoc(page.slug).then(md => {
          const headings = this.extractHeadings(md, page.slug, page.title);
          this.allEntries.update(prev => [...prev, ...headings]);
        }).catch(() => {});
      }
    } catch {}
  }

  private extractHeadings(markdown: string, slug: string, pageTitle: string): SearchEntry[] {
    const entries: SearchEntry[] = [];
    let inFence = false;
    for (const line of markdown.split('\n')) {
      if (line.startsWith('```')) { inFence = !inFence; continue; }
      if (inFence) continue;
      const m = line.match(/^#{2,3}\s+(.+)$/);
      if (m) {
        const raw = m[1];
        const heading = raw.replace(/`/g, '').trim();
        const anchor = toAnchor(raw.replace(/`/g, ''));
        entries.push({ slug, title: heading, section: pageTitle, heading, anchor });
      }
    }
    return entries;
  }

  toggleTheme() {
    const next = !this.isDark();
    this.isDark.set(next);
    localStorage.setItem('theme', next ? 'dark' : 'light');
    document.documentElement.setAttribute('data-theme', next ? 'dark' : 'light');
  }

  onQueryInput(event: Event) {
    const val = (event.target as HTMLInputElement).value;
    this.query.set(val);
    this.isOpen.set(val.trim().length > 0);
  }

  onKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape') this.closeSearch();
  }

  selectResult(entry: SearchEntry) {
    if (entry.anchor) {
      this.router.navigate(['/docs', entry.slug], { fragment: entry.anchor });
    } else {
      this.router.navigate(['/docs', entry.slug]);
    }
    this.closeSearch();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (!this.el.nativeElement.contains(event.target as Node)) {
      this.isOpen.set(false);
    }
  }

  private closeSearch() {
    this.query.set('');
    this.isOpen.set(false);
  }
}
