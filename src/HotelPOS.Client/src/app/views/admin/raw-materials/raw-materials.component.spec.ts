import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { RawMaterialsComponent } from './raw-materials.component';
import { RawMaterialService } from '../../../services/raw-material.service';
import { RawMaterial } from '../../../models/raw-material.model';

describe('RawMaterialsComponent', () => {
  let component: RawMaterialsComponent;
  let fixture: ComponentFixture<RawMaterialsComponent>;
  let rawMaterialServiceSpy: jasmine.SpyObj<RawMaterialService>;

  const mockMaterial: RawMaterial = { id: 1, name: 'Basmati Rice', unit: 'kg', costPerUnit: 110, currentStock: 50, minStockThreshold: 10 };

  beforeEach(async () => {
    rawMaterialServiceSpy = jasmine.createSpyObj('RawMaterialService', [
      'getRawMaterials', 'getMockRawMaterials', 'createRawMaterial', 'updateRawMaterial', 'deleteRawMaterial'
    ]);
    rawMaterialServiceSpy.getRawMaterials.and.returnValue(of([mockMaterial]));

    await TestBed.configureTestingModule({
      declarations: [RawMaterialsComponent],
      imports: [FormsModule],
      providers: [{ provide: RawMaterialService, useValue: rawMaterialServiceSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(RawMaterialsComponent);
    component = fixture.componentInstance;
  });

  it('should create and load raw materials', () => {
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(component.rawMaterials.length).toBe(1);
    expect(component.filteredMaterials.length).toBe(1);
  });

  it('should fall back to mock data on load error', () => {
    rawMaterialServiceSpy.getRawMaterials.and.returnValue(throwError(() => new Error('boom')));
    rawMaterialServiceSpy.getMockRawMaterials.and.returnValue(of([mockMaterial]));

    fixture.detectChanges();

    expect(component.rawMaterials).toEqual([mockMaterial]);
  });

  it('should filter by name or unit', () => {
    fixture.detectChanges();

    component.searchQuery = 'rice';
    component.onSearchChanged();
    expect(component.filteredMaterials.length).toBe(1);

    component.searchQuery = 'nonexistent';
    component.onSearchChanged();
    expect(component.filteredMaterials.length).toBe(0);
  });

  it('should open the add form with a reset default', () => {
    fixture.detectChanges();

    component.openAddForm();

    expect(component.isEditMode).toBeFalse();
    expect(component.showForm).toBeTrue();
    expect(component.form.name).toBe('');
  });

  it('should open the edit form pre-filled with the material', () => {
    fixture.detectChanges();

    component.openEditForm(mockMaterial);

    expect(component.isEditMode).toBeTrue();
    expect(component.editingId).toBe(1);
    expect(component.form.name).toBe('Basmati Rice');
  });

  it('should close the form on cancel', () => {
    fixture.detectChanges();
    component.showForm = true;

    component.cancelForm();

    expect(component.showForm).toBeFalse();
  });

  it('should require a name before saving', () => {
    fixture.detectChanges();
    component.form.name = '   ';

    component.saveRawMaterial();

    expect(component.formError).toBe('Material name is required.');
    expect(rawMaterialServiceSpy.createRawMaterial).not.toHaveBeenCalled();
  });

  it('should create a new material', () => {
    fixture.detectChanges();
    component.openAddForm();
    component.form.name = 'Ghee';
    rawMaterialServiceSpy.createRawMaterial.and.returnValue(of({ ...mockMaterial, id: 2, name: 'Ghee' }));

    component.saveRawMaterial();

    expect(rawMaterialServiceSpy.createRawMaterial).toHaveBeenCalledWith(component.form);
    expect(component.showForm).toBeFalse();
  });

  it('should add a locally-generated material when create fails', () => {
    fixture.detectChanges();
    component.openAddForm();
    component.form.name = 'Ghee';
    rawMaterialServiceSpy.createRawMaterial.and.returnValue(throwError(() => new Error('boom')));

    component.saveRawMaterial();

    expect(component.rawMaterials.some(m => m.name === 'Ghee')).toBeTrue();
  });

  it('should update an existing material', () => {
    fixture.detectChanges();
    component.openEditForm(mockMaterial);
    rawMaterialServiceSpy.updateRawMaterial.and.returnValue(of(mockMaterial));

    component.saveRawMaterial();

    expect(rawMaterialServiceSpy.updateRawMaterial).toHaveBeenCalledWith(1, component.form);
  });

  it('should apply the edit locally when update fails', () => {
    fixture.detectChanges();
    component.openEditForm(mockMaterial);
    component.form.currentStock = 99;
    rawMaterialServiceSpy.updateRawMaterial.and.returnValue(throwError(() => new Error('boom')));

    component.saveRawMaterial();

    expect(component.rawMaterials[0].currentStock).toBe(99);
  });

  it('should delete a material when confirmed', () => {
    fixture.detectChanges();
    spyOn(window, 'confirm').and.returnValue(true);
    rawMaterialServiceSpy.deleteRawMaterial.and.returnValue(of(void 0));

    component.deleteMaterial(mockMaterial);

    expect(rawMaterialServiceSpy.deleteRawMaterial).toHaveBeenCalledWith(1);
  });

  it('should not delete a material when not confirmed', () => {
    fixture.detectChanges();
    spyOn(window, 'confirm').and.returnValue(false);

    component.deleteMaterial(mockMaterial);

    expect(rawMaterialServiceSpy.deleteRawMaterial).not.toHaveBeenCalled();
  });

  it('should remove the material locally when delete fails', () => {
    fixture.detectChanges();
    spyOn(window, 'confirm').and.returnValue(true);
    rawMaterialServiceSpy.deleteRawMaterial.and.returnValue(throwError(() => new Error('boom')));

    component.deleteMaterial(mockMaterial);

    expect(component.rawMaterials.length).toBe(0);
  });
});
