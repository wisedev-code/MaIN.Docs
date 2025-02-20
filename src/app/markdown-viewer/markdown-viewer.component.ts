import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MarkdownModule } from 'ngx-markdown';

@Component({
  selector: 'app-markdown-viewer',
  imports: [CommonModule, MarkdownModule],
  template: `<markdown [src]="docUrl"></markdown>`,
  styleUrls: ['./markdown-viewer.component.css']
})
export class MarkdownViewerComponent implements OnInit {
  docUrl: string = '';

  constructor(private route: ActivatedRoute) {}

  ngOnInit(): void {
 this.route.paramMap.subscribe(params => {
    const docPath = params.get('docPath');
    if (docPath) {
      this.docUrl = `assets/docs/${docPath}.md`;
    }
  });
  }
}
