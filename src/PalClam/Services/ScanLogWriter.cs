// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Text;

namespace PalClam.Services;

/// <summary>Guarda el detalle del análisis localmente; no se envían rutas a terceros.</summary>
internal sealed class ScanLogWriter : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly object _sync = new();
    public string FilePath { get; }

    public ScanLogWriter(string target)
    {
        var folder = Path.Combine(PalClam.Services.AppPaths.Root, "ScanLogs");
        Directory.CreateDirectory(folder);
        FilePath = Path.Combine(folder, "scan_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".txt");
        _writer = new StreamWriter(new FileStream(FilePath, FileMode.CreateNew, FileAccess.Write,
            FileShare.Read), new UTF8Encoding(false)) { AutoFlush = true };
        _writer.WriteLine("PalClam 1.0.0 - Informe de análisis ClamAV");
        _writer.WriteLine("Fecha: " + DateTimeOffset.Now);
        _writer.WriteLine("Objetivo: " + target);
        _writer.WriteLine("Nota: El análisis puede omitir archivos por permisos o límites del motor.");
        _writer.WriteLine(new string('-', 72));
    }

    public void WriteLine(string line, bool isError = false)
    {
        lock (_sync) _writer.WriteLine((isError ? "[STDERR] " : "") + line);
    }

    public void Dispose()
    {
        lock (_sync) _writer.Dispose();
    }
}
