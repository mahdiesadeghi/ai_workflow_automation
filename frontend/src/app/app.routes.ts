import { Routes } from '@angular/router';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { NewWorkflowComponent } from './components/new-workflow/new-workflow.component';
import { WorkflowDetailComponent } from './components/workflow-detail/workflow-detail.component';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'workflows/new', component: NewWorkflowComponent },
  { path: 'workflows/:id', component: WorkflowDetailComponent }
];
