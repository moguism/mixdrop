import { Component, ElementRef, Input, SimpleChanges, ViewChild } from '@angular/core';
import { DragDropModule } from '@angular/cdk/drag-drop';


@Component({
  standalone: true,
  selector: 'app-roulette',
  templateUrl: './roulette.component.html',
  imports: [DragDropModule],
  styleUrls: ['./roulette.component.css']
})
export class RouletteComponent {

  @ViewChild('rouletteElement') rouletteElement!: ElementRef;
  @ViewChild('wheel', { static: false }) wheel!: ElementRef<HTMLDivElement>;
  @ViewChild('spinButton', { static: false }) spinButton!: ElementRef<HTMLButtonElement>;
  @Input() levelToDelete: number = 1;

  private currentRotation = 0;

  spinRoulette(level : number) {
    var rotateLevel = 1;
    switch (level) {
      case 3:
        rotateLevel = Math.random() * (1 - 0.5) + 0.5;
        break;
      case 2: // listo
        rotateLevel = Math.random() * (0.5 - 0.25) + 0.25;
        break;
      case 1: // listo
        rotateLevel =  Math.random() * (0.25 - 0.1) + 0.1;
        break;
      case 0:
        rotateLevel = Math.random() * (0.1 - 0);
        break;
    }


    const extraRotations = 720; // Mínimo de giros completos
    const randomAngle = Math.floor(rotateLevel * 360); // Ángulo aleatorio de detención
    const finalAngle = this.currentRotation + extraRotations + randomAngle;

    // Cambiar la propiedad de transform para girar la rueda
    this.wheel.nativeElement.style.transition = "transform 2s ease-out";
    this.wheel.nativeElement.style.transform = `rotate(${finalAngle}deg)`;

    this.currentRotation = finalAngle;
  }

} 
