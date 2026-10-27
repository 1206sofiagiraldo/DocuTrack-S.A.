using DocuTrack.Models;
using DocuTrack.Views;

namespace DocuTrack.Controllers;

/// <summary>
/// Orquesta los casos de uso del taller: llama al Model y pide a la View que muestre los resultados.
/// No contiene lógica estructural del BST (no recorre ni reenlaza nodos).
/// </summary>
public sealed class DocuTrackController
{
    private readonly ArbolBinario _arbol = new();
    private readonly VistaConsola _vista;

    // Orden de inserción: produce raíz con dos subárboles, hojas en ambos lados
    // y nodos con un solo hijo. Los archivos se insertan donde quedan como hojas.
    private static readonly (string Nombre, bool EsCarpeta)[] DatosIniciales =
    {
        ("Documentos", true),
        ("Contratos", true),
        ("Reportes", true),
        ("Balances", true),
        ("Facturas", true),
        ("Usuarios", true),
        ("Datos.csv", false),
        ("Proyectos", true),
        ("Temp", true),
        ("Actas.pdf", false),
        ("Estados", true),
        ("Manuales", true),
        ("Videos.mp4", false),
        ("Informe.docx", false)
    };

    public DocuTrackController() : this(new VistaConsola())
    {
    }

    public DocuTrackController(VistaConsola vista)
    {
        _vista = vista ?? throw new ArgumentNullException(nameof(vista));
    }

    /// <summary>Ejecuta, en orden, todos los casos de uso pedidos en el taller.</summary>
    public void EjecutarTaller()
    {
        ConstruirArbol();
        ProbarInsercionesInvalidas();
        EjecutarBusquedas();
        EjecutarActualizaciones();
        EjecutarEliminaciones();
        MostrarRecorridos();
        MostrarResumenFinal();
    }

    // ------------------------------------------------------ 1. CONSTRUCCIÓN

    private void ConstruirArbol()
    {
        _vista.MostrarEncabezado("1. CONSTRUCCIÓN DEL ÁRBOL (14 inserciones)");

        foreach (var (nombre, esCarpeta) in DatosIniciales)
        {
            ResultadoInsercion resultado = _arbol.Insertar(nombre, esCarpeta);
            _vista.MostrarInsercion(nombre, esCarpeta, resultado);
        }

        _vista.MostrarArbol(_arbol.Raiz, "Árbol inicial");
    }

    private void ProbarInsercionesInvalidas()
    {
        _vista.MostrarEncabezado("1.1 VALIDACIONES DE INSERCIÓN (duplicados y reglas de dominio)");

        var intentos = new (string Nombre, bool EsCarpeta)[]
        {
            ("Contratos", true),   // duplicado exacto
            ("contratos", false),  // duplicado ignorando mayúsculas
            ("Actas2.pdf", false), // quedaría como hijo de un archivo
            ("   ", true)          // nombre vacío
        };

        foreach (var (nombre, esCarpeta) in intentos)
        {
            ResultadoInsercion resultado = _arbol.Insertar(nombre, esCarpeta);
            _vista.MostrarInsercion(nombre, esCarpeta, resultado);
        }

        _vista.MostrarMensaje("El árbol no cambió: todos los intentos fueron rechazados.");
    }

    // ------------------------------------------------------ 2. BÚSQUEDAS

    private void EjecutarBusquedas()
    {
        _vista.MostrarEncabezado("2. BÚSQUEDAS RÁPIDAS (6 búsquedas)");

        var busquedas = new (string Nombre, string Zona)[]
        {
            ("balances", "existente, subárbol izquierdo"),
            ("Actas.pdf", "existente, subárbol izquierdo"),
            ("Videos.mp4", "existente, subárbol derecho"),
            ("Informe.docx", "existente, subárbol derecho"),
            ("Backup.zip", "inexistente"),
            ("Zeta.txt", "inexistente")
        };

        foreach (var (nombre, zona) in busquedas)
        {
            ResultadoBusqueda resultado = _arbol.Buscar(nombre);
            _vista.MostrarBusqueda(nombre, zona, resultado);
        }
    }

    // ------------------------------------------------------ 3. ACTUALIZACIONES

    private void EjecutarActualizaciones()
    {
        _vista.MostrarEncabezado("3. ACTUALIZACIONES SELECTIVAS (Eliminar + Insertar)");

        Actualizar("3.1 Actualizar en HOJA (archivo)", "Datos.csv", "Datos2024.csv");
        Actualizar("3.2 Actualizar en nodo con UN HIJO", "Proyectos", "Proyectos2024");
        Actualizar("3.3 Actualizar en la RAÍZ (dos hijos)", "Documentos", "Repositorio");
    }

    private void Actualizar(string titulo, string antiguo, string nuevo)
    {
        _vista.MostrarSubtitulo(titulo);

        ResultadoActualizacion resultado = _arbol.Actualizar(antiguo, nuevo);
        _vista.MostrarActualizacion(antiguo, nuevo, resultado);
        _vista.MostrarArbol(_arbol.Raiz, $"Árbol tras actualizar '{antiguo}'");
    }

    // ------------------------------------------------------ 4. ELIMINACIONES

    private void EjecutarEliminaciones()
    {
        _vista.MostrarEncabezado("4. ELIMINACIONES SELECTIVAS");

        Eliminar("4.1 Eliminar HOJA", "Videos.mp4");
        Eliminar("4.2 Eliminar nodo con UN HIJO", "Balances");
        Eliminar("4.3 Eliminar la RAÍZ (dos hijos)", _arbol.Raiz?.Nombre ?? string.Empty);
    }

    private void Eliminar(string titulo, string nombre)
    {
        _vista.MostrarSubtitulo(titulo);

        ResultadoEliminacion resultado = _arbol.Eliminar(nombre);
        _vista.MostrarEliminacion(nombre, resultado);
        _vista.MostrarArbol(_arbol.Raiz, $"Árbol tras eliminar '{nombre}'");
    }

    // ------------------------------------------------------ 5. RECORRIDOS

    private void MostrarRecorridos()
    {
        _vista.MostrarEncabezado("5. RECORRIDOS DE VERIFICACIÓN");

        _vista.MostrarRecorrido("Preorden", _arbol.Preorden());
        _vista.MostrarRecorrido("Inorden", _arbol.Inorden());
        _vista.MostrarRecorrido("Postorden", _arbol.Postorden());
        _vista.MostrarRecorrido("Por niveles", _arbol.PorNiveles());

        _vista.MostrarSubtitulo("Verificaciones");
        _vista.MostrarVerificacion("Inorden estrictamente ordenado (el BST sigue siendo válido)", _arbol.InordenEstaOrdenado());
        _vista.MostrarVerificacion("Todos los archivos son hoja", _arbol.ArchivosSonHojas());
    }

    // ------------------------------------------------------ 6. RESUMEN FINAL

    private void MostrarResumenFinal()
    {
        _vista.MostrarEncabezado("6. MÉTRICAS DEL ÁRBOL FINAL");

        _vista.MostrarArbol(_arbol.Raiz, "Árbol final");
        _vista.MostrarSubtitulo("Métricas");
        _vista.MostrarMetricas(_arbol.Altura(), _arbol.ContarNodos(), _arbol.ContarHojas());
    }
}
