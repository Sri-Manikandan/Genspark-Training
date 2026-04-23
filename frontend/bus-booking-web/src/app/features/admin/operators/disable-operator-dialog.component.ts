import { Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface DisableOperatorDialogData {
  operatorName: string;
}

@Component({
  selector: 'app-disable-operator-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatDialogModule, MatButtonModule,
    MatFormFieldModule, MatInputModule
  ],
  template: `
    <h2 mat-dialog-title>Disable {{ data.operatorName }}?</h2>
    <mat-dialog-content class="space-y-3">
      <p>
        This will retire all of this operator's buses, cancel every upcoming confirmed
        booking as <strong>cancelled by operator</strong>, queue full refunds, and email
        affected customers. The user's customer account stays active.
      </p>
      <mat-form-field class="w-full">
        <mat-label>Reason (optional)</mat-label>
        <textarea matInput rows="3" [formControl]="reason" maxlength="500"></textarea>
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="warn" (click)="confirm()">Disable operator</button>
    </mat-dialog-actions>
  `
})
export class DisableOperatorDialogComponent {
  readonly data = inject<DisableOperatorDialogData>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<DisableOperatorDialogComponent>);
  readonly reason = new FormControl<string>('', { nonNullable: true });

  confirm(): void {
    this.ref.close({ reason: this.reason.value.trim() || null });
  }
}
