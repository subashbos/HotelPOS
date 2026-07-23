import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { TestBed, ComponentFixture } from '@angular/core/testing';
import { CardTableComponent } from './card-table.component';

describe('CardTableComponent', () => {
  let component: CardTableComponent;
  let fixture: ComponentFixture<CardTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [CardTableComponent],
      schemas: [CUSTOM_ELEMENTS_SCHEMA]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(CardTableComponent);
    component = fixture.componentInstance;
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });
});
