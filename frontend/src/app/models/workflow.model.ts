export interface ContractInput {
  provider: string;
  currentPrice: number;
  duration: number;
  planType: string;
  customerName: string;
}

export interface OfferInfo {
  provider: string;
  price: number;
  features: string[];
  planName: string;
}

export interface WorkflowResult {
  recommendation: string;
  reasoning: string;
  suggestedOffer?: OfferInfo;
  estimatedSavings: number;
  analyzedAt: string;
}

export interface WorkflowStep {
  id: string;
  name: string;
  status: string;
  output?: string;
  startedAt?: string;
  completedAt?: string;
  order: number;
}

export type ExecutionMode = 'dotnet' | 'windmill';

export interface Workflow {
  id: string;
  status: string;
  executionMode: ExecutionMode;
  inputData: ContractInput;
  result?: WorkflowResult;
  createdAt: string;
  updatedAt: string;
  steps: WorkflowStep[];
}

export interface StartWorkflowRequest {
  provider: string;
  currentPrice: number;
  duration: number;
  planType: string;
  customerName: string;
  executionMode: ExecutionMode;
}
