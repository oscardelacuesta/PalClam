// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PalClam.MachineService;

/// <summary>
/// SCM host without third-party NuGet packages. It never communicates with an
/// interactive desktop. STOP and SHUTDOWN cancel the worker cooperatively.
/// </summary>
internal static class WindowsServiceHost
{
    private const uint ServiceWin32OwnProcess = 0x10;
    private const uint ServiceStopped = 1, ServiceStartPending = 2, ServiceStopPending = 3, ServiceRunning = 4;
    private const uint AcceptStop = 1, AcceptShutdown = 4;
    private const uint ControlStop = 1, ControlShutdown = 5;
    private static readonly CancellationTokenSource StopToken = new();
    private static readonly ServiceMainDelegate MainCallback = ServiceMain;
    private static readonly ServiceHandlerDelegate HandlerCallback = Handler;
    private static IntPtr _statusHandle;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ServiceTableEntry
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string? Name;
        public ServiceMainDelegate? Main;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ServiceStatus
    {
        public uint ServiceType, CurrentState, ControlsAccepted, Win32ExitCode,
            ServiceSpecificExitCode, CheckPoint, WaitHint;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
    private delegate void ServiceMainDelegate(uint argumentCount, IntPtr arguments);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate uint ServiceHandlerDelegate(uint control, uint eventType,
        IntPtr eventData, IntPtr context);

    [DllImport("advapi32.dll", EntryPoint = "StartServiceCtrlDispatcherW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool StartServiceCtrlDispatcher([In] ServiceTableEntry[] table);

    [DllImport("advapi32.dll", EntryPoint = "RegisterServiceCtrlHandlerExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr RegisterServiceCtrlHandlerEx(string name,
        ServiceHandlerDelegate handler, IntPtr context);

    [DllImport("advapi32.dll", EntryPoint = "SetServiceStatus", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus status);

    public static int RunService()
    {
        var table = new[]
        {
            new ServiceTableEntry { Name = ServicePaths.ServiceName, Main = MainCallback },
            new ServiceTableEntry { Name = null, Main = null }
        };
        if (StartServiceCtrlDispatcher(table)) return 0;
        return Marshal.GetLastWin32Error();
    }

    public static int RunConsole()
    {
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; StopToken.Cancel(); };
        return RunWorker(StopToken.Token, null);
    }

    private static void ServiceMain(uint argumentCount, IntPtr arguments)
    {
        _statusHandle = RegisterServiceCtrlHandlerEx(ServicePaths.ServiceName, HandlerCallback, IntPtr.Zero);
        if (_statusHandle == IntPtr.Zero) return;
        Report(ServiceStartPending, 0, 0, 10_000);
        var exitCode = 0;
        try
        {
            exitCode = RunWorker(StopToken.Token,
                () => Report(ServiceRunning, AcceptStop | AcceptShutdown, 0, 0));
        }
        catch (Exception ex)
        {
            try { MachineGuardReport.Append("ERROR", "Servicio: " + ex.Message); } catch { }
            exitCode = 1;
        }
        // SCM puede cerrar la conexión tras STOPPED: esta ha de ser la última operación.
        Report(ServiceStopped, 0, (uint)exitCode, 0);
    }

    private static int RunWorker(CancellationToken token, Action? onReady)
    {
        using var worker = new MachineGuard();
        return worker.RunAsync(token, onReady).GetAwaiter().GetResult();
    }

    private static uint Handler(uint control, uint eventType, IntPtr eventData, IntPtr context)
    {
        if (control is ControlStop or ControlShutdown)
        {
            Report(ServiceStopPending, 0, 0, 12_000);
            StopToken.Cancel();
        }
        return 0;
    }

    private static void Report(uint state, uint accepted, uint exitCode, uint waitHint)
    {
        if (_statusHandle == IntPtr.Zero) return;
        var status = new ServiceStatus
        {
            ServiceType = ServiceWin32OwnProcess,
            CurrentState = state,
            ControlsAccepted = accepted,
            Win32ExitCode = exitCode,
            WaitHint = waitHint,
            CheckPoint = state is ServiceStartPending or ServiceStopPending ? 1u : 0u
        };
        SetServiceStatus(_statusHandle, ref status);
    }
}
