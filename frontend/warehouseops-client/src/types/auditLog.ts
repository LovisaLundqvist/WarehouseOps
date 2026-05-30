export type AuditLog = {
  id: string;
  entityName: string;
  action: string;
  performedBy: string;
  performedAt: string;
  changes: string;
  createdAt: string;
  updatedAt: string | null;
};
