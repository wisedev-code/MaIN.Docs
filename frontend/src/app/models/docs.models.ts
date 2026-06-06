export interface DocEntry {
  slug: string;
  title: string;
  section?: string;
}

export interface DocsManifest {
  sections: DocSection[];
}

export interface DocSection {
  title: string;
  entries: DocEntry[];
}
