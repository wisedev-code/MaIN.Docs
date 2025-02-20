import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

interface DocNode {
  title: string;
  path?: string;
  children?: DocNode[];
  expanded?: boolean; // We'll toggle this
}

@Component({
  imports: [RouterModule, CommonModule],
  selector: 'app-doc-menu',
  templateUrl: './doc-menu.component.html',
  styleUrls: ['./doc-menu.component.css']
})
export class DocMenuComponent implements OnInit {
  docsTree: DocNode[] = [];

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    // Load the docs structure from JSON
    this.http.get<DocNode[]>('assets/docs/docs-index.json').subscribe(data => {
      this.docsTree = data;
    });
  }

  toggleNode(node: DocNode): void {
    node.expanded = !node.expanded;
  }
}
