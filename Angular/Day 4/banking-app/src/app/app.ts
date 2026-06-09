import { Component, signal } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { usernameSubject, getUsernameFromToken, changeUsername } from './rxjs/auth.operator';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  username = signal(getUsernameFromToken() ?? 'Guest');

  constructor(private router: Router) {
    usernameSubject.subscribe({
      next:(un)=>{
        this.username.set(un);
      }
    })
  }

  isLoggedIn():boolean{
    return !!sessionStorage.getItem('token');
  }

  logout() {
    sessionStorage.removeItem('token');
    changeUsername('Guest');
    this.router.navigate(['/login']);
  }

  onDestroy(){
    usernameSubject.unsubscribe();
  }

  protected readonly title = signal('banking-app');
}
