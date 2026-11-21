# Music

Owns now-playing media session integration, album art extraction, loopback audio spectrum analysis, and the Music tab.

## Key Files
- `Views/MusicPage.xaml` / `.xaml.cs`: Music tab UI with transport controls, progress scrubber, and lyrics.
- `Views/ScrollingLabel.cs`: Smooth horizontally scrolling track and artist label.
- `Views/SpectrumMeter.cs`: Multi-bar live audio spectrum visualizer.
- `ViewModels/MusicViewModel.cs`: Media playback state, track metadata, and commands.
- `Services/MediaSessions.cs`: Windows SystemMediaTransportControlsSessionManager integration.
- `Services/MediaArtwork.cs`: Bitmap decoding and dominant cover accent color calculation.
- `Services/LoopbackSpectrum.cs`: WASAPI audio loopback and FFT frequency analysis.

## Dependencies
- `Core/` (settings)
- `Design/` (tokens, typography, colors)
- `Header/` (PanelNavigation button animation)
