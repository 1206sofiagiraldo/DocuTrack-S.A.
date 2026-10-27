namespace DocuTrack.Models;

/// <summary>
/// Árbol binario de búsqueda (BST simple, sin balanceo) que modela el repositorio de documentos.
/// Clave: Nombre, comparación ordinal sin distinguir mayúsculas. Sin duplicados.
/// Esta clase no conoce la consola: solo devuelve resultados que la View sabe presentar.
/// </summary>
public sealed class ArbolBinario
{
    public Nodo? Raiz { get; private set; }

    public bool EstaVacio => Raiz is null;

    /// <summary>Comparación de claves: ordinal, ignorando mayúsculas/minúsculas.</summary>
    public static int Comparar(string a, string b) =>
        string.Compare(a, b, StringComparison.OrdinalIgnoreCase);

    // ------------------------------------------------------------------ CREATE

    /// <summary>
    /// Inserta un nodo respetando la regla del BST (menor a la izquierda, mayor a la derecha).
    /// Rechaza nombres vacíos, duplicados y posiciones que dejarían a un archivo con hijos.
    /// Cuenta una comparación por cada nodo contra el que se compara la clave.
    /// </summary>
    public ResultadoInsercion Insertar(string nombre, bool esCarpeta)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return new ResultadoInsercion(false, MotivoRechazo.NombreInvalido, 0);

        string clave = nombre.Trim();

        if (Raiz is null)
        {
            Raiz = new Nodo(clave, esCarpeta);
            return new ResultadoInsercion(true, MotivoRechazo.Ninguno, 0);
        }

        Nodo actual = Raiz;
        int comparaciones = 0;

        while (true)
        {
            comparaciones++;
            int cmp = Comparar(clave, actual.Nombre);

            if (cmp == 0)
                return new ResultadoInsercion(false, MotivoRechazo.Duplicado, comparaciones, actual.Nombre);

            // Un archivo es siempre hoja: no puede recibir hijos.
            if (actual.EsArchivo)
                return new ResultadoInsercion(false, MotivoRechazo.PadreEsArchivo, comparaciones, actual.Nombre);

            Nodo? siguiente = cmp < 0 ? actual.Izquierdo : actual.Derecho;

            if (siguiente is null)
            {
                var nuevo = new Nodo(clave, esCarpeta);
                if (cmp < 0) actual.Izquierdo = nuevo;
                else actual.Derecho = nuevo;

                return new ResultadoInsercion(true, MotivoRechazo.Ninguno, comparaciones);
            }

            actual = siguiente;
        }
    }

    // -------------------------------------------------------------------- READ

    /// <summary>Busca por nombre y cuenta cuántos nodos se compararon.</summary>
    public ResultadoBusqueda Buscar(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return new ResultadoBusqueda(false, null, 0);

        string clave = nombre.Trim();
        Nodo? actual = Raiz;
        int comparaciones = 0;

        while (actual is not null)
        {
            comparaciones++;
            int cmp = Comparar(clave, actual.Nombre);

            if (cmp == 0)
                return new ResultadoBusqueda(true, actual, comparaciones);

            actual = cmp < 0 ? actual.Izquierdo : actual.Derecho;
        }

        return new ResultadoBusqueda(false, null, comparaciones);
    }

    // ------------------------------------------------------------------ UPDATE

    /// <summary>
    /// Actualizar = Eliminar(nombreAntiguo) + Insertar(nombreNuevo), conservando el tipo (carpeta/archivo).
    /// Es atómico: si la inserción falla, el árbol vuelve al estado previo (respaldo).
    /// </summary>
    public ResultadoActualizacion Actualizar(string nombreAntiguo, string nombreNuevo)
    {
        if (string.IsNullOrWhiteSpace(nombreAntiguo) || string.IsNullOrWhiteSpace(nombreNuevo))
            return new ResultadoActualizacion(false, MotivoRechazo.NombreInvalido, CasoEliminacion.Ninguno, 0);

        ResultadoBusqueda antiguo = Buscar(nombreAntiguo);
        if (!antiguo.Encontrado)
            return new ResultadoActualizacion(false, MotivoRechazo.NoExiste, CasoEliminacion.Ninguno, 0, nombreAntiguo.Trim());

        ResultadoBusqueda nuevo = Buscar(nombreNuevo);
        if (nuevo.Encontrado)
            return new ResultadoActualizacion(false, MotivoRechazo.Duplicado, CasoEliminacion.Ninguno, 0, nuevo.Nodo!.Nombre);

        bool esCarpeta = antiguo.Nodo!.EsCarpeta;
        Nodo? respaldo = Clonar(Raiz);

        ResultadoEliminacion eliminacion = Eliminar(nombreAntiguo);
        if (!eliminacion.Exito)
        {
            Raiz = respaldo;
            return new ResultadoActualizacion(false, eliminacion.Motivo, CasoEliminacion.Ninguno, 0, eliminacion.NombreReemplazo);
        }

        ResultadoInsercion insercion = Insertar(nombreNuevo, esCarpeta);
        if (!insercion.Exito)
        {
            Raiz = respaldo; // rollback: el árbol queda como estaba
            return new ResultadoActualizacion(false, insercion.Motivo, CasoEliminacion.Ninguno,
                insercion.Comparaciones, insercion.NombreRelacionado);
        }

        return new ResultadoActualizacion(true, MotivoRechazo.Ninguno, eliminacion.Caso,
            insercion.Comparaciones, eliminacion.NombreReemplazo);
    }

    // ------------------------------------------------------------------ DELETE

    /// <summary>
    /// Elimina por nombre. Casos: hoja, un hijo y dos hijos (se usa SIEMPRE el sucesor inorden,
    /// es decir, el mínimo del subárbol derecho).
    /// </summary>
    public ResultadoEliminacion Eliminar(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return new ResultadoEliminacion(false, CasoEliminacion.Ninguno, MotivoRechazo.NombreInvalido);

        var contexto = new ContextoEliminacion();
        Raiz = EliminarRecursivo(Raiz, nombre.Trim(), contexto);

        return new ResultadoEliminacion(
            contexto.Motivo == MotivoRechazo.Ninguno,
            contexto.Caso,
            contexto.Motivo,
            contexto.NombreReemplazo);
    }

    private sealed class ContextoEliminacion
    {
        public CasoEliminacion Caso { get; set; } = CasoEliminacion.Ninguno;
        public MotivoRechazo Motivo { get; set; } = MotivoRechazo.Ninguno;
        public string? NombreReemplazo { get; set; }
    }

    /// <summary>Devuelve la nueva raíz del subárbol tras eliminar el nombre indicado.</summary>
    private static Nodo? EliminarRecursivo(Nodo? nodo, string nombre, ContextoEliminacion contexto)
    {
        if (nodo is null)
        {
            contexto.Motivo = MotivoRechazo.NoExiste;
            return null;
        }

        int cmp = Comparar(nombre, nodo.Nombre);

        if (cmp < 0)
        {
            nodo.Izquierdo = EliminarRecursivo(nodo.Izquierdo, nombre, contexto);
            return nodo;
        }

        if (cmp > 0)
        {
            nodo.Derecho = EliminarRecursivo(nodo.Derecho, nombre, contexto);
            return nodo;
        }

        // Nodo encontrado.
        if (nodo.EsHoja) // Caso 1: hoja -> se desenlaza
        {
            contexto.Caso = CasoEliminacion.Hoja;
            return null;
        }

        if (nodo.Izquierdo is null) // Caso 2: solo hijo derecho -> el hijo sube
        {
            contexto.Caso = CasoEliminacion.UnHijo;
            return nodo.Derecho;
        }

        if (nodo.Derecho is null) // Caso 2: solo hijo izquierdo -> el hijo sube
        {
            contexto.Caso = CasoEliminacion.UnHijo;
            return nodo.Izquierdo;
        }

        // Caso 3: dos hijos -> el sucesor (mínimo del subárbol derecho) ocupa su lugar.
        Nodo sucesor = Minimo(nodo.Derecho);

        // El sucesor quedaría con dos hijos; si fuera un archivo se rompería la regla "archivo = hoja".
        if (sucesor.EsArchivo)
        {
            contexto.Motivo = MotivoRechazo.SucesorEsArchivo;
            contexto.NombreReemplazo = sucesor.Nombre;
            return nodo;
        }

        Nodo? derechoSinSucesor = EliminarMinimo(nodo.Derecho); // desprende al sucesor
        sucesor.Izquierdo = nodo.Izquierdo;
        sucesor.Derecho = derechoSinSucesor;

        contexto.Caso = CasoEliminacion.DosHijos;
        contexto.NombreReemplazo = sucesor.Nombre;
        return sucesor;
    }

    private static Nodo Minimo(Nodo nodo)
    {
        while (nodo.Izquierdo is not null)
            nodo = nodo.Izquierdo;
        return nodo;
    }

    /// <summary>Quita el nodo mínimo del subárbol y devuelve la nueva raíz de ese subárbol.</summary>
    private static Nodo? EliminarMinimo(Nodo nodo)
    {
        if (nodo.Izquierdo is null)
            return nodo.Derecho; // el mínimo no tiene hijo izquierdo: su hijo derecho lo reemplaza

        nodo.Izquierdo = EliminarMinimo(nodo.Izquierdo);
        return nodo;
    }

    // -------------------------------------------------------------- RECORRIDOS

    public List<Nodo> Preorden()
    {
        var resultado = new List<Nodo>();
        Preorden(Raiz, resultado);
        return resultado;
    }

    public List<Nodo> Inorden()
    {
        var resultado = new List<Nodo>();
        Inorden(Raiz, resultado);
        return resultado;
    }

    public List<Nodo> Postorden()
    {
        var resultado = new List<Nodo>();
        Postorden(Raiz, resultado);
        return resultado;
    }

    /// <summary>Recorrido por niveles (BFS) usando una cola.</summary>
    public List<Nodo> PorNiveles()
    {
        var resultado = new List<Nodo>();
        if (Raiz is null) return resultado;

        var cola = new Queue<Nodo>();
        cola.Enqueue(Raiz);

        while (cola.Count > 0)
        {
            Nodo actual = cola.Dequeue();
            resultado.Add(actual);

            if (actual.Izquierdo is not null) cola.Enqueue(actual.Izquierdo);
            if (actual.Derecho is not null) cola.Enqueue(actual.Derecho);
        }

        return resultado;
    }

    private static void Preorden(Nodo? nodo, List<Nodo> salida)
    {
        if (nodo is null) return;
        salida.Add(nodo);
        Preorden(nodo.Izquierdo, salida);
        Preorden(nodo.Derecho, salida);
    }

    private static void Inorden(Nodo? nodo, List<Nodo> salida)
    {
        if (nodo is null) return;
        Inorden(nodo.Izquierdo, salida);
        salida.Add(nodo);
        Inorden(nodo.Derecho, salida);
    }

    private static void Postorden(Nodo? nodo, List<Nodo> salida)
    {
        if (nodo is null) return;
        Postorden(nodo.Izquierdo, salida);
        Postorden(nodo.Derecho, salida);
        salida.Add(nodo);
    }

    // ----------------------------------------------------------------- MÉTRICAS

    /// <summary>Altura medida en niveles: árbol vacío = 0, solo raíz = 1.</summary>
    public int Altura() => Altura(Raiz);

    public int ContarNodos() => ContarNodos(Raiz);

    public int ContarHojas() => ContarHojas(Raiz);

    private static int Altura(Nodo? nodo) =>
        nodo is null ? 0 : 1 + Math.Max(Altura(nodo.Izquierdo), Altura(nodo.Derecho));

    private static int ContarNodos(Nodo? nodo) =>
        nodo is null ? 0 : 1 + ContarNodos(nodo.Izquierdo) + ContarNodos(nodo.Derecho);

    private static int ContarHojas(Nodo? nodo)
    {
        if (nodo is null) return 0;
        if (nodo.EsHoja) return 1;
        return ContarHojas(nodo.Izquierdo) + ContarHojas(nodo.Derecho);
    }

    // ------------------------------------------------------------ VERIFICACIONES

    /// <summary>El inorden de un BST válido es estrictamente creciente (sin duplicados).</summary>
    public bool InordenEstaOrdenado()
    {
        List<Nodo> inorden = Inorden();

        for (int i = 1; i < inorden.Count; i++)
        {
            if (Comparar(inorden[i - 1].Nombre, inorden[i].Nombre) >= 0)
                return false;
        }

        return true;
    }

    /// <summary>Verifica la regla de dominio: todo archivo es hoja.</summary>
    public bool ArchivosSonHojas() => ArchivosSonHojas(Raiz);

    private static bool ArchivosSonHojas(Nodo? nodo)
    {
        if (nodo is null) return true;
        if (nodo.EsArchivo && !nodo.EsHoja) return false;
        return ArchivosSonHojas(nodo.Izquierdo) && ArchivosSonHojas(nodo.Derecho);
    }

    // ------------------------------------------------------------------ UTILIDAD

    /// <summary>Copia profunda, usada como respaldo para que Actualizar sea atómico.</summary>
    private static Nodo? Clonar(Nodo? nodo)
    {
        if (nodo is null) return null;

        return new Nodo(nodo.Nombre, nodo.EsCarpeta)
        {
            Izquierdo = Clonar(nodo.Izquierdo),
            Derecho = Clonar(nodo.Derecho)
        };
    }
}
