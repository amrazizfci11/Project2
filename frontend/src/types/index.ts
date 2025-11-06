export interface User {
  email: string;
  token: string;
  expiresAt: string;
}

export interface LoginData {
  email: string;
  password: string;
}

export interface RegisterData {
  email: string;
  password: string;
  confirmPassword: string;
}

export interface Document {
  id: number;
  fileName: string;
  contentType: string;
  fileSize: number;
  uploadedAt: string;
  analysis?: DocumentAnalysis;
}

export interface DocumentAnalysis {
  id: number;
  projectName?: string;
  projectDuration?: string;
  humanResourcesHierarchy?: string;
  projectStages?: string;
  specialConditions?: string;
  implementationBoundaries?: string;
  analyzedAt: string;
}
