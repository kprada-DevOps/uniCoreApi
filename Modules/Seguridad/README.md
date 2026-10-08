# Modelo inicial de Seguridad

Este módulo usa proyecciones DTO para Dapper, de acuerdo con la persistencia actual
de `UniCore.Api`. No introduce EF Core, `DbContext`, migraciones ni cambios al SQL.

| Tabla | Proyección |
| --- | --- |
| `seg_modulos` | `ModuloDto` |
| `seg_modulos_configuracion` | `ModuloConfiguracionDto` |
| `seg_menu` | `MenuDto` |
| `seg_usuarios` | `UsuarioSeguridadDto` |
| `seg_roles` | `RolSeguridadDto` |
| `seg_permisos` | `PermisoSeguridadDto` |
| `seg_usuario_roles` | `UsuarioRolDto` |
| `seg_rol_permisos` | `RolPermisoDto` |
| `seg_auditoria` | `AuditoriaDto` |

Las proyecciones conservan nombres de columna para facilitar el mapeo explícito de
Dapper. Las cardinalidades siguen las claves foráneas y restricciones únicas de
`UniCoreBDv1.1.sql`: configuración de módulo uno a uno; menú con módulo y padre
autorreferenciado; usuarios/roles y roles/permisos mediante tablas asociativas.

`UsuarioSeguridadDto` omite `password_hash` intencionalmente. La autenticación
continúa siendo dueña de la lectura y verificación del hash. Las escrituras de
auditoría pasan por `AuditoriaManager`.

La primera entrega administrativa de usuarios se implementa en
`Controllers/UsuariosController.cs` y `Managers/UsuarioSeguridadManager.cs`.
La asignación de roles se guarda en la misma transacción que el alta o la edición;
solo roles activos pueden asignarse, pero una relación existente con un rol inactivo
se conserva si el usuario no la cambia. Las contraseñas se procesan con BCrypt y
nunca forman parte de un DTO de respuesta.

## Administración y autorización

Roles, permisos, módulos, configuración de módulos, menú y auditoría se exponen en
sus propios controllers/managers. Las bajas son lógicas; no se borran relaciones ni
registros con claves foráneas. Las escrituras administrativas pasan por
`AuditoriaManager` y los snapshots no incluyen hashes ni contraseñas.

Las pantallas del frontend usan `PERMISO:<codigo>` para navegación. Los endpoints
administrativos de Seguridad requieren `SEGURIDAD.ADMINISTRAR`; las operaciones de
Estructura Académica requieren permisos distintos para consultar, crear y editar.
La API vuelve a consultar permisos, estado del usuario y privilegio de superadmin en
cada petición; los permisos y roles del JWT no son la fuente de autorización. El
superadmin es una marca protegida de `seg_roles`, no asignable desde el mantenimiento
ordinario de roles, y puede asignarse a usuarios solo desde una operación controlada.

`ObtenerMenuUsuario` devuelve solo menús activos de módulos con permisos efectivos
para el usuario. Para superadmin devuelve los menús activos de todos los módulos
activos y habilitados. En ambos casos respeta `habilitado` y las fechas de
configuración del módulo. Angular toma el menú funcional de este endpoint y ya no
mantiene un menú de respaldo codificado.

Las instalaciones nuevas deben partir de `Gestion UniCore/UniCoreBDv1.2.sql`, que
ya incluye el esquema y los datos iniciales de permisos y menús. Para tenants que ya
tienen una base creada, ejecutar una sola vez
`Gestion UniCore/migracion-autorizacion-dinamica.sql` y
`Gestion UniCore/migracion-estructura-academica.sql`, en ese orden. La primera
añade la marca de superadmin y el acceso inicial de Seguridad; la segunda agrega
los permisos y menús de Estructura Académica. La asignación del rol `SUPERADMIN` a
una cuenta se realiza de forma controlada en la base de datos.

La tabla de auditoría no permite revocar sesiones individuales; la desactivación del
usuario bloquea sus tokens de inmediato, pero un cambio de contraseña no invalida
refresh tokens previos antes de su expiración.
