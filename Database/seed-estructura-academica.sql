-- Semilla idempotente para habilitar el menú del módulo Estructura Académica.
-- Ejecutar una vez por tenant después de desplegar la API.

INSERT INTO seg_permisos (cod_modulo, codigo, nombre, accion, activo, descripcion)
SELECT m.cod, 'ESTRUCTURA_ACADEMICA.CONSULTAR', 'Consultar estructura académica', 'CONSULTAR', 1,
       'Habilita el acceso al módulo de estructura académica.'
FROM seg_modulos m
WHERE m.codigo = 'ESTRUCTURA_ACADEMICA'
ON DUPLICATE KEY UPDATE cod = LAST_INSERT_ID(cod);

INSERT INTO seg_menu (cod_modulo, cod_padre, codigo, nombre, descripcion, icono, ruta, orden, tipo, activo)
SELECT m.cod, NULL, 'ESTRUCTURA_ACADEMICA', 'Estructura Académica', NULL, 'pi pi-sitemap', NULL, 25, 'GRUPO', 1
FROM seg_modulos m
WHERE m.codigo = 'ESTRUCTURA_ACADEMICA'
  AND NOT EXISTS (SELECT 1 FROM seg_menu x WHERE x.codigo = 'ESTRUCTURA_ACADEMICA');

SET @menu_estructura_academica = (
    SELECT cod FROM seg_menu WHERE codigo = 'ESTRUCTURA_ACADEMICA' LIMIT 1
);

INSERT INTO seg_menu (cod_modulo, cod_padre, codigo, nombre, descripcion, icono, ruta, orden, tipo, activo)
SELECT m.cod, @menu_estructura_academica, items.codigo, items.nombre, NULL, items.icono, items.ruta, items.orden, 'ITEM', 1
FROM seg_modulos m
CROSS JOIN (
    SELECT 'EA_SEDES' AS codigo, 'Sedes' AS nombre, 'pi pi-map-marker' AS icono, '/estructura-academica/sedes' AS ruta, 1 AS orden
    UNION ALL SELECT 'EA_FACULTADES', 'Facultades', 'pi pi-building', '/estructura-academica/facultades', 2
    UNION ALL SELECT 'EA_DEPARTAMENTOS', 'Departamentos', 'pi pi-sitemap', '/estructura-academica/departamentos', 3
    UNION ALL SELECT 'EA_PROGRAMAS', 'Programas', 'pi pi-book', '/estructura-academica/programas', 4
    UNION ALL SELECT 'EA_AULAS', 'Aulas', 'pi pi-home', '/estructura-academica/aulas', 5
    UNION ALL SELECT 'EA_PROGRAMA_NIVEL', 'Niveles de programa', 'pi pi-list', '/estructura-academica/programa-nivel', 6
    UNION ALL SELECT 'EA_PROGRAMA_MODALIDAD', 'Modalidades de programa', 'pi pi-sliders-h', '/estructura-academica/programa-modalidad', 7
    UNION ALL SELECT 'EA_TIPO_PERIODO', 'Tipos de período', 'pi pi-calendar', '/estructura-academica/tipo-periodo', 8
) items
WHERE m.codigo = 'ESTRUCTURA_ACADEMICA'
  AND NOT EXISTS (SELECT 1 FROM seg_menu x WHERE x.codigo = items.codigo);

INSERT INTO seg_rol_permisos (cod_rol, cod_permiso)
SELECT r.cod, p.cod
FROM seg_roles r
JOIN seg_permisos p ON p.codigo = 'ESTRUCTURA_ACADEMICA.CONSULTAR'
WHERE r.codigo = 'ADMIN'
ON DUPLICATE KEY UPDATE cod = cod;
