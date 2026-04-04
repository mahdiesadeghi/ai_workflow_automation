import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { WorkflowService } from '../../services/workflow.service';

@Component({
  selector: 'app-new-workflow',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './new-workflow.component.html',
  styleUrls: ['./new-workflow.component.css']
})
export class NewWorkflowComponent {
  workflowForm: FormGroup;
  submitting = false;
  error = '';

  constructor(
    private fb: FormBuilder,
    private workflowService: WorkflowService,
    private router: Router
  ) {
    this.workflowForm = this.fb.group({
      customerName: ['', [Validators.required]],
      provider: ['', [Validators.required]],
      currentPrice: [null, [Validators.required, Validators.min(0.01)]],
      duration: [null, [Validators.required, Validators.min(1)]],
      planType: ['', [Validators.required]]
    });
  }

  get f() {
    return this.workflowForm.controls;
  }

  onSubmit(): void {
    if (this.workflowForm.invalid) {
      Object.keys(this.f).forEach(key => {
        this.f[key].markAsTouched();
      });
      return;
    }

    this.submitting = true;
    this.error = '';

    this.workflowService.startWorkflow(this.workflowForm.value).subscribe({
      next: (workflow) => {
        this.submitting = false;
        this.router.navigate(['/workflows', workflow.id]);
      },
      error: (err) => {
        this.submitting = false;
        this.error = err.error?.message || 'Failed to start workflow. Please try again.';
      }
    });
  }
}
