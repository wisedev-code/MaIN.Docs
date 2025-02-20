import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router, RouterModule } from '@angular/router';
import { forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';
import Fuse from 'fuse.js';
import { CommonModule } from '@angular/common';

interface DocNode {
  title: string;
  path?: string;        // optional if it's a folder
  children?: DocNode[]; // nested docs
}

interface DocItem {
  title: string;
  path: string;   // e.g. "examples/example-chat-basic"
  content: string; // we'll fetch .md text
}

@Component({
  imports: [RouterModule, CommonModule],
  selector: 'app-doc-search',
  templateUrl: './doc-search.component.html',
  styleUrls: ['./doc-search.component.css']
})
export class DocSearchComponent implements OnInit {
  query = '';
  results: DocItem[] = [];
  fuse!: Fuse<DocItem>;

  constructor(private http: HttpClient, private router: Router) {}

  ngOnInit(): void {
    // 1) Load the docs structure
    this.http.get<DocNode[]>('assets/docs/docs-index.json')
      .subscribe(structure => {
        // 2) Flatten the nested structure to get a list of doc items with "path"
        const docNodesWithPath = this.flattenDocs(structure);

        // 3) For each doc node, fetch the .md file
        const requests = docNodesWithPath.map(node => {
          return this.http
            .get(`assets/docs/${node.path}.md`, { responseType: 'text' })
            .pipe(
              map(mdContent => {
                // Build a DocItem with content
                return {
                  title: node.title,
                  path: node.path,
                  content: mdContent
                } as DocItem;
              })
            );
        });

        // 4) Wait for all .md fetches to complete
        forkJoin(requests).subscribe((docItems: DocItem[]) => {
          // 5) Build the Fuse.js index
          this.fuse = new Fuse(docItems, {
            keys: [
              { name: 'title', weight: 0.7 },
              { name: 'content', weight: 0.3 }
            ],
            threshold: 0.2,
            minMatchCharLength: 2,
            ignoreLocation: true
          });
        });
      });
  }

  // Recursively traverse the doc structure, collecting nodes that have a "path"
  flattenDocs(nodes: DocNode[]): DocNode[] {
    let result: DocNode[] = [];
    for (const node of nodes) {
      // if this node has a path, it's a doc
      if (node.path) {
        result.push(node);
      }
      // if it has children, recurse
      if (node.children) {
        result = result.concat(this.flattenDocs(node.children));
      }
    }
    return result;
  }

  onSearch(event: Event): void {
    const inputEl = event.target as HTMLInputElement;
    this.query = inputEl.value.trim();
    if (!this.query || !this.fuse) {
      this.results = [];
      return;
    }

    // 6) Search across all docItems
    const fuseResults = this.fuse.search(this.query);
    this.results = fuseResults.map(r => r.item);
  }

  navigateToDoc(path: string): void {
    // Clear results, optionally clear the query
    this.results = [];
    this.query = '';
    // Navigate to the doc route
    this.router.navigate(['/doc', path]);
  }
}
