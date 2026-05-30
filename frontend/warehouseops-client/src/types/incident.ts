export type Incident = {
  id: string;
  title: string;
  description: string;
  severity: string;
  relatedEntityType: string;
  relatedEntityId: string;
  status: string;
  resolutionNotes: string;
  closedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type IncidentFilters = {
  status?: string;
};

export type CreateIncidentRequest = {
  title: string;
  description: string;
  severity: string;
  relatedEntityType: string;
  relatedEntityId: string;
};

export type UpdateIncidentStatusRequest = {
  status: string;
};

export type ResolveIncidentRequest = {
  resolutionNotes: string;
};
