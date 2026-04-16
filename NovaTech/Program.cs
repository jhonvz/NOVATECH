// ============================================================
// NovaTech Supply S.A. — Sistema de Órdenes de Compra
// Archivo: PurchaseOrders.cs | Versión: 1.5.0
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

        public Producto(string codigo, string nombre, decimal precioUnitario, int stock, string unidadMedida = "unidad")
        {
            Codigo = codigo;
            Nombre = nombre;
            PrecioUnitario = precioUnitario;
            StockDisponible = stock;
            UnidadMedida = unidadMedida;
        }

        public override string ToString() => $"[{Codigo}] {Nombre} — Q{PrecioUnitario:F2}";
    }

    // ── LineaOrden ────────────────────────────────────────────
    /// <summary>Representa un ítem dentro de una orden de compra.</summary>
    public class LineaOrden
    {
        public Producto Producto { get; }
        public int Cantidad { get; }

        public LineaOrden(Producto producto, int cantidad)
        {
            if (cantidad <= 0)
                throw new ArgumentException("Solo se permite agregar un producto con una cantidad mayor a 0.");

            Producto = producto;
            Cantidad = cantidad;
        }

        /// <summary>Retorna el subtotal de esta línea.</summary>
        public decimal CalcularSubtotal() => Producto.PrecioUnitario * Cantidad;
    }

    // ── OrdenCompra ───────────────────────────────────────────
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
            Id = ++_ultimoId;

            Proveedor = proveedor;
            Solicitante = solicitante;
            Estado = "BORRADOR";
            FechaCreacion = DateTime.Now;
        }

        public void AgregarLinea(Producto producto, int cantidad)
        {
            if (cantidad > producto.StockDisponible)
                throw new InvalidOperationException(
                    $"Stock insuficiente para {producto.Nombre}: disponible={producto.StockDisponible}, solicitado={cantidad}");

            _lineas.Add(new LineaOrden(producto, cantidad));
            producto.StockDisponible -= cantidad;
        }

        public decimal CalcularTotal()
        {
            decimal total = 0m;
            foreach (var linea in _lineas)
                total += linea.CalcularSubtotal();

            return total;
        }

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
                throw new InvalidOperationException("Solo se pueden enviar órdenes en estado BORRADOR.");

            Estado = "ENVIADA";
        }

        public void Aprobar()
        {
            if (Estado != "ENVIADA")
                throw new InvalidOperationException("Solo se pueden aprobar órdenes en estado ENVIADA.");

            Estado = "APROBADA";
        }

        public void Rechazar(string motivo)
        {
            if (Estado != "ENVIADA")
                throw new InvalidOperationException("Solo se pueden rechazar órdenes en estado ENVIADA.");

            if (string.IsNullOrWhiteSpace(motivo))
                throw new ArgumentException("El motivo no puede estar vacío.");

            foreach (var linea in _lineas)
                linea.Producto.StockDisponible += linea.Cantidad;

            Estado = "RECHAZADA";
            Notas = motivo;
        }

        public override string ToString() =>
            $"OC-{Id:D4} | {Proveedor,-20} | {Estado,-10} | Total: Q{CalcularTotal(),10:F2}";
    }

    // ── GestorOrdenes ─────────────────────────────────────────
    public class GestorOrdenes
    {
        private readonly Dictionary<int, OrdenCompra> _ordenes = new();
        private readonly Dictionary<string, Producto> _productos = new();

        public Producto RegistrarProducto(string codigo, string nombre, decimal precio, int stock, string unidad = "unidad")
        {
            if (precio <= 0)
                throw new ArgumentException("El precio debe ser mayor que cero.");

            if (stock < 0)
                throw new ArgumentException("El stock no puede ser negativo.");

            var prod = new Producto(codigo, nombre, precio, stock, unidad);
            _productos[codigo] = prod;
            return prod;
        }

        public Producto? ObtenerProducto(string codigo) =>
            _productos.TryGetValue(codigo, out var p) ? p : null;

        public OrdenCompra CrearOrden(string proveedor, string solicitante)
        {
            var orden = new OrdenCompra(proveedor, solicitante);
            _ordenes[orden.Id] = orden;
            return orden;
        }

        public OrdenCompra ObtenerOrden(int idOrden)
        {
            if (!_ordenes.TryGetValue(idOrden, out var orden))
                throw new ArgumentException("El id indicado no existe.", nameof(idOrden));

            return orden;
        }

        public IEnumerable<OrdenCompra> ListarOrdenes() => _ordenes.Values;

        public decimal TotalComprometido()
        {
            decimal total = 0m;

            foreach (var orden in _ordenes.Values)
            {
                if (orden.Estado == "APROBADA")
                    total += orden.CalcularTotal();
            }

            return total;
        }

        public IEnumerable<OrdenCompra> OrdenesPorProveedor(string proveedor) =>
            _ordenes.Values.Where(o => o.Proveedor.Contains(proveedor, StringComparison.OrdinalIgnoreCase));

        public IEnumerable<OrdenCompra> GenerarReporte() => _ordenes.Values;

        public IEnumerable<OrdenCompra> FiltrarPorEstado(string estado) =>
            _ordenes.Values.Where(o => o.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase));

        public IEnumerable<OrdenCompra> BuscarPorProveedor(string nombre) =>
            _ordenes.Values.Where(o => o.Proveedor.Contains(nombre, StringComparison.OrdinalIgnoreCase));
    }

    // ── BLOQUE DE PRUEBA ──────────────────────────────────────
    class Program
    {
        static void Main()
        {
            var gestor = new GestorOrdenes();

            gestor.RegistrarProducto("MON-4K", "Monitor 27\" 4K", 1850m, 12);
            gestor.RegistrarProducto("TEC-MEC", "Teclado Mecánico RGB", 420m, 30);
            gestor.RegistrarProducto("CAB-CAT", "Cable Cat6 (rollo 50m)", 185m, 20, "rollo");
            gestor.RegistrarProducto("SSD-1TB", "SSD NVMe 1TB", 1200m, 8);

            var oc1 = gestor.CrearOrden("TechSupplies S.A.", "Carlos Méndez");
            oc1.AgregarLinea(gestor.ObtenerProducto("MON-4K")!, 3);
            oc1.AgregarLinea(gestor.ObtenerProducto("TEC-MEC")!, 5);
            oc1.AgregarLinea(gestor.ObtenerProducto("SSD-1TB")!, 2);
            oc1.Enviar();
            oc1.Aprobar();

            var oc2 = gestor.CrearOrden("CableRed GT", "Ana Pérez");
            oc2.AgregarLinea(gestor.ObtenerProducto("CAB-CAT")!, 10);

            var reporte = gestor.GenerarReporte();

            foreach (var o in reporte)
            {
                Console.WriteLine($"OC-{o.Id:D4}");
                Console.WriteLine($"Proveedor: {o.Proveedor}");
                Console.WriteLine($"Solicitante: {o.Solicitante}");
                Console.WriteLine($"Fecha: {o.FechaCreacion}");
                Console.WriteLine($"Estado: {o.Estado}");
                Console.WriteLine($"Líneas: {o.Lineas.Count}");
                Console.WriteLine($"Total con IVA: Q{o.CalcularTotalConIva():F2}");
                Console.WriteLine($"Total sin IVA: Q{o.CalcularTotal():F2}");
                Console.WriteLine("-----------------------------------");
            }

            Console.WriteLine("=== ÓRDENES DE COMPRA — NovaTech Supply S.A. ===");

            foreach (var o in gestor.ListarOrdenes())
                Console.WriteLine(o);

            Console.WriteLine($"\nTotal comprometido (APROBADAS): Q{gestor.TotalComprometido():F2}");
        }
    }
}