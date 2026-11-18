import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ApiService } from '../../../core/api.service';
import { mediaUrl } from '../../../core/environment';
import { MealCard } from '../../../core/models';
import { FaTabBarComponent } from '../shared/tab-bar.component';
import { FaTopBarComponent } from '../shared/top-bar.component';
import { PLAN_ROWS, PlanTile } from './plan.data';

/** Plan step 1, اختر نوع الوجبة (design/mockups/plan-step1.png). */
@Component({
  selector: 'app-plan-screen',
  imports: [FaTopBarComponent, FaTabBarComponent],
  templateUrl: './plan.component.html',
  styleUrl: './plan.component.css'
})
export class PlanScreenComponent implements OnInit {
  meals: PlanTile[] = [];

  private readonly cols = [627, 332, 35];
  private readonly rowTops = [417, 724, 1008];
  private readonly rowHeights = [283, 272, 272];
  private readonly labelYs = [98, 91, 91];

  constructor(private router: Router, private api: ApiService) {}

  ngOnInit(): void {
    this.api.meals().subscribe({
      next: meals => { this.meals = this.mapMeals(meals); }
    });
  }

  open(id: string): void {
    const meal = this.meals.find(item => item.id === id);
    this.router.navigate(['/meals', id], { state: { name: meal?.label } });
  }

  left(index: number): number {
    return this.cols[index % 3];
  }

  top(index: number): number {
    return this.rowTops[Math.floor(index / 3)] ?? this.rowTops[this.rowTops.length - 1];
  }

  height(index: number): number {
    return this.rowHeights[Math.floor(index / 3)] ?? this.rowHeights[this.rowHeights.length - 1];
  }

  labelY(index: number): number {
    return this.labelYs[Math.floor(index / 3)] ?? this.labelYs[this.labelYs.length - 1];
  }

  private mapMeals(meals: MealCard[]): PlanTile[] {
    const fallback = PLAN_ROWS.flat();
    return [...meals]
      .sort((a, b) => (a.sort ?? 0) - (b.sort ?? 0))
      .map((meal, index) => {
        const sample = fallback[index % fallback.length];
        return {
          id: meal.id,
          label: meal.name || sample.label,
          image: mediaUrl(meal.imagePath) || sample.image,
          icon: mediaUrl(meal.iconPath),
          color: meal.textColor || sample.color,
          bg: meal.color?.trim() || '#FFFCF9',
          labelY: sample.labelY,
          count: meal.count ?? 0
        };
      });
  }
}
