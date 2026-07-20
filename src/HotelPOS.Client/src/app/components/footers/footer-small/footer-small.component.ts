import { Component, Input } from "@angular/core";

@Component({
  standalone: false,
  selector: "app-footer-small",
  templateUrl: "./footer-small.component.html",
})
export class FooterSmallComponent {
  date = new Date().getFullYear();

  @Input()
  get absolute(): boolean {
    return this._absolute;
  }
  set absolute(absolute: boolean) {
    this._absolute = absolute ?? false;
  }
  private _absolute = false;

  constructor() {}
}
