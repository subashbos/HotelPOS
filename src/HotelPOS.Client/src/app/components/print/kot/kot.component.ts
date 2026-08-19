import { Component, Input } from '@angular/core';
import { KotData } from '../../../models/print.model';

@Component({
  standalone: false,
  selector: 'app-kot',
  templateUrl: './kot.component.html'
})
export class KotComponent {
  @Input() data!: KotData;
  @Input() isThermal = true;

  get separator(): string {
    return '-'.repeat(this.isThermal ? 30 : 60);
  }

  get tableDisplay(): string {
    return this.data.tableNumber === 0 ? 'Takeaway / Online' : String(this.data.tableNumber);
  }

  get now(): Date {
    return new Date();
  }
}
