import { Component, OnInit } from '@angular/core';
import { EssService } from '../../../services/ess.service';
import { Employee } from '../../../models/employee.model';

@Component({
  standalone: false,
  selector: 'app-ess-profile',
  templateUrl: './ess-profile.component.html',
})
export class EssProfileComponent implements OnInit {
  profile: Employee | null = null;
  isLoading = false;
  loadError = '';
  actionError = '';
  successMessage = '';
  isSaving = false;

  formPhone = '';
  formAddress = '';
  formEmergencyContactName = '';
  formEmergencyContactPhone = '';

  constructor(private readonly essService: EssService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.loadError = '';
    this.essService.getProfile().subscribe({
      next: (profile) => {
        this.profile = profile;
        this.formPhone = profile.phone ?? '';
        this.formAddress = profile.address ?? '';
        this.formEmergencyContactName = profile.emergencyContactName ?? '';
        this.formEmergencyContactPhone = profile.emergencyContactPhone ?? '';
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load your profile. Please check the server connection.';
        this.isLoading = false;
        console.error('Profile load error:', err);
      }
    });
  }

  save(): void {
    if (this.isSaving) return;
    this.isSaving = true;
    this.actionError = '';
    this.successMessage = '';

    this.essService.updateProfile({
      phone: this.formPhone || undefined,
      address: this.formAddress || undefined,
      emergencyContactName: this.formEmergencyContactName || undefined,
      emergencyContactPhone: this.formEmergencyContactPhone || undefined
    }).subscribe({
      next: () => {
        this.isSaving = false;
        this.successMessage = 'Profile updated.';
        this.load();
      },
      error: (err) => {
        this.isSaving = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to update profile.';
        console.error('Profile update error:', err);
      }
    });
  }
}
