# Idiomas

Módulo independiente para administrar idiomas, niveles, rutas, asignaturas de cada ruta, certificaciones y niveles certificables. Las tablas propias del módulo usan el prefijo `idi_`. Las asignaturas por nivel se guardan en `idi_idioma_asignatura` y se relacionan con rutas mediante `idi_ruta_idioma_asignaturas`; no se crean en el catálogo académico `aca_asignaturas`.

El nombre visible del nivel se deriva de su orden. `idi_configuracion.formato_nivel` permite escoger numeración romana o arábiga y empieza en romana. Al guardar las asignaturas de una ruta, el sistema exige una fila por cada nivel activo hasta el nivel final y construye sus nombres con el idioma y el nivel configurado.

Todas las rutas usan el tenant como primer segmento: `/{conexion}/Idiomas/...`. Las consultas requieren `IDIOMAS.CONSULTAR` y las escrituras `IDIOMAS.ADMINISTRAR`. La autorización del servidor permite el acceso global del superadministrador.

`Gestion UniCore/migracion-idiomas-permisos.sql` registra el módulo, permisos y entrada de menú. También mantiene las asignaciones de acceso que los roles tenían previamente a través de los permisos de Plan de Estudios.
