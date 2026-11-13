// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using WpfPath = System.Windows.Shapes.Path;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private Canvas? _headerAuroraLayer;
    private readonly List<(WpfPath shape, TranslateTransform motion,
        double travelX, double driftY, double seconds)> _headerAuroraWaves = new();
    private bool _headerAuroraRunning;

    /// <summary>
    /// Aurora vectorial suave detrás de la marca: cintas animadas, sin interceptar clics.
    /// </summary>
    private Canvas CreateHeaderAurora()
    {
        var layer = new Canvas
        {
            ClipToBounds = true,
            IsHitTestVisible = false,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        _headerAuroraLayer = layer;

        AddHeaderAuroraWave(layer,
            "M -80,72 C 40,18 145,112 280,66 S 520,28 760,70",
            30, -28, -5, 18);
        AddHeaderAuroraWave(layer,
            "M -40,54 C 88,104 180,8 316,56 S 560,108 808,48",
            24, 32, 6, 22);
        AddHeaderAuroraWave(layer,
            "M -105,91 C 20,48 150,128 302,86 S 546,44 784,94",
            22, -22, 5, 26);
        AddHeaderAuroraWave(layer,
            "M -60,37 C 60,8 160,78 282,42 S 506,8 730,39",
            16, 18, -4, 31);

        UpdateHeaderAuroraPalette(AppearanceCatalog.Find(ActiveTheme));
        return layer;
    }

    private void AddHeaderAuroraWave(Canvas layer, string geometry, double thickness,
        double travelX, double driftY, double seconds)
    {
        var motion = new TranslateTransform();
        var shape = new WpfPath
        {
            Data = Geometry.Parse(geometry),
            Stroke = Brushes.Transparent,
            StrokeThickness = thickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            Opacity = 0.52,
            RenderTransform = motion,
            IsHitTestVisible = false,
            Effect = new BlurEffect { Radius = 18, KernelType = KernelType.Gaussian }
        };
        layer.Children.Add(shape);
        _headerAuroraWaves.Add((shape, motion, travelX, driftY, seconds));
    }

    private void UpdateHeaderAuroraPalette(AppearancePalette palette)
    {
        var colors = new[] { palette.Accent, palette.HeroTo, palette.HeroFrom, palette.HeroTile };
        for (var i = 0; i < _headerAuroraWaves.Count; i++)
        {
            var rgb = (Color)System.Windows.Media.ColorConverter.ConvertFromString(colors[i % colors.Length])!;
            var soft = Color.FromArgb(0, rgb.R, rgb.G, rgb.B);
            var medium = Color.FromArgb(82, rgb.R, rgb.G, rgb.B);
            var bright = Color.FromArgb(156, rgb.R, rgb.G, rgb.B);
            _headerAuroraWaves[i].shape.Stroke = new LinearGradientBrush
            {
                StartPoint = new System.Windows.Point(0, 0.5),
                EndPoint = new System.Windows.Point(1, 0.5),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(soft, 0.00),
                    new GradientStop(medium, 0.18),
                    new GradientStop(bright, 0.50),
                    new GradientStop(medium, 0.82),
                    new GradientStop(soft, 1.00)
                }
            };
        }
    }

    private void RefreshHeaderAuroraMotion(bool forceRestart = false)
    {
        if (_headerAuroraLayer is null) return;
        var enabled = _settings.AnimateHeaderAurora && !SystemParameters.HighContrast;
        _headerAuroraLayer.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        var active = enabled && IsVisible && WindowState != System.Windows.WindowState.Minimized;
        if (active == _headerAuroraRunning && !forceRestart) return;
        _headerAuroraRunning = active;
        foreach (var wave in _headerAuroraWaves)
        {
            if (!active)
            {
                wave.motion.BeginAnimation(TranslateTransform.XProperty, null);
                wave.motion.BeginAnimation(TranslateTransform.YProperty, null);
                wave.shape.BeginAnimation(UIElement.OpacityProperty, null);
                wave.motion.X = 0;
                wave.motion.Y = 0;
                wave.shape.Opacity = 0.52;
                continue;
            }

            // Animación siempre en compositor WPF: nada de Timer, polling o trabajo en ClamAV.
            var seconds = AuroraMotionProfiles.DurationSeconds(wave.seconds, _settings.HeaderAuroraSpeed);
            var easing = new SineEase { EasingMode = EasingMode.EaseInOut };
            wave.motion.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation
            {
                From = 0,
                To = wave.travelX * 1.15,
                Duration = TimeSpan.FromSeconds(seconds),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = easing
            });
            wave.motion.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation
            {
                From = 0,
                To = wave.driftY,
                Duration = TimeSpan.FromSeconds(seconds * 0.72),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = easing
            });
            wave.shape.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation
            {
                From = 0.32,
                To = 0.76,
                Duration = TimeSpan.FromSeconds(seconds * 0.55),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = easing
            });
        }
    }
}
