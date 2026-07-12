import { Component, AfterViewInit } from "@angular/core";
import { Chart, registerables } from "chart.js";
Chart.register(...registerables);

@Component({
  standalone: false,
  selector: "app-card-line-chart",
  templateUrl: "./card-line-chart.component.html",
})
export class CardLineChartComponent implements AfterViewInit {
  constructor() {}

  ngAfterViewInit() {
    const config: any = {
      type: "line",
      data: {
        labels: [
          "January",
          "February",
          "March",
          "April",
          "May",
          "June",
          "July",
        ],
        datasets: [
          {
            label: new Date().getFullYear(),
            backgroundColor: "#4c51bf",
            borderColor: "#4c51bf",
            data: [65, 78, 66, 44, 56, 67, 75],
            fill: false,
          },
          {
            label: new Date().getFullYear() - 1,
            fill: false,
            backgroundColor: "#fff",
            borderColor: "#fff",
            data: [40, 68, 86, 74, 56, 60, 87],
          },
        ],
      },
      options: {
        maintainAspectRatio: false,
        responsive: true,
        plugins: {
          title: {
            display: false,
            text: "Sales Charts",
            color: "white",
          },
          legend: {
            labels: {
              color: "white",
            },
            align: "end",
            position: "bottom",
          },
        },
        scales: {
          x: {
            ticks: {
              color: "rgba(255,255,255,.7)",
            },
            display: true,
            grid: {
              display: false,
              borderDash: [2],
              color: "rgba(33, 37, 41, 0.3)",
            },
          },
          y: {
            ticks: {
              color: "rgba(255,255,255,.7)",
            },
            display: true,
            grid: {
              borderDash: [3],
              drawBorder: false,
              color: "rgba(255, 255, 255, 0.15)",
            },
          },
        },
      },
    };
    let ctx: any = document.getElementById("line-chart") as HTMLCanvasElement;
    if (ctx) {
      ctx = ctx.getContext("2d");
      new Chart(ctx, config);
    }
  }
}
