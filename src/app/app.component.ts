import { Component } from '@angular/core';
import { RouterModule, RouterOutlet } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu'; 
import { DocSearchComponent } from './doc-search/doc-search.component';
import { MatToolbarModule } from '@angular/material/toolbar';  
import {MatSidenavModule} from '@angular/material/sidenav'; 
import { DocMenuComponent } from './doc-menu/doc-menu.component';
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    DocMenuComponent,
     DocSearchComponent,
      MatIconModule,
       MatMenuModule,
        MatToolbarModule,
         RouterModule,
        MatSidenavModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'main-docs';

  onSearch(event: Event): void {
    const inputElement = event.target as HTMLInputElement;
    const searchText = inputElement.value;
    console.log('Search query:', searchText);
    // You can implement actual search functionality here
  }

}
