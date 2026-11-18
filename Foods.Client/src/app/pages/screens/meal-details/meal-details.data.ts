// Static meal details content (design/mockups/meal-details.png). Wire to the API later.

const img = (name: string) => `assets/screens/details/${name}.png`;

export const MEAL_DETAILS = {
  title: 'فلفل شامية',
  tag: 'شرقي',
  desc: 'فلفل محشي بالأرز واللحمة بتتبيلة شامية أصيلة',
  video: img('video-main'),
  facts: [
    { icon: 'chef', label: 'متوسط' },
    { icon: 'clock', label: '60 دقيقة' },
    { icon: 'person', label: '4 أفراد', filled: true }
  ],
  /** Shown with the name above the photo (another country's version, the price). */
  topFacts: [] as { icon: string; label: string; filled?: boolean }[],
  /** Two columns, right column first (RTL). */
  ingredients: [
    [
      { name: 'فلفل رومي كبير', qty: '6 حبات', image: img('ing-peppers') },
      { name: 'لحم مفروم', qty: '500 جرام', image: img('ing-meat') },
      { name: 'أرز مصري', qty: '1 كوب', image: img('ing-rice') },
      { name: 'بصل مفروم', qty: '2 حبة متوسطة', image: img('ing-onion') }
    ],
    [
      { name: 'طماطم مفرومة', qty: '2 حبة كبيرة', image: img('ing-tomatoes') },
      { name: 'صلصة طماطم', qty: '3 ملاعق كبيرة', image: img('ing-paste') },
      { name: 'بقدونس مفروم', qty: '½ كوب', image: img('ing-parsley') },
      { name: 'بهارات شامية', qty: '1 ملعقة صغيرة', image: img('ing-spices') }
    ]
  ],
  steps: [
    'تحضير الحشوة: نشوح البصل في قليل من الزيت ثم نضيف اللحم المفروم ونقلب حتى ينضج.',
    'إضافة الأرز والطماطم والصلصة والبقدونس والبهارات، وتقليب المكونات جيدًا.',
    'تجهيز الفلفل: يُفرغ الفلفل من البذور ويُحشى بالخليط.',
    'وضع الفلفل في قدر مناسب مع صلصة خفيفة ويُترك على نار هادئة لمدة 45–60 دقيقة.'
  ],
  videos: [
    { label: 'تجهيز الفلفل', image: img('vid-1') },
    { label: 'تحضير الحشوة', image: img('vid-2') },
    { label: 'حشو الفلفل', image: img('vid-3') },
    { label: 'الوصفة كاملة', image: img('vid-4') }
  ]
};
