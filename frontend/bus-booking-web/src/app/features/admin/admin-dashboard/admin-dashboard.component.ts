import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss'
})
export class AdminDashboardComponent {
  readonly navItems = [
    { ix: '01', route: '/admin/cities', title: 'Cities', desc: 'Add, deactivate, rename' },
    { ix: '02', route: '/admin/routes', title: 'Routes', desc: 'Pair cities for operators' },
    { ix: '03', route: '/admin/platform-fee', title: 'Platform fee', desc: '₹ fixed or percent · history' },
    { ix: '04', route: '/admin/operator-requests', title: 'Operator requests', desc: 'Review applications' },
    { ix: '05', route: '/admin/bus-approvals', title: 'Bus approvals', desc: 'Approve or reject submissions' },
    { ix: '06', route: '/admin/operators', title: 'Operators', desc: 'Enable or disable operators' },
    { ix: '07', route: '/admin/bookings', title: 'Bookings', desc: 'Cross-operator history' },
    { ix: '08', route: '/admin/revenue', title: 'Revenue', desc: 'GMV and platform-fee income' },
  ];
}
