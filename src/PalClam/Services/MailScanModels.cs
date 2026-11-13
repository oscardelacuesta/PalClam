// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.Services;

internal sealed record MailScanOptions(bool IncludeSubfolders, bool ExtractAttachments,
    bool IncludeInline, bool PhishingHeuristics, bool CheckLinks,
    bool ScanArchives, bool DetectPua, int MaxMessageMb, int MaxAttachmentMb, int MaxMessages);


internal sealed class MailScanSummary
{
    public int Discovered { get; set; }
    public int Processed { get; set; }
    public int AttachmentsAnalyzed { get; set; }
    public int Detections { get; set; }
    public int Errors { get; set; }
    public int Skipped { get; set; }
    public int LinkWarnings { get; set; }
    public bool Truncated { get; set; }
}

