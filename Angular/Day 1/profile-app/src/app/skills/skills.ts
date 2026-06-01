import { Component } from '@angular/core';

@Component({
  selector: 'app-skills',
  imports: [],
  templateUrl: './skills.html',
  styleUrl: './skills.css',
})
export class SkillsComponent {
  skills = ['Angular', '.NET', 'SQL Server', 'Node.js', 'TypeScript'];
}
