# UI & Layout Audit Report

**Date**: 2026-11-19  
**Milestone**: M-Layout: auto-sizing, spacing, readability and design system overhaul  
**Target Hardware/OS**: Windows 11 x64, mixed DPI (100%, 125%, 150%)  

---

## 1. Executive Summary

A comprehensive visual, layout, and ergonomic audit of Island was conducted across all six expanded panel tabs, the header bar, the collapsed notch, and the window composition layer. 

The primary architectural limitation identified is that Island currently operates with a **hardcoded, fixed-size 566×222 DIP inner panel (`Body`) inside a 640×320 DIP black shell** across all tabs, regardless of actual content needs. This causes severe "double-boxing" (a black shape containing a gray panel containing cards), massive dead space on lightweight pages (Tasks empty state, Focus, Quick Controls), and awkward truncation on information-dense pages (System, Music, Calendar/Weather). Furthermore, text styling and spacing have suffered from label-value collision, tiny font sizes (<10 px), raw identifiers, and missing design system tokens.

---

## 2. Global & Window Shell Issues

| Area | Current Behavior | Problem Description | Severity |
| :--- | :--- | :--- | :--- |
| **Shell & Window** | Window fixed at 760×400; Notch clip fixed at 640×320 | Island cannot expand or shrink to fit its content; no dynamic auto-sizing. | **Critical** |
| **Double Boxing** | `Shell` (black) → `Body` (gray `#141820` rounded rect) → Page Cards (`#141822`) | Unnecessary nested bounding boxes; content does not sit directly on the island surface. | **High** |
| **Header Bar** | Header width fixed at 566 px; tabs are icon-only with a fixed 31×30 sliding pill | Active tab has no visible label; tab bar does not expand when selected; time/date and tab bar fight for horizontal space. | **Medium** |
| **Collapsed Notch** | Clock font is 14 px; height is 32 DIP | Slightly small at arm's length on high-DPI laptop displays; time should be at least 16 px semibold. | **Medium** |
| **Design Tokens** | Hardcoded hex colors (`#141822`, `#8E9DAE`, `#BBC8D8`, etc.), font sizes, and radii throughout all XAML files | No single source of truth for colors, margins, radii, or fonts. Inhibits clean runtime theming and glass styling. | **High** |
| **UI Scaling** | UI scales only via Windows system DPI; no in-app Interface Size setting | Laptops at 1080p/125% cannot independently tune Island interface size between Compact, Comfortable, and Large. | **Medium** |

---

## 3. Tab-by-Tab Audit Findings

### 3.1 System Tab
1. **Label-Value Collisions**:
   - `NET4.5 KB/s`, `RAM9.7 GB / 15.7 GB`, `VRAM1.0 GB / 6.0 GB`, `POWER3.9 W`, and `62%55°C` exhibit no horizontal separation between label and value due to `DockPanel` packing without column definitions.
2. **Sensor Wording & Wasted Space**:
   - `CPU N/A` or unavailable temperature sensors take up full text slots instead of hiding gracefully.
3. **GPU Metric Truncation**:
   - GPU name is clipped (`"RTX 4050..."`) because the header dock panel gives only 140 DIP width.
4. **Network Clarity**:
   - Arrows and speeds are ambiguous; daily totals (`Today: 1.7 MB down · 5.3 MB up`) are cramped.
5. **Top Processes Clutter**:
   - Process names are truncated.
   - Identical applications spawn multiple rows (e.g. `LenovoVantage-1234`, `LenovoVantage-5678`) rather than grouping by application.
   - Raw process IDs `(PID)` are displayed directly in the primary row instead of being placed cleanly in tooltips.
   - Missing friendly application names (e.g. `Lenovo Vantage` via `FileDescription` or product name) and executable icons.

### 3.2 Tasks Tab
1. **Large Dead Space**:
   - The tasks page occupies the fixed 222 DIP height even when 0 or 1 task exists.
   - The empty state card is a large, tall container creating a hollow, unfinished look.
2. **Missing Polish in Task Rows**:
   - Completed items lack smooth strikethrough animation.
   - Priority indicators are plain text badges rather than color-coded priority dots/chips.
   - Edit, delete, and move buttons are always visible, cluttering the view instead of appearing on hover.

### 3.3 Quick Controls Tab
1. **Empty Tile Slot & Wasted Bottom Space**:
   - Row 2 has 3 action cards (`Snip`, `Lock`, `Sleep`) and a completely empty 4th column filler.
   - Unused empty space below the cards.
2. **Wrong & Confusing Icons**:
   - `Snip` (screenshot) uses `\uF158` (a 3D cube / HoloLens glyph) instead of scissors (`\uE8C6`).
3. **Inaccurate Tile Subtitles**:
   - Focus Assist tile displays static text `"Settings"` rather than its live system state (`Off`, `Priority only`, `Alarms only`).
4. **Slider Layout**:
   - Sliders are squeezed into side-by-side columns rather than sleek, full-width glass-ready rows with clear output device indicators.

### 3.4 Focus Tab
1. **Header Collisions**:
   - The segment buttons (`Pomodoro`, `Timer`, `Stopwatch`) on the left collide directly with the `Work Focus · Cycle 1 of 4` label on the right when the panel is narrow.
2. **Visual Hierarchy & Empty Space**:
   - Mode controls, title, and cycle counter are jammed on a single row, leaving vast empty space around the timer.
   - The progress bar is a thin 4 px line rather than a prominent focus progress ring.
   - Timer digit size (46 px) is disconnected from the surrounding controls.

### 3.5 Calendar & Weather Tab
1. **Agenda Space Allocation**:
   - When no events exist, the "No events" section wastes vertical space with awkward spacing.
2. **Weather Legibility**:
   - Weather condition text was sometimes missing or squeezed; only the temperature was prominent.
   - Hourly forecast had an unsightly Win32 horizontal scrollbar track cutting off temperatures.
   - Certain WMO codes mapped to unassigned PUA characters resulting in crossed-out box glyphs.

### 3.6 Music Tab
1. **Raw App Identifiers**:
   - Player name button frequently displays raw AUMIDs or package family names (e.g. `SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify` or `msedge.exe`) rather than friendly names (`Spotify`, `Microsoft Edge`, `Brave`, etc.).
2. **Typography & Spacing**:
   - Track title and artist are small (13 px) relative to the 94×94 album art.
   - Lyrics and transport controls lack unified design tokens for spacing and touch targets.

---

## 4. Remediation Plan

1. **Auto-Sizing Island Engine**:
   - Implement dynamic tab measurement (`page.Measure(availableSize)`).
   - Animate the notch clip geometry and shadow smoothly using `SpringValue` with ~350 ms response and small overshoot.
   - Keep OS window bounds pinned at maximum working area constraints (520 DIP min, 62% width max, 70% height max) to eliminate OS-level window resize flicker.
2. **Unified Design System (`DesignTokens.xaml`)**:
   - Centralize all radii (18 card, 12 control, full pill), spacing (4, 8, 12, 16, 24), font sizes (12.5 to 56 px), font weights, and semantic colors.
   - Replace double boxing with unified direct-surface cards.
3. **Interface Scaling System**:
   - Introduce `UiScale` setting with presets (Compact, Comfortable, Large, Auto) and 90%–150% fine slider.
4. **Complete Page Redesigns**:
   - Restructure all 6 tabs according to the exact ergonomic and visual specifications.
