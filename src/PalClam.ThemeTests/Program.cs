// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using PalClam.Services;
using PalClam.UI;
using PalClam.Localization;

namespace PalClam.ThemeTests;

/// <summary>
/// Prueba WPF real de alternancia intensiva de temas + actualizaciones de UI.
/// No ejecuta ClamAV ni altera settings.json, cuarentena ni firmas.
/// Un escaneo real en Windows sigue siendo una prueba de aceptación adicional.
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main()
    {
        Window? window = null;
        Application? app = null;
        try
        {
            app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            PresentationLocalization.Install(app);
            var failures = new List<Exception>();
            app.DispatcherUnhandledException += (_, e) => failures.Add(e.Exception);

            ModernScrollBarTheme.Install(app);
            var tabs = new TabControl { Margin = new Thickness(8) };
            var style = OptionsTabAppearance.CreateStyle();
            var names = new[] { "General", "Apariencia", "Análisis", "Actualizaciones", "Protección", "Integraciones" };
            var resultRows = new ListBox { Height = 120 };
            var stableItems = new[] { "Económico", "Equilibrado", "Prioritario" };
            var priority = new ComboBox { ItemsSource = stableItems, SelectedItem = stableItems[1] };
            var section = PresentationLocalization.Track(new TextBlock { Text = "PROTECCIÓN" });
            var rawFilename = new TextBlock();
            rawFilename.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding { Source = "Estado" });
            var progress = new ProgressBar { Minimum = 0, Maximum = 100, Height = 8 };
            foreach (var name in names)
                tabs.Items.Add(new TabItem
                {
                    Header = new TextBlock { Text = name },
                    Style = style,
                    Content = new StackPanel
                    {
                        Children = { new TextBlock { Text = "Escaneo activo simulado" }, progressClone() }
                    }
                });
            static ProgressBar progressClone() => new() { Minimum = 0, Maximum = 100, Height = 8 };
            var body = new DockPanel();
            DockPanel.SetDock(tabs, Dock.Top);
            DockPanel.SetDock(progress, Dock.Top);
            body.Children.Add(tabs);
            body.Children.Add(progress);
            body.Children.Add(resultRows);
            body.Children.Add(priority);
            body.Children.Add(section);
            body.Children.Add(rawFilename);
            window = new Window
            {
                Title = "PalClam: prueba WPF de temas",
                Width = 720, Height = 310,
                WindowStyle = WindowStyle.ToolWindow,
                ShowActivated = false, ShowInTaskbar = false,
                Opacity = 0.01,
                Left = SystemParameters.VirtualScreenLeft,
                Top = SystemParameters.VirtualScreenTop,
                Content = body
            };
            window.Show();
            DrainUi();

            // Simula un pincel de PalClam compartido con controles ya construidos.
            var liveBrush = new SolidColorBrush(Colors.Purple);
            var status = new TextBlock { Foreground = liveBrush, Text = "Escaneando..." };
            body.Children.Add(status);
            var translatedStatus = new TextBlock { Foreground = liveBrush, TextWrapping = TextWrapping.Wrap };
            LocalizedUi.Bind(translatedStatus, TextBlock.TextProperty, "Header.Scanning");
            LocalizedUi.Bind(translatedStatus, FrameworkElement.ToolTipProperty, "Help.GoTo", "ClamAV");
            body.Children.Add(translatedStatus);
            var hiddenPageTitle = new TextBlock();
            LocalizedUi.Bind(hiddenPageTitle, TextBlock.TextProperty, "Options.Title");
            // Control fuera del árbol visible: las páginas ya construidas también cambian.
            var catalogue = new TextCatalog();
            var displayCatalogue = new DisplayCatalog();
            var originalProgress = progress;
            var originalRows = resultRows;
            var originalCulture = System.Globalization.CultureInfo.CurrentCulture;
            var originalUiCulture = System.Globalization.CultureInfo.CurrentUICulture;
            var tabItems = tabs.Items.Cast<TabItem>().ToArray();
            for (var step = 0; step < 360; step++)
            {
                var palette = AppearanceCatalog.Themes[step % AppearanceCatalog.Themes.Count];
                ModernScrollBarTheme.Apply(app, palette);
                tabs.SelectedIndex = step % tabItems.Length;
                foreach (var tab in tabItems) OptionsTabAppearance.Apply(tab, palette);
                liveBrush.Color = (Color)ColorConverter.ConvertFromString(palette.Accent)!;
                if (liveBrush.IsFrozen)
                    throw new InvalidOperationException("La interfaz congeló un pincel de paleta viva.");
                if (liveBrush.Color != (Color)ColorConverter.ConvertFromString(palette.Accent)!)
                    throw new InvalidOperationException("La paleta compartida no se actualizó.");
                var selected = tabItems[tabs.SelectedIndex];
                var selectedBrush = selected.BorderBrush as SolidColorBrush;
                if (selectedBrush is null || !selectedBrush.IsFrozen ||
                    selectedBrush.Color != (Color)ColorConverter.ConvertFromString(palette.Accent)!)
                    throw new InvalidOperationException("La pestaña activa no conserva el color del tema.");
                if (tabItems.Any(t => t.Style != style || t.Background is not SolidColorBrush ||
                    t.Foreground is not SolidColorBrush || t.BorderBrush is not SolidColorBrush))
                    throw new InvalidOperationException("El cambio de tema reconstruyó estilos o perdió pinceles.");
                if (app.Resources[ModernScrollBarTheme.ThumbKey] is not SolidColorBrush thumb ||
                    thumb.Color == Colors.Transparent)
                    throw new InvalidOperationException("Barra de desplazamiento sin color visible.");
                resultRows.Items.Add("Elemento comprobado " + step);
                if (resultRows.Items.Count > 20) resultRows.Items.RemoveAt(0);
                progress.Value = (step * 17) % 101;
                var language = TextCatalog.Languages[step % TextCatalog.Languages.Count].Id;
                UiText.Current.SetLanguage(language);
                DrainUi();
                if (translatedStatus.Text != catalogue.Get(language, "Header.Scanning") ||
                    hiddenPageTitle.Text != catalogue.Get(language, "Options.Title") ||
                    (string)translatedStatus.ToolTip != catalogue.Format(language, "Help.GoTo", "ClamAV"))
                    throw new InvalidOperationException("El Binding de idioma, la página oculta o sus parámetros no se actualizaron.");
                if (!ReferenceEquals(progress, originalProgress) || !ReferenceEquals(resultRows, originalRows) ||
                    progress.Value != (step * 17) % 101 || resultRows.Items.Count != Math.Min(step + 1, 20))
                    throw new InvalidOperationException("El cambio de idioma perdió controles o progreso simulado.");
                if (!ReferenceEquals(priority.ItemsSource, stableItems) || (string)priority.SelectedItem != "Equilibrado" || rawFilename.Text != "Estado")
                    throw new InvalidOperationException("La presentación modificó un valor técnico o nombre de archivo.");
                if (section.Text != displayCatalogue.Translate("PROTECCIÓN", language))
                    throw new InvalidOperationException("La sección completa no cambió de idioma.");
            }
            DrainUi();
            if (!ReferenceEquals(originalCulture, System.Globalization.CultureInfo.CurrentCulture) ||
                !ReferenceEquals(originalUiCulture, System.Globalization.CultureInfo.CurrentUICulture))
                throw new InvalidOperationException("Se cambió la cultura global del proceso.");
            if (failures.Count != 0) throw new AggregateException(failures);
            Console.WriteLine("[OK] 360 alternancias, 7 temas, 6 idiomas, Binding WPF, página oculta y progreso simulado.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[ERROR] Prueba WPF de temas: " + ex);
            return 1;
        }
        finally
        {
            window?.Close();
            app?.Shutdown();
        }
    }

    private static void DrainUi()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
            new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
}
