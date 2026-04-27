import {
  ApplicationConfig,
  provideAppInitializer,
  inject,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideNativeDateAdapter } from '@angular/material/core';
import { firstValueFrom } from 'rxjs';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { AuthTokenStore } from './core/auth/auth-token.store';
import { AuthStore } from './core/auth/auth.store';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideAnimations(),
    provideNativeDateAdapter(),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAppInitializer(async () => {
      const tokenStore = inject(AuthTokenStore);
      const auth = inject(AuthStore);
      if (tokenStore.token()) {
        try {
          await firstValueFrom(auth.loadMe());
        } catch {
          tokenStore.clear();
        }
      }
    })
  ]
};
