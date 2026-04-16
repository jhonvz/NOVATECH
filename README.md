# Sistema de Órdenes de Compra — NovaTech Supply

## 1. Descripción de funcionalidad

El sistema permite gestionar órdenes de compra internas dentro de la empresa **NovaTech Supply S.A.**.  

Incluye funcionalidades para:

- Registro de productos
- Creación de órdenes de compra
- Agregar productos a una orden
- Gestión de estados:
  - BORRADOR
  - ENVIADA
  - APROBADA
  - RECHAZADA
- Cálculo de totales (con y sin IVA)
- Generación de reportes
- Filtrado y búsqueda de órdenes

Además, el sistema gestiona inventario:
- Descuenta stock al agregar productos
- Restituye stock al rechazar una orden

---

## 2. Descripción técnica

El sistema está desarrollado en **C#** utilizando Programación Orientada a Objetos (POO).

### Componentes principales

- **Producto** → Representa un producto del catálogo
- **LineaOrden** → Representa una línea dentro de una orden
- **OrdenCompra** → Entidad principal del negocio
- **GestorOrdenes** → Administra órdenes y productos

### Estructuras utilizadas

- `List<T>` → almacenamiento de líneas
- `Dictionary<K,V>` → almacenamiento de órdenes y productos
- LINQ → filtrado y búsqueda

---

## 3. Consideraciones y restricciones

- No se permiten cantidades ≤ 0
- No se permite aprobar o rechazar órdenes fuera del flujo definido
- El stock se descuenta al agregar productos
- El stock se restituye al rechazar una orden
- Los IDs de órdenes son autoincrementales
- El sistema funciona en memoria (no hay persistencia)

---

## 4. Tabla de clases / objetos

| Clase          |            Responsabilidad            |
|----------------|---------------------------------------|
| Producto       | Representa productos y su stock       |
| LineaOrden     | Relaciona producto con cantidad       |
| OrdenCompra    | Maneja lógica de negocio de una orden |
| GestorOrdenes  | Administra órdenes y productos        |

---

## 5. Flujo del proceso

1. Crear Orden → **BORRADOR**
2. Agregar productos (descuenta stock)
3. Enviar → **ENVIADA**
4. Resultado:
   - Aprobar → **APROBADA**
   - Rechazar → **RECHAZADA** (restituye stock)
---

## 6. Código y comentarios

El sistema incluye:

- Comentarios XML (`<summary>`, `<param>`, `<returns>`)
- Comentarios en código para lógica crítica
- Sección CHANGELOG documentada

Esto facilita:
- Mantenimiento
- Lectura del código
- Uso con IntelliSense

---

## 7. Gestión de versiones

|Versión|            Descripción          |
|-------|---------------------------------|
| 1.3.0 | Versión inicial con errores     |
| 1.4.0 | Corrección de bugs críticos     |
| 1.5.0 | Implementacion de metodos nuevos|
| 1.6.0 | Documentación y mejoras         |

---

## 8. Casos de prueba

### Caso 1 — Orden aprobada
- Crear orden
- Agregar productos
- Enviar
- Aprobar
- Validar total correcto

---

### Caso 2 — Orden rechazada
- Crear orden
- Agregar productos
- Enviar
- Rechazar
- Validar restitución de stock

---

### Caso 3 — Validación de cantidad
- Intentar agregar cantidad 0 o negativa
- Esperar excepción

---

### Caso 4 — Reporte
- Crear múltiples órdenes
- Generar reporte
- Validar datos

---

### Caso 5 — Filtro y búsqueda
- Filtrar por estado
- Buscar por proveedor

---

## 9. Ejecución

El sistema puede ejecutarse directamente desde el archivo `Program.cs`.
