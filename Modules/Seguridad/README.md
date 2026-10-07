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

Los endpoints de configuración requieren `ROL:ADMIN`, resuelto contra las
asignaciones vigentes en MySQL. Las rutas operativas usan políticas `PERMISO:<codigo>`
que vuelven a consultar los permisos activos del usuario y módulo en cada petición;
una revocación tiene efecto sin esperar a que expire el JWT. La API también consulta
el estado actual del usuario por petición. El frontend usa permisos para navegar,
mientras que la API conserva la decisión final.

`ObtenerMenuUsuario` devuelve la jerarquía activa cuando el módulo está activo y
tiene al menos un permiso vigente asignado al usuario. Si existe configuración,
respeta `habilitado` y sus fechas. El esquema permite módulos sin fila de
configuración y la semilla actual no crea esas filas; en ese caso el módulo se
interpreta habilitado por defecto para mantener compatibilidad. La semilla tampoco
crea registros en `seg_menu`, así que Angular conserva el menú base hasta que el
tenant configure entradas dinámicas.

No se modificó el esquema ni los scripts SQL. La tabla de auditoría no permite
revocar sesiones individuales; la desactivación del usuario bloquea sus tokens de
inmediato, pero un cambio de contraseña no invalida refresh tokens previos antes
de su expiración.
