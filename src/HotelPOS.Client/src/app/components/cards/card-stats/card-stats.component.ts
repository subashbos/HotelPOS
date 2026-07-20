import { Component, Input } from "@angular/core";

@Component({
  standalone: false,
  selector: "app-card-stats",
  templateUrl: "./card-stats.component.html",
})
export class CardStatsComponent {
  @Input()
  get statSubtitle(): string {
    return this._statSubtitle;
  }
  set statSubtitle(statSubtitle: string) {
    this._statSubtitle = statSubtitle ?? "Traffic";
  }
  private _statSubtitle = "Traffic";

  @Input()
  get statTitle(): string {
    return this._statTitle;
  }
  set statTitle(statTitle: string) {
    this._statTitle = statTitle ?? "350,897";
  }
  private _statTitle = "350,897";

  // The value must match one of up or down
  @Input()
  get statArrow(): string {
    return this._statArrow;
  }
  set statArrow(statArrow: string) {
    this._statArrow =
      statArrow !== "down" && statArrow !== "up" ? "up" : statArrow;
  }
  private _statArrow = "up";

  @Input()
  get statPercent(): string {
    return this._statPercent;
  }
  set statPercent(statPercent: string) {
    this._statPercent = statPercent ?? "3.48";
  }
  private _statPercent = "3.48";

  // can be any of the text color utilities
  // from tailwindcss
  @Input()
  get statPercentColor(): string {
    return this._statPercentColor;
  }
  set statPercentColor(statPercentColor: string) {
    this._statPercentColor =
      statPercentColor ?? "text-emerald-500";
  }
  private _statPercentColor = "text-emerald-500";

  @Input()
  get statDescripiron(): string {
    return this._statDescripiron;
  }
  set statDescripiron(statDescripiron: string) {
    this._statDescripiron =
      statDescripiron ?? "Since last month";
  }
  private _statDescripiron = "Since last month";

  @Input()
  get statIconName(): string {
    return this._statIconName;
  }
  set statIconName(statIconName: string) {
    this._statIconName =
      statIconName ?? "far fa-chart-bar";
  }
  private _statIconName = "far fa-chart-bar";

  // can be any of the background color utilities
  // from tailwindcss
  @Input()
  get statIconColor(): string {
    return this._statIconColor;
  }
  set statIconColor(statIconColor: string) {
    this._statIconColor =
      statIconColor ?? "bg-red-500";
  }
  private _statIconColor = "bg-red-500";

  constructor() {}
}
