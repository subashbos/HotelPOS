import { Component, AfterViewInit } from "@angular/core";
import { Chart, registerables } from "chart.js";
Chart.register(...registerables);

@Component({
  standalone: false,
  selector: "app-card-bar-chart",
  templateUrl: "./card-bar-chart.component.html",
})
export class CardBarChartComponent implements AfterViewInit {
  constructor() {}

  ngAfterViewInit() {
    const config: any = {
      type: "bar",
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
            backgroundColor: "#ed64a6",
            borderColor: "#ed64a6",
            data: [30, 78, 56, 34, 100, 45, 13],
            fill: false,
            barThickness: 8,
          },
          {
            label: new Date().getFullYear() - 1,
            fill: false,
            backgroundColor: "#4c51bf",
            borderColor: "#4c51bf",
            data: [27, 68, 86, 74, 10, 4, 87],
            barThickness: 8,
          },
        ],
      },
      options: {
        maintainAspectRatio: false,
        responsive: true,
        plugins: {
          legend: {
            labels: {
              color: "rgba(0,0,0,.4)",
            },
            align: "end",
            position: "bottom",
          },
          title: {
            display: false,
            text: "Orders Chart",
          },
        },
        scales: {
          x: {
            display: true,
            grid: {
              borderDash: [2],
              borderDashOffset: [2],
              color: "rgba(33, 37, 41, 0.3)",
            },
          },
          y: {
            display: true,
            grid: {
              borderDash: [2],
              drawBorder: false,
              color: "rgba(33, 37, 41, 0.2)",
            },
          },
        },
      },
    };
    let ctx: any = document.getElementById("bar-chart");
    if (ctx) {
      ctx = ctx.getContext("2d");
      new Chart(ctx, config);
    }
  }
}
