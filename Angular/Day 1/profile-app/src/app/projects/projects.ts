import { Component } from '@angular/core';

interface Project {
  title: string;
  description: string;
  tags: string[];
}

@Component({
  selector: 'app-projects',
  imports: [],
  templateUrl: './projects.html',
  styleUrl: './projects.css',
})
export class ProjectsComponent {
  projects: Project[] = [
    {
      title: 'E-Commerce Platform',
      description: 'A full-stack e-commerce application with product listings, cart, and checkout flow.',
      tags: ['Angular', '.NET', 'SQL Server'],
    },
    {
      title: 'Task Management API',
      description: 'RESTful API for managing tasks and projects with authentication and role-based access.',
      tags: ['Node.js', 'TypeScript', 'SQL Server'],
    },
    {
      title: 'Real-Time Dashboard',
      description: 'A live data dashboard with real-time updates using WebSockets and Angular.',
      tags: ['Angular', 'TypeScript', 'Node.js'],
    },
  ];
}
