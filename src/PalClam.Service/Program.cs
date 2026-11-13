// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.MachineService;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (!OperatingSystem.IsWindows()) return 1;
        if (args.Length == 1 && args[0].Equals("--selftest", StringComparison.OrdinalIgnoreCase))
            return MachineGuardPolicy.SelfTest();
        if (args.Length == 1 && args[0].Equals("--console", StringComparison.OrdinalIgnoreCase))
            return WindowsServiceHost.RunConsole();
        if (args.Length != 0) return 2;
        return WindowsServiceHost.RunService();
    }
}
