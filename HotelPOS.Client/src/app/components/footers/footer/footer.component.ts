import { Component, OnInit } from "@angular/core";

@Component({ standalone: false,   
  selector: "app-footer",
  
  templateUrl: "./footer.component.html",
})
export class FooterComponent implements OnInit {
  date = new Date().getFullYear();
  constructor() {}

  ngOnInit(): void {}
}
