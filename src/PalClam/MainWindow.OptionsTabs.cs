// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using PalClam.UI;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private System.Windows.Controls.TabControl? _optionsTabs;

    // Mover los controles ya creados conserva sus eventos, validación, estado y persistencia.
    // La cabecera responde al ancho disponible: TabPanel agrupa filas sin cortar títulos.
    private void OrganizeOptionsIntoTabs(StackPanel page)
    {
        // 2 encabezados, 10 secciones y la confirmación de preferencias.
        if (page.Children.Count != 13)
            throw new InvalidOperationException(
                "Ha cambiado la estructura de Opciones; revisa los grupos antes de presentar pestañas.");

        var original = page.Children.Cast<UIElement>().ToArray();
        for (var i = original.Length - 1; i >= 2; i--)
            page.Children.RemoveAt(i);

        var tabs = new System.Windows.Controls.TabControl
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            ItemContainerStyle = OptionsTabAppearance.CreateStyle(),
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 0, 9),
            TabStripPlacement = Dock.Top
        };
        _optionsTabs = tabs;

        void Add(string nameKey, string iconGeometry, string introduction, params int[] sectionIndices)
        {
            var content = new StackPanel();
            var introductionText = T(introduction, 11.5, Muted, FontWeights.Normal,
                new Thickness(0, 9, 0, 7));
            introductionText.TextWrapping = TextWrapping.Wrap;
            if (nameKey == "Options.Tab.Appearance")
                LocalizedUi.Bind(introductionText, TextBlock.TextProperty, "Options.AppearanceIntro");
            content.Children.Add(introductionText);
            foreach (var idx in sectionIndices)
                content.Children.Add(original[idx]);

            var tab = new TabItem
            {
                Header = BuildOptionTabHeader(nameKey, iconGeometry),
                Content = content,
                // ItemContainerStyle puede no aplicarse a un TabItem agregado directamente.
                // Asignarlo también aquí garantiza los estilos en todas las versiones de WPF.
                Style = tabs.ItemContainerStyle
            };
            LocalizedUi.Bind(tab, FrameworkElement.ToolTipProperty, nameKey);
            LocalizedUi.Bind(tab, System.Windows.Automation.AutomationProperties.NameProperty, nameKey);
            // Actualización visual local: no volver a sellar ni sustituir estilos
            // mientras un análisis actualiza métricas o listas de resultados.
            tab.MouseEnter += (_, _) => RefreshSingleOptionsTab(tab);
            tab.MouseLeave += (_, _) => RefreshSingleOptionsTab(tab);
            tab.GotKeyboardFocus += (_, _) => RefreshSingleOptionsTab(tab);
            tab.LostKeyboardFocus += (_, _) => RefreshSingleOptionsTab(tab);
            tabs.Items.Add(tab);
        }

        // Iconos geométricos originales, independientes de fuentes externas o emojis.
        // Mantener este orden: los accesos rápidos usan índices 1 y 3.
        Add("Options.Tab.General", "M4,7 H10 M14,7 H20 M10,4 V10 M4,17 H6 M10,17 H20 M8,14 V20",
            "Comportamiento de PalClam, resultados y avisos de Windows.", 7);
        Add("Options.Tab.Appearance", "M12,3 A9,9 0 1 0 21,12 C21,9 19,7 16,7 C14,7 13,9 14,11 C15,13 13,15 11,15 C9,15 7,17 9,19 M7,10 H7.1 M11,7 H11.1 M17,11 H17.1",
            "Personaliza el color y la lectura. Los cambios se aplican al instante.", 2);
        Add("Options.Tab.Scan", "M11,3 A8,8 0 1 0 11,19 A8,8 0 1 0 11,3 M17,17 L22,22",
            "Motor antivirus, pausa por lotes, barra flotante y exclusiones.", 3, 4, 5, 8);
        Add("Options.Tab.Updates", "M20,11 A8,8 0 0 0 6,6 M6,3 V7 H10 M4,13 A8,8 0 0 0 18,18 M18,21 V17 H14",
            "Opciones del motor ClamAV, firmas y versiones de PalClam.", 6);
        Add("Options.Tab.Protection", "M12,2 L20,6 V12 C20,17 17,20 12,22 C7,20 4,17 4,12 V6 Z M8,12 L11,15 L16,10",
            "Vigilancia local y acceso a los monitores de protección.", 9);
        Add("Options.Tab.Integrations", "M9,2 V7 M15,2 V7 M6,7 H18 V10 C18,14 15,16 12,16 C9,16 6,14 6,10 Z M12,16 V22",
            "Explorador, correo, privacidad y conectores opcionales de IA.", 10, 11);

        ((StackPanel)((TabItem)tabs.Items[0]).Content).Children.Add(BuildDiskLedOptions());

        var protection = (StackPanel)((TabItem)tabs.Items[4]).Content;
        var selfDefenseCard = Card(PurplePale, PurplePale);
        selfDefenseCard.Margin = new Thickness(0, 13, 0, 0);
        var defenseStack = new StackPanel();
        defenseStack.Children.Add(T("Supervisión de PalClam", 15, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 7)));
        defenseStack.Children.Add(T(
            "Consulta el monitor de cierres inesperados. No impide que un malware con privilegios elevados cierre el programa.",
            11.5, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 9)));
        defenseStack.Children.Add(ActionButton("Ver Self-Defense", false, (_, _) => Navigate("selfdefense")));
        selfDefenseCard.Child = defenseStack;
        protection.Children.Add(selfDefenseCard);
        protection.Children.Add(ActionButton("Configurar monitor anti-ransomware", false, (_, _) => Navigate("ransomware")));

        var integrations = (StackPanel)((TabItem)tabs.Items[5]).Content;
        var aiCard = Card(PurplePale, PurplePale);
        aiCard.Margin = new Thickness(0, 13, 0, 0);
        var aiStack = new StackPanel();
        aiStack.Children.Add(T("Inteligencia artificial opcional", 15, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 7)));
        aiStack.Children.Add(T(
            "La configuración de proveedores y claves está en PalClam AI. Las consultas remotas requieren autorización explícita.",
            11.5, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 9)));
        aiStack.Children.Add(ActionButton("Configurar proveedores de IA", false, (_, _) => Navigate("ai")));
        aiCard.Child = aiStack;
        integrations.Children.Add(aiCard);

        // Recurso independiente para técnicos: un enlace discreto, no un módulo
        // antivirus ni una llamada automática a un servicio externo.
        var diagnosticResource = new TextBlock
        {
            Margin = new Thickness(4, 16, 4, 2),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11.5,
            Foreground = Muted
        };
        diagnosticResource.Inlines.Add(new Run("Diagnóstico complementario de Windows y redes: "));
        var modernTerminalLink = new Hyperlink(new Run("Modern Terminal AI ↗"))
        {
            Foreground = Purple,
            ToolTip = "Abrir modernterminal-ai.com en el navegador",
            TextDecorations = System.Windows.TextDecorations.Underline
        };
        modernTerminalLink.Click += (_, _) => OpenAboutWebLink(ModernTerminalAiUrl);
        System.Windows.Automation.AutomationProperties.SetName(modernTerminalLink,
            "Abrir Modern Terminal AI, herramienta independiente de diagnóstico");
        diagnosticResource.Inlines.Add(modernTerminalLink);
        diagnosticResource.Inlines.Add(new Run(" · Herramienta independiente de PalClam."));
        integrations.Children.Add(diagnosticResource);

        tabs.SelectionChanged += (_, e) =>
        {
            // ComboBox internos también emiten SelectionChanged burbujeantes.
            if (ReferenceEquals(e.OriginalSource, tabs)) RefreshOptionsTabTheme();
        };
        tabs.SelectedIndex = 0;
        RefreshOptionsTabTheme();
        page.Children.Add(tabs);
        page.Children.Add(original[12]); // Confirmación siempre disponible al cambiar de pestaña.
    }

    private static UIElement BuildOptionTabHeader(string nameKey, string iconGeometry)
    {
        var path = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse(iconGeometry),
            Fill = Brushes.Transparent,
            StrokeThickness = 1.8,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round
        };
        // El icono adopta el texto del TabItem (normal, activo y hover).
        path.SetBinding(System.Windows.Shapes.Path.StrokeProperty, new Binding("Foreground")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(TabItem), 1)
        });
        var icon = new Border
        {
            Background = PurplePale,
            CornerRadius = new CornerRadius(7),
            Width = 27,
            Height = 27,
            Margin = new Thickness(0, 0, 8, 0),
            Child = new Viewbox
            {
                Width = 17,
                Height = 17,
                Stretch = Stretch.Uniform,
                Child = path
            }
        };
        var title = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 11.5,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.None
        };
        LocalizedUi.Bind(title, TextBlock.TextProperty, nameKey);
        title.SetBinding(TextBlock.ForegroundProperty, new Binding("Foreground")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(TabItem), 1)
        });
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { icon, title }
        };
    }

    // El estilo se crea UNA VEZ al construir las pestañas. Al cambiar el tema
    // solo se reemplazan 3 colores locales por pestaña. No se pierde el foco,
    // el TabItem seleccionado, ni se desmonta el contenido de Opciones.
    private void RefreshOptionsTabTheme()
    {
        if (_optionsTabs is null) return;
        foreach (var tab in _optionsTabs.Items.OfType<TabItem>())
            RefreshSingleOptionsTab(tab);
    }

    private static void RefreshSingleOptionsTab(TabItem tab) =>
        OptionsTabAppearance.Apply(tab, AppearanceCatalog.Find(ActiveTheme));

    private void OpenOptionsTab(int index)
    {
        Navigate("options");
        if (_optionsTabs is not null && index >= 0 && index < _optionsTabs.Items.Count)
            _optionsTabs.SelectedIndex = index;
    }
}
