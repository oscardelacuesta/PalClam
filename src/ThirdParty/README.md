# Terceros preparados para PalClam 1.2.2

EMPEZAR_AQUI.bat reúne estos elementos antes de crear los cinco distribuibles:

- `ClamAV/win-x64` y `ClamAV/win-x86`: paquetes oficiales completos de ClamAV 1.5.4, con sus DLL, COPYING.txt, carpeta COPYING y demás documentación original. SHA256 fijado y arquitectura PE comprobada.
- `SourcePackages/clamav-1.5.4.tar.gz`: fuente oficial correspondiente al motor fijado.
- `SourcePackages/Libraries`: fuentes exactas de OpenMcdf 3.4.0 y UTF.Unknown 2.8.0, con commit y SHA256 revisados.
- `Licenses`: avisos, licencias y metadatos NuGet de las once bibliotecas directas/transitivas, además del texto original de Inno Setup.
- `OfflineSignatures`: main, daily y bytecode CVD/CLD preparados con FreshClam y validados con SigTool.

`PrepararLicencias.ps1` contrasta el inventario con los paquetes realmente resueltos y reúne los avisos originales de los runtimes .NET/Windows Desktop autocontenidos en `dist/legal`. La construcción falla ante dependencias no revisadas, textos legales alterados o fuentes obligatorias ausentes. Los archivos se incorporan a instaladores y portables. La primera preparación requiere conexión; el uso de las firmas iniciales en destino puede ser offline.

Mantén licencias, avisos y fuentes al redistribuir. Las firmas deben renovarse: la preparación comprueba las tres familias y su antigüedad. `CHECKSUMS.txt` y `Tools/licenses-lock.json` documentan los archivos fijados. THIRD-PARTY-NOTICES.md, COPYRIGHT.md y docs/TERCEROS-Y-LEGAL.md contienen el detalle de las condiciones de esta entrega.

El código propio 1.2.2 y sus compilables utilizan GPLv3-o-posterior. ClamAV mantiene GPLv2 como programa externo; los textos GPLv2 originales de terceros no se sustituyen. El inventario project_license fija el SHA256 del texto GPLv3 oficial y la generación verifica su inclusión en fuentes, ensamblados y paquetes.
