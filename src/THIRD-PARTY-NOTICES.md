# PalClam 1.2.2 — componentes y avisos de terceros

El código propio de PalClam es GPLv3 o posterior (GPL-3.0-or-later); esto no sustituye las licencias de sus dependencias. Las distribuciones conservan los textos íntegros, atribuciones y metadatos originales en `Licenses`. El inventario revisado con versiones y SHA256 es `Tools/licenses-lock.json` en las fuentes y `Licenses/DEPENDENCIES.json` en el producto generado.

| Componente | Versión revisada | Licencia / distribución |
|---|---|---|
| ClamAV, ejecutables oficiales externos | 1.5.4, x64 o win32 | GPLv2 y avisos propios en `ClamAV/COPYING.txt` y `ClamAV/COPYING`; conservar también las condiciones específicas de los componentes incluidos, entre ellos UnRAR. |
| .NET y Windows Desktop runtime | Versión realmente publicada por el SDK .NET 10 | MIT y avisos originales de Microsoft y colaboradores; se extraen de los paquetes runtime usados, no del runtime del equipo del usuario. |
| BouncyCastle.Cryptography | 2.7.0 | MIT, The Legion of the Bouncy Castle Inc. |
| Microsoft.Identity.Client | 4.92.0 | MIT, Microsoft Corporation. |
| Microsoft.IdentityModel.Abstractions | 8.14.0 | MIT, Microsoft Corporation. |
| Microsoft.IO.RecyclableMemoryStream | 4.0.1 | MIT, Microsoft Corporation. |
| Microsoft.Maui.Graphics | 10.0.120 | MIT; incluye THIRD-PARTY-NOTICES original. |
| MimeKit | 4.18.0 | MIT, .NET Foundation y colaboradores. |
| MsgReader | 6.2.1 | MIT, Kees van Spelde / Magic-Sessions y colaboradores; se conservan los avisos originales. |
| OpenMcdf | 3.4.0 | MPL-2.0, Federico Blaseotto, Jeremy Powell y colaboradores. Se adjunta su fuente exacta sin cambios, con sus licencias y avisos originales. Al combinarse con PalClam GPLv3 se ofrece además bajo GPLv3 o posterior mediante la sección 3.3 de MPL-2.0, manteniendo ambas opciones para el destinatario y los avisos particulares de otros archivos. |
| RtfPipe | 2.0.7677.4303 | MIT, Eric Domke y colaboradores. |
| System.Security.Cryptography.Pkcs | 10.0.14 | MIT, .NET Foundation; incluye los avisos originales del paquete y del runtime. |
| UTF.Unknown | 2.8.0 | El proyecto ofrece MPL-1.1 / GPL-2.0-o-posterior / LGPL-2.1-o-posterior. Para esta distribución se selecciona GPL-3.0-o-posterior a partir de la alternativa original GPL-2.0-o-posterior; se conservan los textos de las tres alternativas y su fuente exacta. |
| Inno Setup | Compilador instalado por el desarrollador | Licencia propia de Inno Setup, Jordan Russell / Martijn Laan. Se adjunta el texto original y, si existe junto a ISCC.exe, también el de esa instalación. |

## Fuentes y redistribución

El instalador y cada portable incluyen el ZIP de las fuentes de PalClam en `Source/PalClam_1.2.2_Source.zip`. Incluye scripts de compilación, instalación, inventario de licencias y clave **pública** de actualizaciones; no incluye claves privadas ni datos personales. No basta con entregar un enlace a la web para sustituir las fuentes que deben acompañar esta distribución.

Se incluye `clamav-1.5.4.tar.gz` oficial y verificado (en `Source/ClamAV` en el instalador, directamente en `Source` en la portable), conservando íntegros el paquete binario oficial del motor y sus avisos. OpenMcdf y UTF.Unknown se acompañan en `Source/Libraries` mediante archivos del commit exacto indicado por sus paquetes NuGet, con SHA256 fijado. No se elimina ni modifica la licencia MPL original. El origen de las fuentes y los hashes se detallan en el inventario y en `ThirdParty/CHECKSUMS.txt`.

La generación comprueba las dependencias directas y transitivas resueltas. Una versión nueva o desconocida, un aviso alterado o una fuente obligatoria ausente detienen el empaquetado. Si se sustituye el motor o se cambian sus componentes, deben revisarse de nuevo sus condiciones y fuentes correspondientes antes de distribuir esa nueva combinación. La aceptación `VERIFICACION_LICENCIAS` sigue siendo una comprobación del autor sobre los archivos finales, sin conformidad automática.

## Origen y marcas

- ClamAV: https://github.com/Cisco-Talos/clamav — https://www.clamav.net/
- .NET: https://github.com/dotnet/runtime — https://github.com/dotnet/wpf
- Dependencias NuGet: repositorio y metadatos originales en cada subcarpeta de `Licenses`.
- Inno Setup: https://jrsoftware.org/ — licencia original: https://jrsoftware.org/files/is/license.txt

PalClam utiliza ClamAV como proceso externo. No es un producto oficial de Cisco o Microsoft. No altera los avisos de origen ni reclama sus marcas o su autoría. El monitor de comportamiento de PalClam es reactivo: no bloquea preventivamente procesos ni descifra archivos.
