// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.Services;

internal sealed record MailFinding(string Status, string Path, string Attachment, string Detail, string Subject)
{
    public string DisplayPath => System.IO.Path.GetFileName(Path);
    public string DisplaySubject => string.IsNullOrWhiteSpace(Subject) ? "—" : Subject;
}
