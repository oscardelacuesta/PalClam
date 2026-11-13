// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PalClam.Services;

internal static class OutlookClassicImport
{
    internal static Task ImportAsync(MailImportSession session, MailScanOptions options,
        Action<MailFinding> report, CancellationToken ct)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Outlook clásico requiere Windows.");
        return StartSta(session, options, report, ct);
    }

    [SupportedOSPlatform("windows")]
    private static Task StartSta(MailImportSession session, MailScanOptions options,
        Action<MailFinding> report, CancellationToken ct)
    {
        // No completar la tarea antes de liberar COM: no borrar temporales mientras SaveAs sigue activo.
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try { Export(session, options, report, ct); completion.TrySetResult(); }
            catch (OperationCanceledException) { completion.TrySetCanceled(new CancellationToken(true)); }
            catch (Exception ex) { completion.TrySetException(ex); }
        }) { IsBackground = true, Name = "PalClam Outlook export (STA)" };
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); return completion.Task;
    }

    [SupportedOSPlatform("windows")]
    private static void Export(MailImportSession session, MailScanOptions options,
        Action<MailFinding> report, CancellationToken ct)
    {
        object? application = null, mapi = null, folder = null, items = null;
        try
        {
            ct.ThrowIfCancellationRequested();
            var clsid = new Guid("0006F03A-0000-0000-C000-000000000046");
            try { GetActiveObject(ref clsid, IntPtr.Zero, out application); }
            catch (COMException ex) { throw new InvalidOperationException("Abre Outlook clásico y su perfil antes de analizar. El nuevo Outlook utiliza Microsoft Graph.", ex); }
            mapi = ((dynamic)application!).GetNamespace("MAPI");
            folder = ((dynamic)mapi).PickFolder();
            if (folder is null) throw new OperationCanceledException();
            ct.ThrowIfCancellationRequested();
            string folderLabel = ((dynamic)folder).FolderPath;
            items = ((dynamic)folder).Items;
            int total = ((dynamic)items).Count;
            if (total > options.MaxMessages)
            { session.Truncated = true; session.Notice(report, folderLabel, "Solo se copiará el máximo configurado; el resto de la carpeta queda sin analizar."); }
            for (int index = 1; index <= Math.Min(total, options.MaxMessages); index++)
            {
                ct.ThrowIfCancellationRequested();
                object? item = null; var destination = session.NextPath(".msg");
                var label = folderLabel + " [Outlook #" + index + "]";
                session.Seen++;
                try
                {
                    item = ((dynamic)items).Item(index);
                    if ((int)((dynamic)item).Class != 43)
                    { session.Notice(report, label, "Elemento distinto de un mensaje de correo; no analizado."); continue; }
                    long size = ((dynamic)item).Size;
                    if (size > options.MaxMessageMb * 1048576L || session.Bytes + size > MailImportSession.MaxTemporaryBytes)
                    { session.Notice(report, label, "Mensaje por encima del límite de importación; no analizado."); continue; }
                    // olMSGUnicode = 9: conservar contenido y asuntos Unicode. Nunca Send, Delete, Move, Save, UnRead ni Quit.
                    ((dynamic)item).SaveAs(destination, 9);
                    ct.ThrowIfCancellationRequested();
                    session.Accept(destination, label, options.MaxMessageMb * 1048576L, report);
                    if (session.Bytes >= MailImportSession.MaxTemporaryBytes)
                    { session.Truncated = true; session.Notice(report, folderLabel, "Límite de temporales alcanzado; quedan mensajes sin analizar."); break; }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                { if (File.Exists(destination)) File.Delete(destination); session.Notice(report, label, "Outlook no permitió copiar el mensaje: " + ex.Message, true); }
                finally { Release(item); }
            }
            if ((int)((dynamic)items).Count != total)
                session.Notice(report, folderLabel, "La carpeta de Outlook cambió durante la copia; el resultado es incompleto.", true);
        }
        finally { Release(items); Release(folder); Release(mapi); Release(application); }
    }

    [SupportedOSPlatform("windows")]
    private static void Release(object? value) { if (value is not null && Marshal.IsComObject(value)) Marshal.ReleaseComObject(value); }
    [DllImport("oleaut32.dll", PreserveSig = false)]
    private static extern void GetActiveObject(ref Guid clsid, IntPtr reserved, [MarshalAs(UnmanagedType.IUnknown)] out object? application);
}
