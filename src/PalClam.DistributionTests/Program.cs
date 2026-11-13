// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using PalClam.Services;

internal static class Program
{
    private static int _checks;
    private static void Check(bool success, string name) { if (!success) throw new Exception(name); _checks++; }
    private static void Main(string[] args)
    {
        if (args.Contains("--probe-storage"))
        { AppPaths.Prepare(); Console.WriteLine(JsonSerializer.Serialize(new { Portable=AppPaths.IsPortable, Root=AppPaths.Root, Temp=AppPaths.PrivateTemp })); return; }
        Check(DistributionPolicy.RuntimeId(Architecture.X86)=="win-x86", "RID x86");
        Check(DistributionPolicy.RuntimeId(Architecture.X64)=="win-x64", "RID x64");
        try { DistributionPolicy.RuntimeId(Architecture.Arm); throw new Exception("ARM no rechazado"); } catch (PlatformNotSupportedException) { _checks++; }
        Check(DistributionPolicy.EngineSuffix("win-x86")=="win32" && DistributionPolicy.EngineSuffix("win-x64")=="x64", "Nombres oficiales de motor");
        Check(DistributionPolicy.EngineSha256("win-x86")!=DistributionPolicy.EngineSha256("win-x64") && DistributionPolicy.EngineSha256("win-x86").Length==64, "Hashes separados");
        Check(ProductLinks.Website=="https://palclam.com/" && ProductLinks.FutureSignedFeed.StartsWith(ProductLinks.Website),"Dominio preparado");
        var temporary=Path.Combine(Path.GetTempPath(),"PalClam-Distribution-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temporary);
        try
        {
            var local=Path.Combine(temporary,"profile");var portable=Path.Combine(temporary,"USB path");Directory.CreateDirectory(portable);
            var install=DistributionPolicy.ResolveStorage(portable,local,false);var usb=DistributionPolicy.ResolveStorage(portable,local,true);
            Check(!install.Portable && install.Root==Path.Combine(local,"PalClam"),"Datos instalados conservan ruta");
            Check(usb.Portable && usb.Root==Path.Combine(portable,"Data") && usb.Root!=install.Root,"Portable no usa perfil instalado");
            Check(!Directory.Exists(install.Root) && !Directory.Exists(usb.Root),"Resolver rutas no crea archivos");
            foreach (var (rid,machine) in new[]{("win-x86",0x14c),("win-x64",0x8664)})
            {
                var file=Path.Combine(temporary,rid+".exe");var bytes=new byte[256];
                bytes[0]=0x4d;bytes[1]=0x5a;BitConverter.GetBytes(128).CopyTo(bytes,0x3c);BitConverter.GetBytes(0x4550).CopyTo(bytes,128);BitConverter.GetBytes((ushort)machine).CopyTo(bytes,132);File.WriteAllBytes(file,bytes);
                Check(DistributionPolicy.MatchesExecutable(file,rid),"PE correcto "+rid);
                Check(!DistributionPolicy.MatchesExecutable(file,rid=="win-x86"?"win-x64":"win-x86"),"PE de otra arquitectura rechazado");
                BitConverter.GetBytes(int.MaxValue).CopyTo(bytes,0x3c);File.WriteAllBytes(file,bytes);
                Check(!DistributionPolicy.MatchesExecutable(file,rid),"Offset PE desbordado rechazado");
            }
            Check(!DistributionPolicy.MatchesExecutable(Path.Combine(temporary,"missing.exe"),"win-x86"),"PE ausente no valida");
            foreach (var withMarker in new[]{false,true})
            {
                var dir=Path.Combine(temporary,withMarker?"portable":"installed");Directory.CreateDirectory(dir);
                var assembly=Assembly.GetExecutingAssembly().Location;var baseName=Path.GetFileNameWithoutExtension(assembly);
                foreach (var file in Directory.GetFiles(Path.GetDirectoryName(assembly)!,baseName+".*")) File.Copy(file,Path.Combine(dir,Path.GetFileName(file)));
                if (withMarker) File.WriteAllText(Path.Combine(dir,DistributionPolicy.Marker),"portable test");
                var host=Environment.ProcessPath!;
                var psi=new ProcessStartInfo(host){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
                if (Path.GetFileNameWithoutExtension(host).Equals("dotnet",StringComparison.OrdinalIgnoreCase)) psi.ArgumentList.Add(Path.Combine(dir,Path.GetFileName(assembly)));
                else
                {
                    // dotnet run puede usar apphost; copiar también ese host a la carpeta de prueba.
                    var apphost=Path.Combine(dir,Path.GetFileName(host));if (!File.Exists(apphost)) File.Copy(host,apphost);
                    if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(apphost,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
                    psi.FileName=apphost;
                }
                psi.ArgumentList.Add("--probe-storage");using var child=Process.Start(psi)!;var output=child.StandardOutput.ReadToEnd();var error=child.StandardError.ReadToEnd();child.WaitForExit();
                Check(child.ExitCode==0,"Prueba de proceso de almacenamiento: "+error);
                using var json=JsonDocument.Parse(output);var obj=json.RootElement;
                Check(obj.GetProperty("Portable").GetBoolean()==withMarker,"Modo por marcador real");
                if (withMarker) Check(obj.GetProperty("Root").GetString()==Path.Combine(dir,"Data") && Directory.Exists(Path.Combine(dir,"Data","Temp")),"Proceso portable escribe junto al ejecutable");
                else Check(!Directory.Exists(Path.Combine(dir,"Data")),"Proceso instalado no crea Data local");
            }
        }
        finally { Directory.Delete(temporary,true); }
        Console.WriteLine($"PASS {_checks} comprobaciones de arquitectura y almacenamiento. Sin ejecutar WPF ni ClamAV.");
    }
}
