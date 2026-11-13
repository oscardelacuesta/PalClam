// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System;
using System.Collections.Generic;

namespace PalClam.Services;

/// <summary>
/// Límite local de avisos de bandeja. Nunca suprime registros ni detecciones:
/// solamente evita inundar la sesión con globos repetidos.
/// Sin acceso a WPF, al servicio de Windows o a la red.
/// </summary>
internal enum GuardNoticeKind { Threat, Warning, Error }

internal sealed class GuardNoticeGate
{
    private readonly Dictionary<string, DateTimeOffset> _recentThreats =
        new(StringComparer.OrdinalIgnoreCase);
    private DateTimeOffset _warningTime = DateTimeOffset.MinValue;
    private DateTimeOffset _errorTime = DateTimeOffset.MinValue;
    private const int MaxThreatKeys = 256;

    public bool ShouldShow(GuardNoticeKind kind, string? key, DateTimeOffset now)
    {
        if (kind == GuardNoticeKind.Threat)
        {
            key = string.IsNullOrWhiteSpace(key) ? "amenaza-sin-ruta" : key.Trim();
            // Mismo archivo: una notificación cada dos minutos; archivos distintos sí avisan.
            if (_recentThreats.TryGetValue(key, out var last) &&
                now >= last && now - last < TimeSpan.FromMinutes(2)) return false;
            if (_recentThreats.Count >= MaxThreatKeys)
            {
                foreach (var item in new List<string>(_recentThreats.Keys))
                    if (now >= _recentThreats[item] && now - _recentThreats[item] >= TimeSpan.FromMinutes(10))
                        _recentThreats.Remove(item);
                if (_recentThreats.Count >= MaxThreatKeys) _recentThreats.Clear();
            }
            _recentThreats[key] = now;
            return true;
        }

        var lastNotice = kind == GuardNoticeKind.Warning ? _warningTime : _errorTime;
        if (now >= lastNotice && now - lastNotice < TimeSpan.FromMinutes(5)) return false;
        if (kind == GuardNoticeKind.Warning) _warningTime = now;
        else _errorTime = now;
        return true;
    }
}
