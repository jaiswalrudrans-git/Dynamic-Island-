# System

Owns CPU, GPU, RAM, disk, network, and battery hardware telemetry monitoring and the System tab.

## Key Files
- `Views/SystemPage.xaml` / `.xaml.cs`: Balanced bento grid displaying hardware gauges and top processes.
- `Views/SystemResources.xaml`: Core bar and segment sort button styles.
- `ViewModels/SystemViewModel.cs`: Hardware metric aggregation, formatting, and process sorting.
- `Services/SystemHardwareMonitor.cs`: Background performance telemetry collector.
- `Services/ProcessIconCache.cs`: Win32 shell icon extraction and caching for active processes.
- `Services/DynamicNvml.cs`: Dynamic NVIDIA NVML library loader and metrics query.
- `Services/PdhGpuReader.cs`: Windows Performance Data Helper GPU utilization reader.

## Dependencies
- `Core/` (desktop native metrics, settings)
- `Design/` (tokens, typography, colors)
