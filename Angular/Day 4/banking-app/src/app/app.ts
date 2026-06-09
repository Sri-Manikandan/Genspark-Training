import { Component, signal } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { usernameSubject, getUsernameFromToken } from './rxjs/auth.operator';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  username = signal(getUsernameFromToken() ?? 'Guest');

  constructor() {
    usernameSubject.subscribe({
      next:(un)=>{
        this.username.set(un);
      }
    })
  }

  onDestroy(){
    usernameSubject.unsubscribe();
  }

  protected readonly title = signal('banking-app');
}
