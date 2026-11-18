// Static plan step 1 content (design/mockups/plan-step1.png). Wire to the API later.

export interface PlanTile {
  id: string;
  label: string;
  image: string;
  icon: string;
  color: string;
  bg: string;
  /** Label centre from the tile top, mockup px. */
  labelY: number;
  /** How many dishes the customer can see in this meal. */
  count?: number;
}

const img = (name: string) => `assets/screens/plan/${name}.png`;

/** Rows of tiles in RTL order (first item is the right-most). */
export const PLAN_ROWS: PlanTile[][] = [
  [
    { id: 'breakfast', label: 'الفطار', image: img('breakfast'), icon: '', color: '#3A2210', bg: '#FFFCF9', labelY: 98 },
    { id: 'lunch', label: 'الغداء', image: img('lunch'), icon: '', color: '#3A2210', bg: '#FFFCF9', labelY: 98 },
    { id: 'dinner', label: 'العشاء', image: img('dinner'), icon: '', color: '#5B2A86', bg: '#FFFCF9', labelY: 98 }
  ],
  [
    { id: 'snacks', label: 'وجبات خفيفة', image: img('snacks'), icon: '', color: '#1E2A1E', bg: '#FFFCF9', labelY: 91 },
    { id: 'drinks', label: 'مشروبات', image: img('drinks'), icon: '', color: '#1E2A1E', bg: '#FFFCF9', labelY: 91 },
    { id: 'healthy', label: 'أكلات صحية', image: img('healthy'), icon: '', color: '#1E2A1E', bg: '#FFFCF9', labelY: 91 }
  ]
];

export const PLAN_DESSERTS: PlanTile = { id: 'desserts', label: 'حلويات', image: img('desserts'), icon: '', color: '#4A1F2E', bg: '#FFFCF9', labelY: 100 };
