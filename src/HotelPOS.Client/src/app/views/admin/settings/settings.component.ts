import { Component, OnInit } from '@angular/core';
import { SettingsService } from '../../../services/settings.service';
import { SystemSettings } from '../../../models/settings.model';

@Component({
  standalone: false,
  selector: 'app-settings',
  templateUrl: './settings.component.html',
})
export class SettingsComponent implements OnInit {
  settings: SystemSettings | null = null;
  smtpPassword = '';

  isLoading = false;
  loadError = '';
  actionError = '';
  isSaving = false;
  savedMessage = '';

  constructor(private readonly settingsService: SettingsService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.loadError = '';
    this.settingsService.getSettings().subscribe({
      next: (settings) => {
        this.settings = settings;
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load settings. Please check the server connection.';
        this.isLoading = false;
        console.error('Settings load error:', err);
      }
    });
  }

  save(): void {
    if (!this.settings || this.isSaving) return;
    this.isSaving = true;
    this.actionError = '';
    this.savedMessage = '';

    const { smtpPasswordSet, ...rest } = this.settings;
    this.settingsService.saveSettings({
      ...rest,
      smtpPassword: this.smtpPassword || undefined
    }).subscribe({
      next: () => {
        this.isSaving = false;
        this.smtpPassword = '';
        this.savedMessage = 'Settings saved.';
        this.load();
      },
      error: (err) => {
        this.isSaving = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to save settings.';
        console.error('Settings save error:', err);
      }
    });
  }
}
