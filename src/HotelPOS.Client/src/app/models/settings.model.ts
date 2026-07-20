export interface SystemSettings {
  hotelName: string;
  hotelAddress: string;
  hotelPhone: string;
  hotelGst: string;

  defaultPrinter: string;
  showPrintPreview: boolean;
  receiptFormat: string;

  showGstBreakdown: boolean;
  showItemsOnBill: boolean;
  showDiscountLine: boolean;
  showPhoneOnReceipt: boolean;
  showThankYouFooter: boolean;

  enableRoundOff: boolean;
  isCompositionScheme: boolean;

  enableAutomatedBackups: boolean;
  offsiteBackupPath?: string;

  idleTimeoutMinutes: number;

  smtpHost?: string;
  smtpPort: number;
  smtpUsername?: string;
  smtpPasswordSet: boolean;
  smtpUseSsl: boolean;
  smtpFromAddress?: string;
}

export type SaveSettingsRequest = Omit<SystemSettings, 'smtpPasswordSet'> & {
  smtpPassword?: string;
};
