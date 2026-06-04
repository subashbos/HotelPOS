import { Component, OnInit, ViewChild, ElementRef } from "@angular/core";
import { createPopper } from "@popperjs/core";
import { RouterModule } from "@angular/router";

@Component({
  standalone: false,    
  selector: "app-pages-dropdown",
  
  
  templateUrl: "./pages-dropdown.component.html",
})
export class PagesDropdownComponent implements OnInit {
  dropdownPopoverShow = false;
  @ViewChild("btnDropdownRef", { static: false }) btnDropdownRef!: ElementRef;
  @ViewChild("popoverDropdownRef", { static: false })
  popoverDropdownRef!: ElementRef;
  ngOnInit() {}
  toggleDropdown(event: any) {
    event.preventDefault();
    if (this.dropdownPopoverShow) {
      this.dropdownPopoverShow = false;
    } else {
      this.dropdownPopoverShow = true;
      this.createPopper();
    }
  }
  createPopper() {
    createPopper(
      this.btnDropdownRef.nativeElement,
      this.popoverDropdownRef.nativeElement,
      {
        placement: "bottom-start",
      }
    );
  }
}
