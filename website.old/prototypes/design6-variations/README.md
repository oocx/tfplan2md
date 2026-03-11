# Design 6 Variations

These are 6 color scheme and styling variations based on Design 6 (Card-Based Modern). All variations maintain the same layout and structure but explore different visual directions.

## Variations Overview

### Variation 1: Blue/Tech Focus (GitHub Colors)
**Theme:** Professional blue palette inspired by GitHub  
**Colors:** Blue (#0969da), green accents  
**Feel:** Clean, professional, developer-friendly  
**Best for:** Technical audiences, GitHub users

### Variation 2: Dark Theme
**Theme:** Modern dark mode with vibrant accents  
**Colors:** Dark backgrounds (#0d1117), blue (#58a6ff), purple (#a371f7)  
**Feel:** Modern, technical, eye-friendly  
**Best for:** Developers who prefer dark mode

### Variation 3: Warm Terraform Colors
**Theme:** Orange and purple inspired by Terraform branding  
**Colors:** Purple (#7b42bc), orange (#ff6b35)  
**Feel:** Warm, branded, distinctive  
**Best for:** Terraform users, brand recognition

### Variation 4: Minimal Monochrome
**Theme:** Black, white, and one accent color  
**Colors:** Black/white with blue accent (#0066ff)  
**Feel:** Bold, minimal, high-contrast  
**Best for:** Modern, minimalist aesthetic

### Variation 5: Green/Nature Tech
**Theme:** Sustainable, eco-friendly green palette  
**Colors:** Green (#16a34a), nature-inspired  
**Feel:** Fresh, sustainable, positive  
**Best for:** Modern, environment-conscious feel

### Variation 6: Soft Pastels
**Theme:** Gentle, approachable pastel colors  
**Colors:** Soft purple (#8b7cb8), salmon (#e9967a), pastels  
**Feel:** Friendly, approachable, soft  
**Best for:** Less technical, more approachable feel

## How to View

Start web servers for each variation:

```bash
cd website/prototypes/design6-variations/var1 && python3 -m http.server 8101 &
cd website/prototypes/design6-variations/var2 && python3 -m http.server 8102 &
cd website/prototypes/design6-variations/var3 && python3 -m http.server 8103 &
cd website/prototypes/design6-variations/var4 && python3 -m http.server 8104 &
cd website/prototypes/design6-variations/var5 && python3 -m http.server 8105 &
cd website/prototypes/design6-variations/var6 && python3 -m http.server 8106 &
```

Then visit:
- Variation 1: http://localhost:8101
- Variation 2: http://localhost:8102
- Variation 3: http://localhost:8103
- Variation 4: http://localhost:8104
- Variation 5: http://localhost:8105
- Variation 6: http://localhost:8106

## Key Differences

| Variation | Primary Color | Secondary | Background | Hero Gradient | Feel |
|-----------|--------------|-----------|------------|---------------|------|
| 1. Blue/Tech | GitHub Blue | Green | White | Light gray | Professional |
| 2. Dark Theme | Bright Blue | Purple | Dark (#0d1117) | Dark gray | Modern dark |
| 3. Terraform | Purple | Orange | White | Purple→Orange | Branded |
| 4. Monochrome | Black | Blue accent | White | Gray | Minimal |
| 5. Green | Green | Emerald | White | Green gradient | Fresh |
| 6. Pastels | Soft Purple | Salmon | Cream | Soft pastels | Friendly |

## What Changed Between Variations

- **Color palette**: Primary, secondary, and accent colors
- **Background colors**: Page and section backgrounds
- **Hero gradient**: Different gradient combinations
- **Border colors**: Adjusted to match palette
- **Shadow colors**: Tinted to match primary color
- **Button styles**: Colors adapted to palette
- **Footer background**: Matches overall theme

## What Stayed the Same

- Layout and structure
- Typography and font sizes
- Spacing and padding
- Responsive breakpoints
- Component design (cards, buttons, etc.)
- Accessibility features
- Navigation structure

## Selection Criteria

Consider these questions:

1. **Does the color palette match the project's identity?**
2. **Is it readable and accessible?**
3. **Does it feel appropriate for a technical tool?**
4. **Will it stand out or blend in?**
5. **Is it timeless or trendy?**
6. **Does it work in both light and dark environments?**

## Next Steps

After selecting a variation:
1. The chosen color scheme will be applied to the full website
2. Additional pages will be created using this design system
3. Final refinements and polish will be applied
4. Website will be ready for deployment
