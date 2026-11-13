// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private TextBlock? _signatureCenterUserStatus;
    private TextBlock? _signatureCenterMachineStatus;
    private bool _signatureCenterRefreshing;

    private void BuildSignatureCenterPanel(StackPanel page)
    {
        var card = Card(Surface, Outline);
        card.Margin = new Thickness(0, 13, 0, 0);
        var content = new StackPanel();
        content.Children.Add(T("Signature Center PRO · Bases de detección", 16, Ink,
            FontWeights.Bold, new Thickness(0, 0, 0, 8)));
        var detail = T("Las firmas de tu sesión y las del servicio Machine Guard son copias independientes. " +
            "La lectura de archivos y sus fechas NO acredita que sean las últimas versiones oficiales. " +
            "La actualización del servicio es manual, exige autorización UAC y no cambia las bases de otros antivirus.",
            11, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 10));
        detail.TextWrapping = TextWrapping.Wrap;
        content.Children.Add(detail);

        content.Children.Add(T("MI CUENTA · FreshClam", 11, Purple, FontWeights.Bold,
            new Thickness(0, 4, 0, 3)));
        _signatureCenterUserStatus = T("Pendiente de consultar las bases de tu cuenta.", 11, Ink);
        _signatureCenterUserStatus.TextWrapping = TextWrapping.Wrap;
        content.Children.Add(_signatureCenterUserStatus);

        content.Children.Add(T("SERVICIO DEL EQUIPO · Machine Guard", 11, Purple, FontWeights.Bold,
            new Thickness(0, 12, 0, 3)));
        _signatureCenterMachineStatus = T("Pendiente de consultar las firmas compartidas.", 11, Ink);
        _signatureCenterMachineStatus.TextWrapping = TextWrapping.Wrap;
        content.Children.Add(_signatureCenterMachineStatus);

        var actions = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
        actions.Children.Add(ActionButton("↻  Comprobar ambas copias", false,
            (_, _) => _ = RefreshSignatureCenterAsync()));
        actions.Children.Add(ActionButton("Actualizar mi cuenta · FreshClam", false,
            (_, _) => _ = UpdateSignaturesAsync()));
        actions.Children.Add(ActionButton("Actualizar equipo online · UAC", false,
            async (_, _) => await ConfigureMachineServiceAsync("UpdateOnlineSignatures")));
        actions.Children.Add(ActionButton("Restaurar firmas del instalador · UAC", false,
            async (_, _) => await ConfigureMachineServiceAsync("RefreshSignatures")));
        content.Children.Add(actions);

        var caveat = T("Las versiones se leen con sigtool cuando está disponible. Una base no validada, " +
            "un error de actualización o la falta de latido del servicio no se muestran como protección activa. " +
            "El servicio sigue siendo vigilancia reactiva experimental, sin bloqueo preventivo.",
            10.5, Muted, FontWeights.Normal, new Thickness(0, 8, 0, 0));
        caveat.TextWrapping = TextWrapping.Wrap;
        content.Children.Add(caveat);
        card.Child = content;
        page.Children.Add(card);
    }

    private async Task RefreshSignatureCenterAsync()
    {
        if (_signatureCenterRefreshing || _signatureCenterUserStatus is null ||
            _signatureCenterMachineStatus is null) return;
        _signatureCenterRefreshing = true;
        try
        {
            var sharedFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "PalClam", "MachineGuard", "Signatures");
            var localFolder = SignatureStore.DatabaseDirectory;
            var localTask = Task.Run(() => SignatureCenterInventory.Read(localFolder));
            var sharedTask = Task.Run(() => SignatureCenterInventory.Read(sharedFolder));
            var local = await localTask;
            var shared = await sharedTask;
            var scanner = _engineBox?.Text?.Trim() ?? "";
            var localSigtool = Path.Combine(Path.GetDirectoryName(scanner) ?? "", "sigtool.exe");
            var machineSigtool = Path.Combine(AppContext.BaseDirectory, "ClamAV", "sigtool.exe");

            _signatureCenterUserStatus.Text = await DescribeSignatureFolderAsync(
                "Cuenta actual", local, localSigtool) +
                "\nÚltimo FreshClam correcto registrado: " +
                (_settings.LastSignatureCheckUtc == DateTimeOffset.MinValue ? "sin registro" :
                    _settings.LastSignatureCheckUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm")) +
                (string.IsNullOrWhiteSpace(_settings.LastSignatureUpdateError) ? "" :
                    "\nÚltima incidencia: " + _settings.LastSignatureUpdateError);
            _signatureCenterMachineStatus.Text = await DescribeSignatureFolderAsync(
                "Equipo compartido", shared, machineSigtool) +
                "\nLa actualización online del equipo requiere una instalación compartida y UAC.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
            InvalidOperationException or ArgumentException or System.Security.SecurityException)
        {
            _signatureCenterUserStatus.Text = "No se pudieron consultar todas las bases: " + ex.GetType().Name;
            _signatureCenterMachineStatus.Text = "Consulta el registro del servicio y los permisos de ProgramData.";
        }
        finally { _signatureCenterRefreshing = false; }
    }

    private static async Task<string> DescribeSignatureFolderAsync(string title,
        SignatureFolderSnapshot snapshot, string sigtool)
    {
        var sb = new StringBuilder(title).Append(": ").Append(snapshot.Describe());
        foreach (var family in snapshot.Files)
        {
            sb.Append("\n").Append(family.Family).Append(": ")
                .Append(family.FileName).Append(" · ")
                .Append(family.ModifiedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
        }
        if (snapshot.MainAndDailyPresent && File.Exists(sigtool))
        {
            var versions = await SignatureInfoService.ReadFolderAsync(
                snapshot.Folder, sigtool, CancellationToken.None);
            foreach (var item in versions.Where(x => x.Version != "No disponible"))
            {
                sb.Append("\n").Append(item.Name).Append(" · sigtool v")
                    .Append(item.Version);
                if (item.Signatures is long n) sb.Append(" · ").Append(n.ToString("N0")).Append(" firmas");
            }
        }
        else if (snapshot.MainAndDailyPresent)
            sb.Append("\nNo hay sigtool disponible para comprobar versiones.");
        return sb.ToString();
    }
}
