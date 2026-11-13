// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Diagnostics;
using PalClam.Services;
using System.Windows;
using System.Windows.Controls;

namespace PalClam;

public sealed partial class MainWindow
{
    private const string PersonalProfileUrl = "https://www.palentino.es/oscardelacuesta.html";
    private const string PalentinoUrl = "https://www.palentino.es/";
    private const string ClamAvOfficialUrl = "https://www.clamav.net/";
    private const string ModernTerminalAiUrl = "https://modernterminal-ai.com/";

    private void BuildAboutPage(StackPanel page)
    {
        page.Children.Add(T("Acerca de PalClam", 26, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 8)));
        page.Children.Add(T("Proyecto independiente de análisis antivirus para Windows.",
            13, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 19)));

        page.Children.Add(T(AppPaths.IsPortable
            ? "Edición portable: preferencias, firmas e historial en Data junto al programa. Las claves de IA guardadas siguen vinculadas al usuario de Windows."
            : "Edición instalada: los datos se guardan en el perfil de Windows.", 12, Muted));
        var identity = Card(Surface, Outline);
        identity.Margin = new Thickness(0, 0, 0, 15);
        var author = new StackPanel();
        author.Children.Add(T("PalClam 1.2.2", 20, Purple, FontWeights.Bold,
            new Thickness(0, 0, 0, 5)));
        author.Children.Add(ActionButton("palclam.com", false, (_, _) => Process.Start(new ProcessStartInfo(ProductLinks.Website) { UseShellExecute = true })));
        author.Children.Add(T(AppPaths.RuntimeId == "win-x86" ? "Windows 32 bits · Motor ClamAV · Software libre" : "Windows 64 bits · Motor ClamAV · Software libre", 12, Muted,
            FontWeights.Normal, new Thickness(0, 0, 0, 14)));
        author.Children.Add(T("Autor y desarrollo", 11, Muted, FontWeights.Bold));
        author.Children.Add(T("Óscar de la Cuesta · Palentino.es", 17, Ink,
            FontWeights.Bold, new Thickness(0, 5, 0, 8)));
        author.Children.Add(T("Proyecto de utilidades de escritorio y seguridad, desarrollado de forma independiente.",
            12, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 12)));
        var personalLinks = new WrapPanel();
        personalLinks.Children.Add(ActionButton("↗  Perfil y trayectoria del autor", false,
            (_, _) => OpenAboutWebLink(PersonalProfileUrl)));
        personalLinks.Children.Add(ActionButton("↗  Palentino.es", false,
            (_, _) => OpenAboutWebLink(PalentinoUrl)));
        author.Children.Add(personalLinks);
        identity.Child = author;
        page.Children.Add(identity);

        var helpButton = ActionButton("?  Ayuda: características y ventajas de PalClam", false,
            (_, _) => Navigate("help"));
        helpButton.Margin = new Thickness(0, 0, 0, 14);
        page.Children.Add(helpButton);

        var technology = Card(Surface, Outline);
        technology.Margin = new Thickness(0, 0, 0, 15);
        var technologyStack = new StackPanel();
        technologyStack.Children.Add(T("Tecnología y créditos", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 10)));
        technologyStack.Children.Add(T("PalClam dispone de interfaz y funciones propias. Utiliza ClamAV como motor antivirus externo y FreshClam para las actualizaciones de firmas. La detección depende de la versión del motor y de las firmas disponibles.",
            12, Slate, FontWeights.Normal, new Thickness(0, 0, 0, 9)));
        technologyStack.Children.Add(T("Incluye bibliotecas de terceros para correo e integración opcional con Outlook, con sus respectivas condiciones de licencia.",
            12, Slate, FontWeights.Normal, new Thickness(0, 0, 0, 12)));
        var techLinks = new WrapPanel();
        techLinks.Children.Add(ActionButton("↗  ClamAV oficial", false,
            (_, _) => OpenAboutWebLink(ClamAvOfficialUrl)));
        techLinks.Children.Add(ActionButton("▤  Avisos de terceros", false,
            (_, _) => OpenAboutLocalDocument("Source/THIRD-PARTY-NOTICES.md")));
        technologyStack.Children.Add(techLinks);
        technology.Child = technologyStack;
        page.Children.Add(technology);

        var legal = Card(B("#F7F9FF"), Outline);
        legal.Margin = new Thickness(0, 0, 0, 15);
        var legalStack = new StackPanel();
        legalStack.Children.Add(T("Licencia, privacidad y avisos legales", 16, Ink,
            FontWeights.Bold, new Thickness(0, 0, 0, 12)));
        legalStack.Children.Add(T("• PalClam es software libre; el código propio se distribuye bajo GPL-3.0-or-later. Las dependencias mantienen sus licencias y avisos correspondientes.",
            12, Slate, FontWeights.Normal, new Thickness(0, 0, 0, 8)));
        legalStack.Children.Add(T("• ClamAV es un proyecto independiente. PalClam no está desarrollado, aprobado ni respaldado por Cisco o Talos.",
            12, Slate, FontWeights.Normal, new Thickness(0, 0, 0, 8)));
        legalStack.Children.Add(T("• La información de análisis y cuarentena se guarda localmente. La actualización de firmas y motor utiliza Internet; la conexión opcional con Outlook requiere autorización y accede a los mensajes seleccionados.",
            12, Slate, FontWeights.Normal, new Thickness(0, 0, 0, 8)));
        legalStack.Children.Add(T("• PalClam AI es opcional: los modelos locales permanecen en este equipo y las API remotas reciben únicamente el texto autorizado en cada consulta; pueden aplicar políticas y costes propios. Las claves guardadas se cifran con DPAPI del usuario.",
            12, Slate, FontWeights.Normal, new Thickness(0, 0, 0, 8)));
        legalStack.Children.Add(T("• La detección de una amenaza no está garantizada. Guard observa cambios de archivos, pero no proporciona bloqueo preventivo en Windows ni sustituye Microsoft Defender.",
            12, Slate, FontWeights.Normal, new Thickness(0, 0, 0, 8)));
        legalStack.Children.Add(T("• Las firmas pueden estar desactualizadas sin conexión. No deben interpretarse los análisis cancelados o incompletos como resultados limpios.",
            12, Slate, FontWeights.Normal, new Thickness(0, 0, 0, 13)));
        var legalActions = new WrapPanel();
        legalActions.Children.Add(ActionButton("▤  Leer licencia GPLv3", false,
            (_, _) => OpenAboutLocalDocument("LICENSE-GPL-3.0.txt")));
        legalActions.Children.Add(ActionButton("▤  Información legal completa", false,
            (_, _) => OpenAboutLocalDocument("Source/docs/TERCEROS-Y-LEGAL.md")));
        legalStack.Children.Add(legalActions);
        legal.Child = legalStack;
        page.Children.Add(legal);

        page.Children.Add(T("PalClam · Proyecto independiente de Óscar de la Cuesta · " + DateTime.Now.Year + ". " +
            "Antes de la distribución pública deben revisarse marca, redistribución de firmas y licencias del paquete final.",
            11, Muted, FontWeights.Normal, new Thickness(2, 3, 2, 8)));
    }

    private void OpenAboutWebLink(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps) return;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = uri.AbsoluteUri, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se pudo abrir el enlace: " + ex.Message,
                "Acerca de PalClam", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OpenAboutLocalDocument(string relativePath)
    {
        // Los documentos legales son instalados en {app} por Inno Setup.
        // Abrir sólo rutas de un catálogo cerrado definido en la propia interfaz.
        var path = Path.Combine(AppContext.BaseDirectory,
            relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            MessageBox.Show(this, "Documento no disponible en esta carpeta. " +
                "Consulta el ZIP de fuentes o el apartado Source del instalador.",
                "Documentación", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se pudo abrir el documento: " + ex.Message,
                "Documentación", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
