import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subscription, interval } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { WorkflowService } from '../../services/workflow.service';
import { Workflow } from '../../models/workflow.model';

@Component({
  selector: 'app-workflow-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './workflow-detail.component.html',
  styleUrls: ['./workflow-detail.component.css']
})
export class WorkflowDetailComponent implements OnInit, OnDestroy {
  workflow: Workflow | null = null;
  loading = true;
  error = '';
  approvalComment = '';
  approving = false;
  private workflowId = '';
  private refreshSub?: Subscription;

  constructor(
    private route: ActivatedRoute,
    private workflowService: WorkflowService
  ) {}

  ngOnInit(): void {
    this.workflowId = this.route.snapshot.paramMap.get('id') || '';
    this.loadWorkflow();
  }

  ngOnDestroy(): void {
    this.refreshSub?.unsubscribe();
  }

  loadWorkflow(): void {
    this.loading = true;
    this.error = '';
    this.workflowService.getWorkflow(this.workflowId).subscribe({
      next: (data) => {
        this.workflow = data;
        this.loading = false;
        this.setupAutoRefresh();
      },
      error: () => {
        this.error = 'Failed to load workflow details.';
        this.loading = false;
      }
    });
  }

  private setupAutoRefresh(): void {
    this.refreshSub?.unsubscribe();
    if (this.workflow && this.isActiveStatus(this.workflow.status)) {
      this.refreshSub = interval(3000).pipe(
        switchMap(() => this.workflowService.getWorkflow(this.workflowId))
      ).subscribe({
        next: (data) => {
          this.workflow = data;
          if (!this.isActiveStatus(data.status)) {
            this.refreshSub?.unsubscribe();
          }
        }
      });
    }
  }

  private isActiveStatus(status: string): boolean {
    const s = status.toLowerCase();
    return s === 'pending' || s === 'running';
  }

  getStatusBadgeClass(status: string): string {
    return `badge badge-status-${status.toLowerCase()}`;
  }

  getStepIconClass(status: string): string {
    return `step-icon ${status.toLowerCase()}`;
  }

  getStepSymbol(status: string): string {
    switch (status.toLowerCase()) {
      case 'completed': return '\u2713';
      case 'failed': return '\u2717';
      case 'running': return '\u25B6';
      default: return '\u25CB';
    }
  }

  get isAwaitingApproval(): boolean {
    return this.workflow?.status.toLowerCase() === 'awaitingapproval';
  }

  get isCompleted(): boolean {
    return this.workflow?.status.toLowerCase() === 'completed';
  }

  get recommendationClass(): string {
    const rec = this.workflow?.result?.recommendation?.toLowerCase() || '';
    if (rec.includes('switch')) return 'recommendation-switch';
    return 'recommendation-keep';
  }

  get sortedSteps() {
    return this.workflow?.steps?.slice().sort((a, b) => a.order - b.order) || [];
  }

  approve(approved: boolean): void {
    this.approving = true;
    const comment = this.approvalComment.trim() || undefined;
    this.workflowService.approveWorkflow(this.workflowId, approved, comment).subscribe({
      next: (data) => {
        this.workflow = data;
        this.approving = false;
        this.approvalComment = '';
        this.setupAutoRefresh();
      },
      error: () => {
        this.error = 'Failed to submit approval. Please try again.';
        this.approving = false;
      }
    });
  }
}
