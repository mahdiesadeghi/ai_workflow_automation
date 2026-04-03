import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Workflow, StartWorkflowRequest } from '../models/workflow.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class WorkflowService {
  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  startWorkflow(request: StartWorkflowRequest): Observable<Workflow> {
    return this.http.post<Workflow>(`${this.baseUrl}/workflows/start`, request);
  }

  getWorkflow(id: string): Observable<Workflow> {
    return this.http.get<Workflow>(`${this.baseUrl}/workflows/${id}`);
  }

  getAllWorkflows(): Observable<Workflow[]> {
    return this.http.get<Workflow[]>(`${this.baseUrl}/workflows`);
  }

  approveWorkflow(id: string, approved: boolean, comment?: string): Observable<Workflow> {
    return this.http.post<Workflow>(`${this.baseUrl}/workflows/${id}/approve`, {
      approved,
      comment
    });
  }
}
