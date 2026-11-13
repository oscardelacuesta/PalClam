# PalClam 1.2.1

Ediciones x64 y x86, instalación automática según arquitectura y ZIP portables autocontenidos. EMPEZAR_AQUI.bat sigue siendo la entrada principal y genera los cinco archivos de distribución en Windows.

Cada edición utiliza ClamAV de su arquitectura, con SHA256 oficial fijado, comprobación PE y actualizaciones separadas. La portable se identifica con palclam.portable, guarda datos propios en Data junto al programa y bloquea servicios, tareas, inicio automático y registro de Explorer. Las credenciales IA siguen cifradas con DPAPI y vinculadas al usuario Windows; no se hacen portables en texto claro. El instalador conserva AppId y la edición instalada conserva la ruta de configuración existente.

palclam.com se incorpora como web del producto, con créditos y enlaces del autor a palentino.es. El dominio nuevo no recibe consultas automáticas de actualización hasta que el autor publique un canal firmado; se conserva la verificación criptográfica y el canal anterior.

No cambia la naturaleza reactiva de Guard y del monitor anti-ransomware. La aceptación real en Windows, incluyendo arquitectura x86, portables, Outlook e instalación, debe documentarse en ACEPTACION_1.2.1.ini.

Se corrige el inventario de licencias: once paquetes NuGet, avisos del runtime real, Inno Setup, MPL-2.0 de OpenMcdf y alternativa GPL de UTF.Unknown. Los instaladores y portables reciben licencias, atribuciones, fuentes correspondientes y el inventario con hashes. La generación rechaza bibliotecas no revisadas, documentos alterados o fuentes ausentes. Se añaden pruebas aisladas de empaquetado y privacidad.
