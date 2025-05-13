import { Component, HostListener, OnDestroy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './services/auth.service';
import { WebsocketService } from './services/websocket.service';
import { CommonModule } from '@angular/common';


@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnDestroy {
  title = 'mixdrop-front';

  constructor(private authService : AuthService, private websocketService: WebsocketService){}

  // Cada vez que el usuario recarga o cierra la página, previamente lo desconecta (no existe un evento para el cierre únicamente)
  @HostListener('window:beforeunload', ['$event']) 
  unloadNotification(): void {
    this.authService.disconnectUser()
  }

  ngOnDestroy(): void {
      this.websocketService.disconnectNative()
  }
}
