# Design
Owns shared visual design system tokens, themes, and typography presets.

## Key Files
- `DesignTokens.xaml`: Spacing, radius, font sizes, colors, and control styles.
- `ThemeResources.cs`: Palette catalog manager, surface brushes, and glow effects.
- `TypographyResources.cs`: 5-role typography presets and pack font resolvers.

## Dependencies
- Uses `Island.Core.Themes` and `Island.Core.Settings`.
- Shared dependency consumed by Shell, Header, and Features.
