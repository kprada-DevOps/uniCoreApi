# Plan de Estudios

Módulo backend organizado según los módulos existentes de `UniCore.Api`: controladores, managers, DTOs y requests. La persistencia usa Dapper a través de `DatabaseProvider`; los cambios que afectan varias tablas se ejecutan dentro de una transacción.

## Modelo

La referencia del esquema es `Gestion UniCore/UniCoreBDv1.3.sql`. Las operaciones de malla usan `aca_plan_elementos`, que admite una asignatura o un componente por elemento. `aca_plan_asignaturas` se conserva sin uso desde este módulo para no mezclar el modelo anterior con el aprobado en la fase.

La malla (`aca_asignaturas`, prerrequisitos, componentes, opciones y elementos de plan) permanece separada de requisitos cocurriculares (idioma, rutas, certificaciones y actividades). El seguimiento de estudiantes y sus evidencias conserva referencias históricas; al retirar mecanismos o requisitos con seguimiento, estos se marcan inactivos en vez de eliminarse.

## Rutas principales

Todas las rutas incluyen el tenant como primer segmento, por ejemplo `/{conexion}/PlanEstudios/...`.

- `Asignaturas`: consulta y CRUD, estado y gestión de prerrequisitos.
- `Planes`: consulta y CRUD de planes; `/{cod}/Elementos` y `/{cod}/Requisitos` reemplazan cada conjunto de forma transaccional.
- `Componentes`: CRUD y reemplazo de opciones.
- `RequisitosCocurriculares`: CRUD y configuración transaccional de mecanismos, idiomas, certificaciones, rutas y actividades.
- Catálogos: `Catalogos/{catalogo}` para estados, tipos, comportamientos y unidades de medida.
- Catálogos relacionados: `Idiomas`, `NivelesIdioma`, `RutasIdioma`, `CertificacionesIdioma` y `ActividadesCocurriculares`.
- `Estudiantes/{codEstudiante}/Requisitos`: consulta todos los requisitos activos del plan asociado, incluso si aún no hay seguimiento guardado; PUT registra o actualiza el cumplimiento y POST/DELETE administran evidencias.

Las consultas de detalle cargan conjuntos relacionados por lotes y evitan consultas por cada elemento.

## Autorización y auditoría

Los endpoints usan permisos dinámicos del servidor:

- `PLAN_ESTUDIOS.CONSULTAR`
- `PLAN_ESTUDIOS.ADMINISTRAR`
- `PLAN_ESTUDIOS.CUMPLIMIENTO`

Las operaciones de escritura se registran mediante `AuditoriaManager`. El script `Gestion UniCore/migracion-plan-estudios-permisos.sql` crea el módulo y estos permisos para un tenant existente. La migración no asigna permisos a roles; esa asignación permanece bajo administración de Seguridad.
