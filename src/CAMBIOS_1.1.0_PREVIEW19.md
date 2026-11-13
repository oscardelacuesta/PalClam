# PalClam 1.1.0 Preview 19 — Security Health 360 + USB Watch + Download Guard

## A · Security Health 360

- Pantalla de diagnóstico con ocho componentes: motor, bases de firmas, Guard personal,
  Download Guard, USB Watch, servicio del equipo, inicio con Windows y tareas programadas.
- Estados **OPERATIVO**, **REVISAR**, **NO ACTIVADO** y **NO VERIFICADO**; un componente
  instalado o una preferencia marcada no bastan para declarar cobertura.
- Consulta la antigüedad/fallo de firmas mediante la política ya existente, el estado de
  los observadores en la sesión actual y el latido del servicio experimental.
- Comprobación explícita y copia del informe al portapapeles. No borra ni toca archivos.

## B · USB Watch y Download Guard

- Nueva pantalla accesible desde Protección, con preferencia independiente para activar
  detección de unidades extraíbles mientras la aplicación está abierta.
- Detecta unidades cuya clase Windows es **Removable**, listas y listas para acceder;
  algunas memorias o discos USB son clasificados como *Fixed* y no aparecerán.
- Observación por sondeo cada 5 s, sin WMI ni drivers: la primera lectura es una línea
  base y no provoca análisis inesperados de unidades preexistentes.
- Análisis manual de una unidad seleccionada y análisis automático **optativo y
  desactivado por defecto**, únicamente si el motor está libre y hay firmas; en caso
  contrario se registra una advertencia para análisis posterior manual.
- Download Guard agrega la carpeta Descargas a Guard 2.0, reutilizando su cola,
  estabilidad por archivo, límites, exclusiones, análisis ClamAV y auditoría;
  funciona con la sesión abierta o con PalClam en bandeja. No crea otro motor.
- Registro informativo local separado de eventos USB; el centro de notificaciones
  puede filtrarlos bajo **USB Watch**. Conectar una unidad no equivale a detectar malware.
- Todas las preferencias son voluntarias y se guardan por usuario. Ninguna función
  intercepta o bloquea preventivamente la ejecución.

## Seguridad y límites

- No se ejecutan programas detectados en las unidades, no se aísla ni elimina contenido
  sin autorización. El análisis automático USB no se lanza si hay un análisis ocupado,
  no hay motor o no hay bases de firmas.
- La detección de unidad depende de Windows, del estado listo del medio y del sondeo.
  Un USB insertado y retirado entre dos sondeos puede pasar inadvertido.
- La vigilancia de Descargas necesita firmas, motor, carpeta existente y Guard operativo;
  no implica protección antes de la ejecución de un archivo. Conservar Microsoft Defender.

## Validación

Ejecutar `PRUEBAS.bat` y después `EMPEZAR_AQUI.bat` solo si pasan las cinco fases.
La compilación .NET/WPF y pruebas de hardware Windows deben confirmarse en una máquina
Windows; el entorno de empaquetado no dispone de .NET 10 ni USB físico.
