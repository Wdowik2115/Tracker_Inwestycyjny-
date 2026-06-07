# Investee UI Design System (v2.0)

## Core Philosophy
The Investee platform follows a **Premium Glassmorphism** aesthetic, designed to feel modern, high-tech, and integrated. The UI avoids the "Lego block" feel by using deep blurs, sharp borders, and continuous animated backgrounds.

## Visual Primitives
- **Background:** Global animated mesh/crypto background (must always be visible).
- **Surface:** `rgba(13, 17, 28, 0.4)` with `32px` blur.
- **Borders:** `1px solid rgba(255, 255, 255, 0.15)` (Sharp and distinct).
- **Accent:** `#F5A623` (Investee Gold).
- **Success:** `#00f2fe` (Neon Cyan).
- **Typography:** 'Outfit' (Primary), 'JetBrains Mono' (Stats).

## Layout Standards
1. **Full-Height Sidebar:** Occupies 100% height on the left.
2. **Floating Header:** A centered pill-shaped "Cloud" for core portfolio stats.
3. **Pill-Style Components:** High border radius (24px - 28px) for all major panels.

## Component Classes
- `.glass-panel`: Major page containers.
- `.glass-card`: Interactive dashboard items.
- `.glow-text`: Applied to critical financial figures.
