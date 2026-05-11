import { Component, OnInit } from "@angular/core";
import { IndexNavbarComponent } from "../../components/navbars/index-navbar/index-navbar.component";
import { FooterComponent } from "../../components/footers/footer/footer.component";

@Component({ standalone: false,   
  selector: "app-index",
  
  
  templateUrl: "./index.component.html",
})
export class IndexComponent implements OnInit {
  constructor() {}

  ngOnInit(): void {}
}
