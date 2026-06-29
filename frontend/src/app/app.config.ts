import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { AuthService } from './core/auth/auth.service';
import { RestoApiService } from './core/api/resto-api.service';
import { SignalRService } from './core/signalr/signalr.service';
import { MockAuthService } from './core/demo/mock-auth.service';
import { MockRestoApiService } from './core/demo/mock-resto-api.service';
import { MockSignalRService } from './core/demo/mock-signalr.service';
import { environment } from '../environments/environment';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideRouter(routes),
    ...(environment.demoMode
      ? [
          { provide: AuthService, useClass: MockAuthService },
          { provide: RestoApiService, useClass: MockRestoApiService },
          { provide: SignalRService, useClass: MockSignalRService },
        ]
      : []),
  ],
};
