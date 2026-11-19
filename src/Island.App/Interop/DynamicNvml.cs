using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Island.App.Interop;

public sealed record NvmlDeviceMetrics(
    string Name,
    double GpuPercent,
    long VramUsedBytes,
    long VramTotalBytes,
    double? TemperatureC,
    double? PowerWatts
);

public sealed class DynamicNvml : IDisposable
{
    private nint library;
    private bool initialized;
    private readonly Action<string>? log;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvmlInitDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvmlShutdownDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvmlDeviceGetCountDelegate(out uint count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvmlDeviceGetHandleByIndexDelegate(uint index, out nint device);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvmlDeviceGetNameDelegate(nint device, byte[] name, uint length);

    [StructLayout(LayoutKind.Sequential)]
    private struct NvmlUtilization
    {
        public uint Gpu;
        public uint Memory;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvmlDeviceGetUtilizationRatesDelegate(nint device, out NvmlUtilization utilization);

    [StructLayout(LayoutKind.Sequential)]
    private struct NvmlMemory
    {
        public ulong Total;
        public ulong Free;
        public ulong Used;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvmlDeviceGetMemoryInfoDelegate(nint device, out NvmlMemory memory);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvmlDeviceGetTemperatureDelegate(nint device, int sensorType, out uint temp);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvmlDeviceGetPowerUsageDelegate(nint device, out uint milliwatts);

    private NvmlInitDelegate? nvmlInit;
    private NvmlShutdownDelegate? nvmlShutdown;
    private NvmlDeviceGetCountDelegate? nvmlDeviceGetCount;
    private NvmlDeviceGetHandleByIndexDelegate? nvmlDeviceGetHandleByIndex;
    private NvmlDeviceGetNameDelegate? nvmlDeviceGetName;
    private NvmlDeviceGetUtilizationRatesDelegate? nvmlDeviceGetUtilizationRates;
    private NvmlDeviceGetMemoryInfoDelegate? nvmlDeviceGetMemoryInfo;
    private NvmlDeviceGetTemperatureDelegate? nvmlDeviceGetTemperature;
    private NvmlDeviceGetPowerUsageDelegate? nvmlDeviceGetPowerUsage;

    public bool IsAvailable => initialized;
    public int DeviceCount { get; private set; }

    public DynamicNvml(Action<string>? log = null)
    {
        this.log = log;
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            if (!NativeLibrary.TryLoad("nvml.dll", out library))
            {
                string sysPath = Path.Combine(Environment.SystemDirectory, "nvml.dll");
                string nvsmiPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NVIDIA Corporation", "NVSMI", "nvml.dll");

                if (File.Exists(sysPath)) NativeLibrary.TryLoad(sysPath, out library);
                else if (File.Exists(nvsmiPath)) NativeLibrary.TryLoad(nvsmiPath, out library);
            }

            if (library == 0) return;

            nvmlInit = GetExport<NvmlInitDelegate>("nvmlInit_v2") ?? GetExport<NvmlInitDelegate>("nvmlInit");
            nvmlShutdown = GetExport<NvmlShutdownDelegate>("nvmlShutdown");
            nvmlDeviceGetCount = GetExport<NvmlDeviceGetCountDelegate>("nvmlDeviceGetCount_v2") ?? GetExport<NvmlDeviceGetCountDelegate>("nvmlDeviceGetCount");
            nvmlDeviceGetHandleByIndex = GetExport<NvmlDeviceGetHandleByIndexDelegate>("nvmlDeviceGetHandleByIndex_v2") ?? GetExport<NvmlDeviceGetHandleByIndexDelegate>("nvmlDeviceGetHandleByIndex");
            nvmlDeviceGetName = GetExport<NvmlDeviceGetNameDelegate>("nvmlDeviceGetName");
            nvmlDeviceGetUtilizationRates = GetExport<NvmlDeviceGetUtilizationRatesDelegate>("nvmlDeviceGetUtilizationRates");
            nvmlDeviceGetMemoryInfo = GetExport<NvmlDeviceGetMemoryInfoDelegate>("nvmlDeviceGetMemoryInfo");
            nvmlDeviceGetTemperature = GetExport<NvmlDeviceGetTemperatureDelegate>("nvmlDeviceGetTemperature");
            nvmlDeviceGetPowerUsage = GetExport<NvmlDeviceGetPowerUsageDelegate>("nvmlDeviceGetPowerUsage");

            if (nvmlInit == null || nvmlDeviceGetCount == null || nvmlDeviceGetHandleByIndex == null)
            {
                CleanupLibrary();
                return;
            }

            int initResult = nvmlInit();
            if (initResult != 0)
            {
                log?.Invoke($"NVML init returned error {initResult}");
                CleanupLibrary();
                return;
            }

            uint count = 0;
            nvmlDeviceGetCount(out count);
            DeviceCount = (int)count;
            initialized = count > 0;
        }
        catch (Exception ex)
        {
            log?.Invoke($"NVML initialization failed: {ex.Message}");
            CleanupLibrary();
        }
    }

    private T? GetExport<T>(string name) where T : Delegate
    {
        if (NativeLibrary.TryGetExport(library, name, out nint address))
        {
            return Marshal.GetDelegateForFunctionPointer<T>(address);
        }
        return null;
    }

    public bool TryGetMetrics(int deviceIndex, out NvmlDeviceMetrics? metrics)
    {
        metrics = null;
        if (!initialized || nvmlDeviceGetHandleByIndex == null) return false;

        try
        {
            if (deviceIndex < 0 || deviceIndex >= DeviceCount) deviceIndex = 0;
            int res = nvmlDeviceGetHandleByIndex((uint)deviceIndex, out nint device);
            if (res != 0 || device == 0) return false;

            string name = "NVIDIA GPU";
            if (nvmlDeviceGetName != null)
            {
                byte[] nameBuffer = new byte[96];
                if (nvmlDeviceGetName(device, nameBuffer, (uint)nameBuffer.Length) == 0)
                {
                    name = Encoding.ASCII.GetString(nameBuffer).TrimEnd('\0').Trim();
                }
            }

            double gpuPercent = 0;
            if (nvmlDeviceGetUtilizationRates != null)
            {
                if (nvmlDeviceGetUtilizationRates(device, out var util) == 0)
                {
                    gpuPercent = Math.Clamp(util.Gpu, 0, 100);
                }
            }

            long vramUsed = 0, vramTotal = 0;
            if (nvmlDeviceGetMemoryInfo != null)
            {
                if (nvmlDeviceGetMemoryInfo(device, out var mem) == 0)
                {
                    vramUsed = (long)mem.Used;
                    vramTotal = (long)mem.Total;
                }
            }

            double? tempC = null;
            if (nvmlDeviceGetTemperature != null)
            {
                if (nvmlDeviceGetTemperature(device, 0, out uint temp) == 0)
                {
                    tempC = temp;
                }
            }

            double? powerWatts = null;
            if (nvmlDeviceGetPowerUsage != null)
            {
                if (nvmlDeviceGetPowerUsage(device, out uint mw) == 0)
                {
                    powerWatts = mw / 1000.0;
                }
            }

            metrics = new NvmlDeviceMetrics(name, gpuPercent, vramUsed, vramTotal, tempC, powerWatts);
            return true;
        }
        catch (Exception ex)
        {
            log?.Invoke($"NVML reading failed: {ex.Message}");
            return false;
        }
    }

    private void CleanupLibrary()
    {
        initialized = false;
        if (library != 0)
        {
            try { nvmlShutdown?.Invoke(); } catch { }
            NativeLibrary.Free(library);
            library = 0;
        }
    }

    public void Dispose()
    {
        CleanupLibrary();
        GC.SuppressFinalize(this);
    }
}
