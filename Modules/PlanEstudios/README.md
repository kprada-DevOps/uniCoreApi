# Plan de Estudios

El módulo administra asignaturas, planes, componentes curriculares, actividades cocurriculares y requisitos de plan. Sus operaciones usan las tablas `aca_*` definidas en `Gestion UniCore/UniCoreBDv1.3.sql`.

## Modelo

Las operaciones de malla usan `aca_plan_elementos`, que permite registrar una asignatura o un componente por elemento. `aca_plan_asignaturas` se conserva sin uso desde este módulo para evitar mezclar el modelo anterior con el vigente.

Los requisitos cocurriculares pueden referenciar idiomas, certificaciones y rutas. Sus catálogos se administran ahora desde el módulo independiente `Idiomas`; Plan de Estudios conserva la configuración de esos requisitos y las actividades cocurriculares. El seguimiento de estudiantes y sus evidencias conserva referencias históricas; cuando hay seguimiento, los requisitos relacionados se inactivan en vez de eliminarse.

## Rutas principales

Todas las rutas incluyen el tenant como primer segmento, por ejemplo `/{conexion}/PlanEstudios/...`.

- `Asignaturas`: consulta y CRUD, estado y gestión de prerrequisitos.
- `Planes`: consulta y CRUD de planes; `/{cod}/Elementos` y `/{cod}/Requisitos` reemplazan cada conjunto de forma transaccional.
- `Componentes`: CRUD y reemplazo de opciones.
- `RequisitosCocurriculares`: CRUD y configuración transaccional de mecanismos, referencias a idiomas y actividades.
- Catálogos: `Catalogos/{catalogo}` para estados, tipos, comportamientos y unidades de medida.
- Catálogos relacionados: `ActividadesCocurriculares`.
- `Estudiantes/{codEstudiante}/Requisitos`: consulta los requisitos activos del plan y permite registrar cumplimiento y evidencias.

Los catálogos de idiomas se consultan en `/{conexion}/Idiomas/...` con permisos `IDIOMAS.CONSULTAR` o `IDIOMAS.ADMINISTRAR`.

## Autorización y auditoría

Los endpoints de Plan de Estudios usan permisos dinámicos del servidor: `PLAN_ESTUDIOS.CONSULTAR`, `PLAN_ESTUDIOS.ADMINISTRAR` y `PLAN_ESTUDIOS.CUMPLIMIENTO`. Las operaciones de escritura se registran mediante `AuditoriaManager`. El superadministrador conserva acceso global mediante la autorización del servidor.

La migración `Gestion UniCore/migracion-plan-estudios-permisos.sql` crea el módulo y sus permisos. La migración `Gestion UniCore/migracion-idiomas-permisos.sql` crea el módulo Idiomas y sus permisos, traslada acceso de roles existente y registra su opción de menú.
