using System.Text;
using DocuTrack.Models;

namespace DocuTrack.Views;

/// <summary>
/// Capa de presentación: solo imprime. No decide ni modifica el árbol.
/// </summary>
public sealed class VistaConsola
{
    public VistaConsola()
    {
        // Necesario para que se vean los conectores ├── └── │ en la consola.
        Console.OutputEncoding = Encoding.UTF8;
    }

    // ------------------------------------------------------------ ESTRUCTURA

    public void MostrarEncabezado(string titulo)
    {
        string linea = new('=', 72);
        Console.WriteLine();
        Console.WriteLine(linea);
        Console.WriteLine($" {titulo}");
        Console.WriteLine(linea);
    }

    public void MostrarSubtitulo(string titulo)
    {
        Console.WriteLine();
        Console.WriteLine($"--- {titulo} ---");
    }

    public void MostrarMensaje(string mensaje) => Console.WriteLine($"  {mensaje}");

    // ------------------------------------------------------------ ÁRBOL ASCII

    /// <summary>Imprime el árbol con la raíz arriba y conectores ├── └── │.</summary>
    public void MostrarArbol(Nodo? raiz, string titulo)
    {
        MostrarSubtitulo(titulo);

        if (raiz is null)
        {
            Console.WriteLine("  (árbol vacío)");
            return;
        }

        Console.WriteLine("  Leyenda: [C]=carpeta  [A]=archivo  (I)=hijo izquierdo  (D)=hijo derecho");
        Console.WriteLine($"  {Etiqueta(raiz)}   <- raíz");
        ImprimirHijos(raiz, "  ");
    }

    private static void ImprimirHijos(Nodo nodo, string prefijo)
    {
        var hijos = new List<(string Lado, Nodo Nodo)>();
        if (nodo.Izquierdo is not null) hijos.Add(("I", nodo.Izquierdo));
        if (nodo.Derecho is not null) hijos.Add(("D", nodo.Derecho));

        for (int i = 0; i < hijos.Count; i++)
        {
            bool esUltimo = i == hijos.Count - 1;
            string conector = esUltimo ? "└── " : "├── ";
            string continuacion = esUltimo ? "    " : "│   ";

            Console.WriteLine($"{prefijo}{conector}({hijos[i].Lado}) {Etiqueta(hijos[i].Nodo)}");
            ImprimirHijos(hijos[i].Nodo, prefijo + continuacion);
        }
    }

    private static string Etiqueta(Nodo nodo) =>
        nodo.EsCarpeta ? $"[C] {nodo.Nombre}" : $"[A] {nodo.Nombre}";

    // ------------------------------------------------------------ RESULTADOS

    public void MostrarInsercion(string nombre, bool esCarpeta, ResultadoInsercion resultado)
    {
        string tipo = esCarpeta ? "carpeta" : "archivo";

        if (resultado.Exito)
        {
            Console.WriteLine($"  [OK]         Insertado {tipo} '{nombre}' | comparaciones: {resultado.Comparaciones}");
        }
        else
        {
            string motivo = DescribirMotivo(resultado.Motivo, resultado.NombreRelacionado);
            Console.WriteLine($"  [RECHAZADO]  '{nombre}' ({tipo}): {motivo} | comparaciones: {resultado.Comparaciones}");
        }
    }

    public void MostrarBusqueda(string nombre, string zona, ResultadoBusqueda resultado)
    {
        string veredicto = resultado.Encontrado
            ? $"HALLADO ({Etiqueta(resultado.Nodo!)})"
            : "NO HALLADO";

        Console.WriteLine($"  Buscar '{nombre}' [{zona}] -> {veredicto} | comparaciones: {resultado.Comparaciones}");
    }

    public void MostrarActualizacion(string antiguo, string nuevo, ResultadoActualizacion resultado)
    {
        if (resultado.Exito)
        {
            Console.WriteLine($"  [OK]         Actualizado '{antiguo}' -> '{nuevo}'");
            Console.WriteLine($"               Eliminación: {DescribirCaso(resultado.CasoEliminacion, resultado.NombreRelacionado)}");
            Console.WriteLine($"               Inserción:   {resultado.ComparacionesInsercion} comparaciones");
        }
        else
        {
            string motivo = DescribirMotivo(resultado.Motivo, resultado.NombreRelacionado);
            Console.WriteLine($"  [RECHAZADO]  Actualizar '{antiguo}' -> '{nuevo}': {motivo} (el árbol no cambió)");
        }
    }

    public void MostrarEliminacion(string nombre, ResultadoEliminacion resultado)
    {
        if (resultado.Exito)
        {
            Console.WriteLine($"  [OK]         Eliminado '{nombre}'");
            Console.WriteLine($"               {DescribirCaso(resultado.Caso, resultado.NombreReemplazo)}");
        }
        else
        {
            string motivo = DescribirMotivo(resultado.Motivo, resultado.NombreReemplazo);
            Console.WriteLine($"  [RECHAZADO]  Eliminar '{nombre}': {motivo}");
        }
    }

    public void MostrarRecorrido(string titulo, IReadOnlyList<Nodo> nodos)
    {
        string nombres = nodos.Count == 0
            ? "(vacío)"
            : string.Join(" -> ", nodos.Select(n => n.Nombre));

        Console.WriteLine($"  {titulo,-11}: {nombres}");
    }

    public void MostrarVerificacion(string descripcion, bool cumple)
    {
        string estado = cumple ? "SÍ" : "NO";
        Console.WriteLine($"  {descripcion}: {estado}");
    }

    public void MostrarMetricas(int altura, int nodos, int hojas)
    {
        Console.WriteLine($"  Altura del árbol : {altura} niveles ({Math.Max(altura - 1, 0)} aristas en el camino más largo)");
        Console.WriteLine($"  Total de nodos   : {nodos}");
        Console.WriteLine($"  Nodos hoja       : {hojas}");
    }

    // ------------------------------------------------------------ TEXTOS

    private static string DescribirCaso(CasoEliminacion caso, string? reemplazo) => caso switch
    {
        CasoEliminacion.Hoja =>
            "Caso 1: nodo hoja (se desenlaza del padre)",
        CasoEliminacion.UnHijo =>
            "Caso 2: nodo con un solo hijo (el hijo se reconecta con el padre)",
        CasoEliminacion.DosHijos =>
            $"Caso 3: nodo con dos hijos (lo reemplaza su sucesor inorden '{reemplazo}')",
        _ => "Sin eliminación"
    };

    private static string DescribirMotivo(MotivoRechazo motivo, string? relacionado) => motivo switch
    {
        MotivoRechazo.NombreInvalido =>
            "el nombre es nulo, vacío o solo espacios",
        MotivoRechazo.Duplicado =>
            $"ya existe '{relacionado}' (los nombres se comparan sin distinguir mayúsculas; no se permiten duplicados)",
        MotivoRechazo.PadreEsArchivo =>
            $"quedaría como hijo del archivo '{relacionado}', y un archivo siempre es hoja",
        MotivoRechazo.NoExiste =>
            "el nombre no existe en el árbol",
        MotivoRechazo.SucesorEsArchivo =>
            $"su sucesor '{relacionado}' es un archivo y no puede quedar con dos hijos",
        _ => "motivo desconocido"
    };
}
