import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class AppStateService {
  readonly chatReset = signal(0);
  requestChatReset() { this.chatReset.update(v => v + 1); }
}
