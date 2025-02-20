import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DocSearchComponent } from './doc-search.component';

describe('DocSearchComponent', () => {
  let component: DocSearchComponent;
  let fixture: ComponentFixture<DocSearchComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DocSearchComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DocSearchComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
