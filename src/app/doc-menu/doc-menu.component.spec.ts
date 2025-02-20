import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DocMenuComponent } from './doc-menu.component';

describe('DocMenuComponent', () => {
  let component: DocMenuComponent;
  let fixture: ComponentFixture<DocMenuComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DocMenuComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DocMenuComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
