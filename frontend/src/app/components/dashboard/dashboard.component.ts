import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subscription, interval } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { WorkflowService } from '../../services/workflow.service';
import { Workflow } from '../../models/workflow.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  workflows: Workflow[] = [];
  loading = true;
  error = '';
  private refreshSub?: Subscription;

  constructor(private workflowService: WorkflowService) {}

  ngOnInit(): void {
    this.loadWorkflows();
    this.refreshSub = interval(5000).pipe(
      switchMap(() => this.workflowService.getAllWorkflows())
    ).subscribe({
      next: (data) => this.workflows = data,
      error: () => {} // silent refresh errors
    });
  }

  ngOnDestroy(): void {
    this.refreshSub?.unsubscribe();
  }

  loadWorkflows(): void {
    this.loading = true;
    this.error = '';
    this.workflowService.getAllWorkflows().subscribe({
      next: (data) => {
        this.workflows = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load workflows. Please check if the API server is running.';
        this.loading = false;
      }
    });
  }

  getStatusBadgeClass(status: string): string {
    const s = status.toLowerCase();
    return `badge badge-status-${s}`;
  }

  shortenId(id: string): string {
    return id.length > 8 ? id.substring(0, 8) + '...' : id;
  }

  get totalCount(): number {
    return this.workflows.length;
  }

  get runningCount(): number {
    return this.workflows.filter(w =>
      w.status.toLowerCase() === 'running' || w.status.toLowerCase() === 'pending'
    ).length;
  }

  get completedCount(): number {
    return this.workflows.filter(w => w.status.toLowerCase() === 'completed').length;
  }

  get failedCount(): number {
    return this.workflows.filter(w =>
      w.status.toLowerCase() === 'failed' || w.status.toLowerCase() === 'rejected'
    ).length;
  }
}
