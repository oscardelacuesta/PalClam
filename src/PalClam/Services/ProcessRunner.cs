// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Diagnostics;

namespace PalClam.Services;

internal sealed record ProcessResult(int ExitCode);

/// <summary>Instantánea del proceso clamscan, no del consumo de todo Windows.</summary>
internal sealed record EngineProcessSample(int ProcessId, double MachineCpuPercent,
    long WorkingSetBytes, long PeakWorkingSetBytes, DateTimeOffset Timestamp);

internal static class ProcessRunner
{
    /// <summary>Ejecuta una herramienta sin shell. Muestra CPU/RAM solo cuando se solicita.
    /// No eleva privilegios, no añade procesos de antivirus ni comparte datos fuera del equipo.</summary>
    public static async Task<ProcessResult> RunAsync(
        string executable,
        IEnumerable<string> arguments,
        Action<string, bool> onLine,
        CancellationToken cancellationToken,
        Action<EngineProcessSample>? onMetrics = null,
        ProcessPriorityClass? priority = null)
    {
        var start = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = Path.GetDirectoryName(executable) ?? AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var arg in arguments) start.ArgumentList.Add(arg);
        using var process = new Process { StartInfo = start };
        var stdoutDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stderrDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) { stdoutDone.TrySetResult(true); return; }
            try { onLine(e.Data, false); } catch { /* no lanzar excepción en evento del proceso */ }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) { stderrDone.TrySetResult(true); return; }
            try { onLine(e.Data, true); } catch { }
        };

        cancellationToken.ThrowIfCancellationRequested();
        if (!process.Start()) throw new InvalidOperationException("No se pudo iniciar ClamAV.");
        if (priority is not null)
        {
            try { process.PriorityClass = priority.Value; }
            catch (System.ComponentModel.Win32Exception) { }
            catch (InvalidOperationException) { }
            catch (System.Security.SecurityException) { }
        }
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        using var registration = cancellationToken.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
        });
        using var monitorCancellation = new CancellationTokenSource();
        Task? monitorTask = onMetrics is null ? null :
            ObserveAsync(process, onMetrics, monitorCancellation.Token);
        try
        {
            await process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAll(stdoutDone.Task, stderrDone.Task);
            cancellationToken.ThrowIfCancellationRequested();
            return new ProcessResult(process.ExitCode);
        }
        finally
        {
            monitorCancellation.Cancel();
            if (monitorTask is not null)
            {
                try { await monitorTask; }
                catch (OperationCanceledException) { }
            }
        }
    }

    private static async Task ObserveAsync(Process process, Action<EngineProcessSample> report,
        CancellationToken cancel)
    {
        var previousStamp = Stopwatch.GetTimestamp();
        var previousCpu = TimeSpan.Zero;
        try { previousCpu = process.TotalProcessorTime; }
        catch (InvalidOperationException) { return; }
        while (!cancel.IsCancellationRequested)
        {
            await Task.Delay(750, cancel);
            try
            {
                if (process.HasExited) return;
                process.Refresh();
                var currentStamp = Stopwatch.GetTimestamp();
                var cpu = process.TotalProcessorTime;
                var elapsedSeconds = Stopwatch.GetElapsedTime(previousStamp, currentStamp).TotalSeconds;
                var usage = ScanPerformanceMath.CpuShareOfMachine(cpu - previousCpu,
                    elapsedSeconds, Environment.ProcessorCount);
                previousStamp = currentStamp;
                previousCpu = cpu;
                report(new EngineProcessSample(process.Id, usage,
                    process.WorkingSet64, process.PeakWorkingSet64, DateTimeOffset.Now));
            }
            catch (InvalidOperationException) { return; }
            catch (System.ComponentModel.Win32Exception) { return; }
            catch (Exception) { /* la instrumentación nunca debe interrumpir el escáner */ }
        }
    }
}
