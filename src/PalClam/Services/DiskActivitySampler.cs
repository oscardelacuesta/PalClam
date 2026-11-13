// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Runtime.InteropServices;

namespace PalClam.Services;

// PDH solo consulta contadores. No abre discos para escribir ni requiere un controlador.
internal sealed class DiskActivitySampler : IDisposable
{
    private readonly object _sync = new();
    private readonly DiskLedOptions _options;
    private IntPtr _query, _read, _write;
    private bool _primed, _disposed;
    private DateTimeOffset _lastOpen, _lastSpace;
    private string _diagnostic = "";
    private long _free, _total;
    private bool _spaceAvailable;
    internal string DriveRoot { get; }

    internal DiskActivitySampler(DiskLedOptions options)
    {
        _options = DiskLedOptions.Normalize(options);
        DriveRoot = _options.DriveRoot == "auto"
            ? Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\" : _options.DriveRoot;
    }

    internal DiskActivitySnapshot Sample()
    {
        lock (_sync)
        {
            var now = DateTimeOffset.UtcNow;
            if (_disposed || !OperatingSystem.IsWindows())
                return new(false, 0, 0, false, 0, 0, DriveRoot, now, "Los contadores requieren Windows.");
            if (_query == IntPtr.Zero && now - _lastOpen >= TimeSpan.FromSeconds(15)) Open(now);
            var valid = false; double read = 0, write = 0;
            if (_query != IntPtr.Zero)
            {
                var status = PdhCollectQueryData(_query);
                if (status == 0 && _primed)
                {
                    valid = Read(_read, out read) && Read(_write, out write);
                    if (!valid) _diagnostic = "Los contadores aún no ofrecen una muestra válida.";
                    else _diagnostic = "";
                }
                else if (status != 0) { _diagnostic = "No se pudo consultar PDH: " + status.ToString("X8"); Close(); }
                _primed = status == 0;
            }
            if (now - _lastSpace >= TimeSpan.FromSeconds(10))
            {
                _lastSpace = now; _spaceAvailable = false;
                try
                {
                    var drive = new DriveInfo(DriveRoot);
                    if (drive.IsReady && drive.DriveType is DriveType.Fixed or DriveType.Removable)
                    { _free = drive.AvailableFreeSpace; _total = drive.TotalSize; _spaceAvailable = _total > 0; }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { }
            }
            return new(valid, read, write, _spaceAvailable, _free, _total, DriveRoot, now, _diagnostic);
        }
    }

    private void Open(DateTimeOffset now)
    {
        _lastOpen = now;
        var status = PdhOpenQueryW(IntPtr.Zero, UIntPtr.Zero, out _query);
        var instance = _options.AllPhysicalDisks ? @"\PhysicalDisk(_Total)" : @"\LogicalDisk(" + DriveRoot[..2] + ")";
        if (status == 0) status = PdhAddEnglishCounterW(_query, instance + @"\Disk Read Bytes/sec", UIntPtr.Zero, out _read);
        if (status == 0) status = PdhAddEnglishCounterW(_query, instance + @"\Disk Write Bytes/sec", UIntPtr.Zero, out _write);
        if (status != 0) { _diagnostic = "Contadores no disponibles: " + status.ToString("X8"); Close(); }
    }

    private static bool Read(IntPtr counter, out double value)
    {
        value = 0;
        var result = PdhGetFormattedCounterValue(counter, 0x00000200, IntPtr.Zero, out var data);
        if (result != 0 || data.Status > 1 || !double.IsFinite(data.Value)) return false;
        value = Math.Max(0, data.Value); return true;
    }
    private void Close()
    {
        if (_query != IntPtr.Zero) PdhCloseQuery(_query);
        _query = _read = _write = IntPtr.Zero; _primed = false;
    }
    public void Dispose() { lock (_sync) { if (_disposed) return; _disposed = true; Close(); } }

    [StructLayout(LayoutKind.Sequential)]
    private struct CounterValue { internal uint Status; internal double Value; }
    [DllImport("pdh.dll", CharSet = CharSet.Unicode)] private static extern uint PdhOpenQueryW(IntPtr source, UIntPtr userData, out IntPtr query);
    [DllImport("pdh.dll", CharSet = CharSet.Unicode)] private static extern uint PdhAddEnglishCounterW(IntPtr query, string path, UIntPtr userData, out IntPtr counter);
    [DllImport("pdh.dll")] private static extern uint PdhCollectQueryData(IntPtr query);
    [DllImport("pdh.dll")] private static extern uint PdhGetFormattedCounterValue(IntPtr counter, uint format, IntPtr type, out CounterValue value);
    [DllImport("pdh.dll")] private static extern uint PdhCloseQuery(IntPtr query);
}
