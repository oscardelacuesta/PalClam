// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;
using System.Windows.Documents;
using System.Windows.Media;
using PalClam.Localization;

namespace PalClam.UI;

// Adaptadores de presentación. Nunca cambian Content/ItemsSource/SelectedItem de
// los selectores ni las cadenas de los modelos que utiliza la lógica antivirus.
internal static class PresentationLocalization
{
    private sealed class TextState
    {
        internal string Original = "";
        internal string Rendered = "";
        internal bool Updating;
        internal bool Listening;
    }

    private static readonly ConditionalWeakTable<TextBlock, TextState> States = new();
    private static readonly List<WeakReference<TextBlock>> Tracked = new();
    private static readonly DependencyPropertyDescriptor Descriptor =
        DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
    private static bool _installed;
    private static Dispatcher? _dispatcher;

    internal static readonly DependencyProperty PreserveDataProperty = DependencyProperty.RegisterAttached(
        "PreserveData", typeof(bool), typeof(PresentationLocalization), new PropertyMetadata(false));

    internal static void Install(Application app)
    {
        if (_installed) return;
        _installed = true;
        _dispatcher = app.Dispatcher;
        EventManager.RegisterClassHandler(typeof(FrameworkElement), FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnLoaded));
        UiText.Current.PropertyChanged += OnLanguageChanged;
    }

    private static void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(UiText.LanguageId) || _dispatcher is null) return;
        if (!_dispatcher.CheckAccess()) { _dispatcher.BeginInvoke(new Action(Refresh)); return; }
        Refresh();
    }

    private static void Refresh()
    {
        for (var i = Tracked.Count - 1; i >= 0; i--)
        {
            if (!Tracked[i].TryGetTarget(out var text)) { Tracked.RemoveAt(i); continue; }
            Update(text);
        }
    }

    internal static TextBlock Track(TextBlock text)
    {
        TranslateComboBinding(text);
        if (text.Inlines.Count > 1 || text.Inlines.Any(x => x is not Run))
        {
            TranslateInlines(text.Inlines);
            return text;
        }
        if ((bool)text.GetValue(PreserveDataProperty) ||
            BindingOperations.IsDataBound(text, TextBlock.TextProperty)) return text;
        if (!States.TryGetValue(text, out var state))
        {
            state = new TextState { Original = text.Text, Rendered = text.Text };
            States.Add(text, state);
            Tracked.Add(new WeakReference<TextBlock>(text));
            if (Tracked.Count >= 1024) Tracked.RemoveAll(reference => !reference.TryGetTarget(out _));
            text.Unloaded += OnUnloaded;
        }
        if (text.IsLoaded && !state.Listening)
        {
            Descriptor.AddValueChanged(text, OnTextChanged);
            state.Listening = true;
        }
        Update(text);
        return text;
    }

    private static void TranslateComboBinding(TextBlock text)
    {
        if (BindingOperations.GetBinding(text, TextBlock.TextProperty) is not Binding binding ||
            binding.Converter is not null || ReferenceEquals(binding.Source, UiText.Current)) return;
        DependencyObject? current = text;
        ComboBox? combo = null;
        var label = false;
        while (current is not null)
        {
            if (current is TextBox or PasswordBox or System.Windows.Controls.ListViewItem) return;
            if (current is ComboBox candidate) { combo = candidate; break; }
            if (current is ComboBoxItem item && ItemsControl.ItemsControlFromItemContainer(item) is ComboBox owner)
            { combo = owner; break; }
            if (current is GridViewColumnHeader or System.Windows.Controls.Primitives.ButtonBase or
                System.Windows.Controls.Label or System.Windows.Controls.MenuItem or System.Windows.Controls.ToolTip)
            { label = true; break; }
            current = VisualTreeHelper.GetParent(current);
        }
        if (!label && (combo is null || combo.IsEditable)) return;
        // Solo se adapta el enlace visual generado por el selector. DisplayMemberPath,
        // ItemTemplate, ItemsSource y SelectedItem no se cambian, ni temporalmente.
        var translated = new MultiBinding { Mode = BindingMode.OneWay, Converter = ValueConverter.Instance };
        translated.Bindings.Add(binding);
        translated.Bindings.Add(LanguageBinding());
        text.SetBinding(TextBlock.TextProperty, translated);
    }

    private static void TranslateInlines(InlineCollection inlines)
    {
        foreach (var inline in inlines)
        {
            if (inline is Span span) TranslateInlines(span.Inlines);
            if (inline is Run run && !BindingOperations.IsDataBound(run, Run.TextProperty))
            {
                var binding = new MultiBinding { Mode = BindingMode.OneWay, Converter = ValueConverter.Instance };
                binding.Bindings.Add(new Binding { Source = run.Text });
                binding.Bindings.Add(LanguageBinding());
                BindingOperations.SetBinding(run, Run.TextProperty, binding);
            }
        }
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBlock text && States.TryGetValue(text, out var state) && state.Listening)
        {
            Descriptor.RemoveValueChanged(text, OnTextChanged);
            state.Listening = false;
        }
    }

    private static void OnTextChanged(object? sender, EventArgs e)
    {
        if (sender is TextBlock text) Update(text);
    }

    private static void Update(TextBlock text)
    {
        if (!States.TryGetValue(text, out var state) || state.Updating ||
            (bool)text.GetValue(PreserveDataProperty) || BindingOperations.IsDataBound(text, TextBlock.TextProperty)) return;
        if (text.Text != state.Rendered) state.Original = text.Text;
        var translated = DisplayText.Translate(state.Original);
        state.Rendered = translated;
        if (text.Text == translated) return;
        state.Updating = true;
        try { text.SetCurrentValue(TextBlock.TextProperty, translated); }
        finally { state.Updating = false; }
    }

    internal static string Original(TextBlock text) =>
        States.TryGetValue(text, out var state) && text.Text == state.Rendered ? state.Original : text.Text;

    internal static void Append(TextBlock text, string suffix)
    {
        text.Text = Original(text) + suffix;
        Track(text);
    }

    internal static void SetReport(TextBox text, string original)
    {
        var report = new MultiBinding { Mode = BindingMode.OneWay, Converter = ValueConverter.Instance };
        report.Bindings.Add(new Binding { Source = original });
        report.Bindings.Add(LanguageBinding());
        text.SetBinding(TextBox.TextProperty, report);
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBlock text) { Track(text); return; }
        if (sender is Window window)
        {
            var title = new MultiBinding { Mode = BindingMode.OneWay, Converter = ValueConverter.Instance };
            title.Bindings.Add(new Binding { Source = window.Title });
            title.Bindings.Add(LanguageBinding());
            window.SetBinding(Window.TitleProperty, title);
        }
        if (sender is ComboBox) return;
        if (sender is System.Windows.Controls.Primitives.ButtonBase or System.Windows.Controls.Label or System.Windows.Controls.ToolTip)
        {
            var content = (ContentControl)sender;
            if (content.ContentTemplate is null && content.Content is string)
                content.ContentTemplate = Template();
        }
        if (sender is System.Windows.Controls.MenuItem menu && menu.HeaderTemplate is null && menu.Header is string)
            menu.HeaderTemplate = Template();
        if (sender is Expander expander && expander.HeaderTemplate is null && expander.Header is string)
            expander.HeaderTemplate = Template();
        if (sender is TabItem tab && tab.HeaderTemplate is null && tab.Header is string)
            tab.HeaderTemplate = Template();
        if (sender is ListView list && list.View is GridView grid)
        {
            foreach (var column in grid.Columns)
            {
                if (column.HeaderTemplate is null && column.Header is string) column.HeaderTemplate = Template();
                if (column.DisplayMemberBinding is Binding binding && binding.Converter is null &&
                    IsDisplayField(binding.Path?.Path))
                {
                    var translated = new MultiBinding { Mode = BindingMode.OneWay, Converter = ValueConverter.Instance, ConverterParameter = binding.Path?.Path };
                    translated.Bindings.Add(new Binding());
                    translated.Bindings.Add(LanguageBinding());
                    column.DisplayMemberBinding = translated;
                }
            }
        }
    }

    private static bool IsDisplayField(string? path) => path is
        "Outcome" or "Status" or "StatusText" or "DisplayStatus" or "DisplayLevel" or "ReviewStatus" or
        "Review" or "Verdict" or "Detail" or "Description" or "Reason" or "Action" or "Kind" or "Level" or
        "State" or "Component" or "DisplayOutcome" or "DisplayDetail" or "DisplaySource" or "RecheckLabel" or "ReviewLabel";

    private static Binding LanguageBinding() => new(nameof(UiText.LanguageId)) { Source = UiText.Current };

    private static DataTemplate Template(string path = "")
    {
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        var values = new MultiBinding { Mode = BindingMode.OneWay, Converter = ValueConverter.Instance, ConverterParameter = path };
        values.Bindings.Add(new Binding());
        values.Bindings.Add(LanguageBinding());
        presenter.SetBinding(ContentPresenter.ContentProperty, values);
        return new DataTemplate { VisualTree = presenter };
    }

    private sealed class ValueConverter : IMultiValueConverter
    {
        internal static ValueConverter Instance { get; } = new();
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var value = values[0];
            if (value is not null && parameter is "DisplayDetail" &&
                value.GetType().GetProperty("Detail")?.GetValue(value) is string detail &&
                value.GetType().GetProperty("FilePath")?.GetValue(value) is string filePath)
                return filePath.Length == 0 ? DisplayText.Translate(detail) :
                    filePath + " · " + DisplayText.Translate(detail);
            if (value is UiLanguage language)
                return language.Id == TextCatalog.AutomaticLanguage ? UiText.Current["Language.Automatic"] : language.NativeName;
            if (parameter is string { Length: > 0 } path && value is not null && value != DependencyProperty.UnsetValue)
                value = value.GetType().GetProperty(path)?.GetValue(value) ?? value;
            return value is string text ? DisplayText.Translate(text) : value ?? "";
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
