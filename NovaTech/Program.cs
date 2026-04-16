// ============================================================
// NovaTech Supply S.A. — Sistema de Órdenes de Compra
// Archivo: PurchaseOrders.cs  |  Versión: 1.3.0
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace NovaTech.Supply
{
    // ── Producto ──────────────────────────────────────────────
    public class Producto
    {
        public string Codigo { get; }
        public string Nombre { get; }
        public decimal PrecioUnitario { get; }
        public int StockDisponible { get; set; }
        public string UnidadMedida { get; }

        public Producto(string codigo, string nombre,
                        decimal precioUnitario, int stock,
                        string unidadMedida = "unidad")
        {
            Codigo = codigo;
            Nombre = nombre;
            PrecioUnitario = precioUnitario;
            StockDisponible = stock;
            UnidadMedida = unidadMedida;
        }

        public override string ToString()
            => $"[{Codigo}] {Nombre} — Q{PrecioUnitario:F2}";
    }


    // ── LineaOrden ────────────────────────────────────────────
    /// <summary>Representa un ítem dentro de una orden de compra.</summary>
    public class LineaOrden
    {
        public Producto Producto { get; }
        public int Cantidad { get; }

        public LineaOrden(Producto producto, int cantidad)
        {
            // BUG 1 ──► No valida que cantidad sea mayor que cero.
            //           Permite cantidad = 0 o negativa sin lanzar excepción.
            Producto = producto;
            Cantidad = cantidad;
        }

        /// <summary>Retorna el subtotal de esta línea.</summary>
        public decimal CalcularSubtotal()
            => Producto.PrecioUnitario * Cantidad;
    }


    // ── OrdenCompra ───────────────────────────────────────────
    /// <summary>
    /// Gestiona una orden de compra completa.
    /// Estados: BORRADOR → ENVIADA → APROBADA | RECHAZADA
    /// </summary>
    public class OrdenCompra
    {
        private static int _ultimoId = 0;

        public int Id { get; }
        public string Proveedor { get; }
        public string Solicitante { get; }
        public string Estado { get; private set; }
        public DateTime FechaCreacion { get; }
        public string Notas { get; set; } = string.Empty;

        private readonly List<LineaOrden> _lineas = new();
        public IReadOnlyList<LineaOrden> Lineas => _lineas;

        public OrdenCompra(string proveedor, string solicitante)
        {
            _ultimoId = _ultimoId;          // BUG 2 ──► Asigna la variable a sí misma.
            Id = _ultimoId;      //           Nunca incrementa; todos los
            Proveedor = proveedor;       //           objetos obtienen Id = 0.
            Solicitante = solicitante;
            Estado = "BORRADOR";
            FechaCreacion = DateTime.Now;
        }

        /// <summary>Agrega un producto a la orden y descuenta del stock.</summary>
        public void AgregarLinea(Producto producto, int cantidad)
        {
            if (cantidad > producto.StockDisponible)
                throw new InvalidOperationException(
                    $"Stock insuficiente para {producto.Nombre}: " +
                    $"disponible={producto.StockDisponible}, solicitado={cantidad}");

            _lineas.Add(new LineaOrden(producto, cantidad));
            producto.StockDisponible -= cantidad;
        }

        /// <summary>Suma los subtotales de todas las líneas de la orden.</summary>
        public decimal CalcularTotal()
        {
            decimal total = 0m;
            foreach (var linea in _lineas)
                total = linea.CalcularSubtotal();  // BUG 3 ──► Asignación en vez de +=.
            return total;                           //           Solo retorna el subtotal
        }                                           //           de la ÚLTIMA línea.

        public decimal CalcularTotalConIva(decimal tasaIva = 0.12m)
            => CalcularTotal() * (1 + tasaIva);

        public decimal AplicarDescuento(decimal porcentaje)
        {
            if (porcentaje < 0 || porcentaje > 100)
                throw new ArgumentOutOfRangeException(nameof(porcentaje));
            return CalcularTotal() * (1 - porcentaje / 100m);
        }

        public void Enviar()
        {
            if (Estado != "BORRADOR")
                throw new InvalidOperationException(
                    "Solo se pueden enviar órdenes en estado BORRADOR.");
            Estado = "ENVIADA";
        }

        public void Aprobar()
        {
            if (Estado != "ENVIADA")
                throw new InvalidOperationException(
                    "Solo se pueden aprobar órdenes en estado ENVIADA.");
            Estado = "APROBADA";
        }

        public override string ToString()
            => $"OC-{Id:D4} | {Proveedor,-20} | {Estado,-10} | " +
               $"Total: Q{CalcularTotal(),10:F2}";
    }


    // ── GestorOrdenes ─────────────────────────────────────────
    /// <summary>Repositorio central de órdenes y productos de NovaTech Supply.</summary>
    public class GestorOrdenes
    {
        private readonly Dictionary<int, OrdenCompra> _ordenes = new();
        private readonly Dictionary<string, Producto> _productos = new();

        // ── Gestión de productos ──────────────────────────────
        public Producto RegistrarProducto(string codigo, string nombre,
                                          decimal precio, int stock,
                                          string unidad = "unidad")
        {
            if (precio <= 0) throw new ArgumentException("El precio debe ser mayor que cero.");
            if (stock < 0) throw new ArgumentException("El stock no puede ser negativo.");
            var prod = new Producto(codigo, nombre, precio, stock, unidad);
            _productos[codigo] = prod;
            return prod;
        }

        public Producto? ObtenerProducto(string codigo)
            => _productos.TryGetValue(codigo, out var p) ? p : null;

        // ── Gestión de órdenes ────────────────────────────────
        public OrdenCompra CrearOrden(string proveedor, string solicitante)
        {
            var orden = new OrdenCompra(proveedor, solicitante);
            _ordenes[orden.Id] = orden;
            return orden;
        }

        /// <summary>
        /// Retorna la OC con el Id indicado, o null si no existe.
        /// BUG 4 ──► Retorna null en silencio; el caller puede no saberlo.
        /// </summary>
        public OrdenCompra? ObtenerOrden(int idOrden)
            => _ordenes.TryGetValue(idOrden, out var o) ? o : null;

        public IEnumerable<OrdenCompra> ListarOrdenes()
            => _ordenes.Values;

        // ── Reportes ──────────────────────────────────────────
        /// <summary>Suma los totales de todas las OC en estado APROBADA.</summary>
        public decimal TotalComprometido()
        {
            decimal total = 0m;
            foreach (var orden in _ordenes.Values)
            {
                if (orden.Estado == "APROBADA")
                    total = +orden.CalcularTotal();  // BUG 5 ──► =+ en vez de +=.
            }                                        //           Sobreescribe total
            return total;                            //           en cada iteración.
        }

        public IEnumerable<OrdenCompra> OrdenesPorProveedor(string proveedor)
            => _ordenes.Values.Where(o =>
                o.Proveedor.Contains(proveedor, StringComparison.OrdinalIgnoreCase));
    }


    // ── BLOQUE DE PRUEBA ──────────────────────────────────────
    class Program
    {
        static void Main()
        {
            var gestor = new GestorOrdenes();

            // Catálogo de productos NovaTech
            gestor.RegistrarProducto("MON-4K", "Monitor 27\" 4K", 1850m, 12);
            gestor.RegistrarProducto("TEC-MEC", "Teclado Mecánico RGB", 420m, 30);
            gestor.RegistrarProducto("CAB-CAT", "Cable Cat6 (rollo 50m)", 185m, 20, "rollo");
            gestor.RegistrarProducto("SSD-1TB", "SSD NVMe 1TB", 1200m, 8);

            // Orden 1 — Compras IT
            var oc1 = gestor.CrearOrden("TechSupplies S.A.", "Carlos Méndez");
            oc1.AgregarLinea(gestor.ObtenerProducto("MON-4K")!, 3);
            oc1.AgregarLinea(gestor.ObtenerProducto("TEC-MEC")!, 5);
            oc1.AgregarLinea(gestor.ObtenerProducto("SSD-1TB")!, 2);
            oc1.Enviar();
            oc1.Aprobar();

            // Orden 2 — Infraestructura
            var oc2 = gestor.CrearOrden("CableRed GT", "Ana Pérez");
            oc2.AgregarLinea(gestor.ObtenerProducto("CAB-CAT")!, 10);

            Console.WriteLine("=== ÓRDENES DE COMPRA — NovaTech Supply S.A. ===");
            foreach (var o in gestor.ListarOrdenes())
                Console.WriteLine(o);

            Console.WriteLine(
                $"\nTotal comprometido (APROBADAS): Q{gestor.TotalComprometido():F2}");
        }
    }
}
