export interface AuditLog {
  sNo: number;
  id: number;
  entityName: string;
  entityId: number;
  action: string;
  timestamp: string;
  details?: string;
  username?: string;
}
