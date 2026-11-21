# Island Production Fix Log

This log tracks every defect identified during the Stage A audit, the exact changes made, their root causes, and verification status across Stages B through G.

---

## Stage A: Audit & Issue Catalog
- **Date**: 2026-11-21
- **Status**: Completed
- **Baseline Git Commit**: `6f79d2c`
- **Artifacts Created**:
  - `docs/AUDIT.md`: Catalog of 12 distinct defect areas with root cause analysis.
  - `docs/screens/audit/`: 144 snapshot captures spanning all 8 tabs, 3 typography presets (Classic, Dot Matrix, LED), 2 DPIs (100%, 125%), and 3 surface transparencies (0%, 12%, 50%).
  - `tests/Island.Tests/AuditCaptureRunner.cs`: Automated offscreen rendering harness.

---

## Stage B: Foundation Fixes
- **Status**: Completed
- **Changes Implemented**:
  1. **Font Fallback Chains & Glyph Coverage (`TypographyResources.cs`)**:
     - Rewrote `CreateFamily` to generate valid WPF composite font fallback chains ending in `Segoe UI Variable Text, Segoe UI, Segoe UI Symbol, Segoe UI Emoji, Arial`.
     - Integrated `HasGeneralTextCoverage` using `GlyphTypeface.CharacterToGlyphMap` ensuring A-Z, a-z, 0-9 and standard punctuation exist.
     - Strictly forbade decorative fonts (`Doto`, `Silkscreen`, `Bebas Neue`, `Caveat`) on `Heading`, `Body`, and `Label` roles.
     - Protected transport icons in `FocusPage.xaml` by binding foreground to `{DynamicResource Theme.Background}` and enforcing Segoe Fluent/MDL2 icon font families.
  2. **Auto-Sizing & Layout Engine Fixes (`OverlayWindow.Panel.cs`, `PanelNavigation.cs`)**:
     - Fixed `Body.Height` calculation in `OverlayWindow.Panel.cs` which had hardcoded `- 76` DIP overhead instead of dynamic `overhead = 112` DIP (`12 top + 80 header + 8 gap + 12 bottom`).
     - Eliminated bottom clipping across all pages (Top Apps 5th row now has full clearance and 12 DIP bottom margin).
     - Fixed `LeftOfClock` header column sizing so `HeaderLeftCol` uses `GridLength.Auto`, preventing tab bar squishing.
  3. **Thin Overlay Scrollbars (`DesignTokens.xaml`)**:
     - Introduced modern Windows 11-style overlay scrollbar style (5 DIP width, rounded pill thumb `CornerRadius="2.5"`, transparent track, 45% resting opacity, 85% hover, 100% active drag).
     - Replaced default Windows gray scrollbars across the application.
  4. **Active Tab Pill Styling (`OverlayWindow.xaml`, `PanelNavigation.cs`, `ThemeResources.cs`)**:
     - Replaced solid saturated blue block and pink drop shadow with theme `AccentSoft` brush (accent color at 20% opacity).
     - Active tab icon and label now render in crisp `Theme.Accent`, matching native Windows 11 aesthetic.
  5. **Text Overflow Protection (`SystemPage.xaml`)**:
     - Removed hardcoded `MaxWidth="105"` on GPU name, allowing elastic sizing with ellipsis and full tooltip.
     - Network daily totals and storage drives equipped with `TextTrimming="CharacterEllipsis"` and full tooltips.
     - Added app icon glyphs `\uE71D` to Top Apps process rows.
  6. **Theme Contrast Compliance (`Palettes.json`, `ThemeTests.cs`)**:
     - Updated `One Dark Pro` foreground and secondary text colors to exceed 7:1 (primary) and 4.5:1 (secondary) WCAG contrast.
     - Added unit tests in `ThemeTests.cs` and `TypographyTests.cs` validating all 14 themes at 0% and 50% transparency and all presets for character coverage.

---

## Stage C: Merge Clipboard & Screenshots
- **Status**: Completed
- **Changes Implemented**:
  1. **Unified Dual-Half Layout (`ClipboardPage.xaml`, `ClipboardPage.xaml.cs`)**:
     - Merged separate Clipboard and Screenshots tabs into a single unified 7th tab "Clipboard" (`\uE8C8`).
     - Left Half: Scrollable recent copies with type icons (text, code, HTML, file list, image thumbnail), snippet preview, character counts, relative timestamps, and pin/unpin toggles.
     - Right Half: 3x2 uniform grid (`UniformGrid Columns="3"`) displaying 6 recent screenshot thumbnails and snips, dividing width equally and filling 100% of the right width without clipping.
     - Top Utility Strip: Filter/search input, copy count and screenshot count badges, pause/resume capture toggle, Clear All button, and "New Snip" button.
     - Compact Empty State: When empty, displays a clean centered empty prompt without wasting vertical space.
  2. **Model & ViewModel Consolidation (`ClipboardViewModel.cs`, `PanelTab.cs`, `PanelNavigation.cs`)**:
     - Merged clipboard items and screenshot collections into `ClipboardViewModel`.
     - Supports double-click / Ctrl+click to copy, single-click to paste into background target app without activating window, and full OLE drag-and-drop out.
     - Background thumbnail loading downsampled to 240px (`DecodePixelWidth = 240`) preventing memory spikes.
     - Updated tab enumeration to 7 tabs in `PanelTab.cs`, `PanelNavigation.cs`, and `OverlayWindow.Panel.cs`.
  3. **Settings Normalization (`AppSettings.cs`)**:
     - Filtered legacy "screenshots" tab from `TabOrder` and `HiddenTabs` during normalization and deserialization.
  4. **Verification**:
     - Removed obsolete `ScreenshotsPage.xaml` and `ScreenshotsPage.xaml.cs`.
     - Updated `LayoutTests.cs` and `AuditCaptureRunner.cs` to test the 7-tab architecture.
     - Verified clean builds in Debug and Release with warnings as errors and all 165 unit/layout tests passing.

---

## Stage D: Per-Tab Polish & Settings Sidebar
- **Status**: Pending

---

## Stage E: Robustness & Production Readiness
- **Status**: Pending

---

## Stage F: Automated Test Suite & Build Automation
- **Status**: Pending

---

## Stage G: Packaging & Final Verification
- **Status**: Pending
