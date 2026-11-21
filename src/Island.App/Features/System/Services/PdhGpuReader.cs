using System.Runtime.InteropServices;

namespace Island.App.Features.SystemInfo.Services;

internal sealed class PdhGpuReader : IDisposable
{
    private const uint PDH_FMT_DOUBLE = 0x00000200;
    private const int PDH_MORE_DATA = unchecked((int)0x800007D2);
    private const int ERROR_SUCCESS = 0;

    [DllImport("pdh.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int PdhOpenQuery(nint szDataSource, nint dwUserData, out nint phQuery);

    [DllImport("pdh.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int PdhAddEnglishCounter(nint hQuery, string szFullCounterPath, nint dwUserData, out nint phCounter);

    [DllImport("pdh.dll", SetLastError = true)]
    private static extern int PdhCollectQueryData(nint hQuery);

    [StructLayout(LayoutKind.Sequential)]
    private struct PDH_FMT_COUNTERVALUE_ITEM
    {
        public nint szName;
        public uint CStatus;
        public double doubleValue;
    }

    [DllImport("pdh.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int PdhGetFormattedCounterArray(
        nint hCounter,
        uint dwFormat,
        ref uint lpdwBufferSize,
        ref uint lpdwItemCount,
        nint ItemBuffer);

    [DllImport("pdh.dll", SetLastError = true)]
    private static extern int PdhCloseQuery(nint hQuery);

    private nint query;
    private nint counter;
    private bool initialized;
    private readonly Action<string>? log;

    public bool IsAvailable => initialized;

    public PdhGpuReader(Action<string>? log = null)
    {
        this.log = log;
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            int res = PdhOpenQuery(0, 0, out query);
            if (res != ERROR_SUCCESS || query == 0) return;

            res = PdhAddEnglishCounter(query, "\\GPU Engine(*)\\Utilization Percentage", 0, out counter);
            if (res != ERROR_SUCCESS || counter == 0)
            {
                PdhCloseQuery(query);
                query = 0;
                return;
            }

            PdhCollectQueryData(query);
            initialized = true;
        }
        catch (Exception ex)
        {
            log?.Invoke($"PDH GPU counter init error: {ex.Message}");
            Dispose();
        }
    }

    public double? GetUtilization()
    {
        if (!initialized || query == 0 || counter == 0) return null;

        try
        {
            int res = PdhCollectQueryData(query);
            if (res != ERROR_SUCCESS) return null;

            uint bufferSize = 0;
            uint itemCount = 0;
            res = PdhGetFormattedCounterArray(counter, PDH_FMT_DOUBLE, ref bufferSize, ref itemCount, 0);
            if (res != PDH_MORE_DATA && res != ERROR_SUCCESS) return null;
            if (bufferSize == 0 || itemCount == 0) return 0;

            nint buffer = Marshal.AllocHGlobal((int)bufferSize);
            try
            {
                res = PdhGetFormattedCounterArray(counter, PDH_FMT_DOUBLE, ref bufferSize, ref itemCount, buffer);
                if (res != ERROR_SUCCESS) return null;

                int itemSize = Marshal.SizeOf<PDH_FMT_COUNTERVALUE_ITEM>();
                double total3D = 0;
                var processSums = new Dictionary<string, double>();

                for (int i = 0; i < itemCount; i++)
                {
                    nint itemPtr = buffer + (i * itemSize);
                    var item = Marshal.PtrToStructure<PDH_FMT_COUNTERVALUE_ITEM>(itemPtr);
                    if (item.CStatus != ERROR_SUCCESS || item.doubleValue <= 0) continue;

                    string? name = Marshal.PtrToStringUni(item.szName);
                    if (name == null) continue;

                    // Only count 3D/compute/graphics engines to match Windows Task Manager
                    if (name.Contains("engtype_3D", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("engtype_Compute", StringComparison.OrdinalIgnoreCase))
                    {
                        total3D += item.doubleValue;
                    }
                }

                return Math.Clamp(Math.Round(total3D, 1), 0, 100);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch (Exception ex)
        {
            log?.Invoke($"PDH GPU collect error: {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        initialized = false;
        if (query != 0)
        {
            try { PdhCloseQuery(query); } catch { }
            query = 0;
            counter = 0;
        }
        GC.SuppressFinalize(this);
    }
}
