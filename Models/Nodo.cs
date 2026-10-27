namespace DocuTrack.Models;

/// <summary>
/// Nodo del BST. Representa una carpeta o un archivo del repositorio de DocuTrack.
/// Regla de dominio: un archivo siempre es hoja.
/// </summary>
public sealed class Nodo
{
    /// <summary>Clave de ordenamiento del BST (se compara sin distinguir mayúsculas).</summary>
    public string Nombre { get; }

    /// <summary>true = carpeta, false = archivo.</summary>
    public bool EsCarpeta { get; }

    public Nodo? Izquierdo { get; internal set; }
    public Nodo? Derecho { get; internal set; }

    public bool EsArchivo => !EsCarpeta;
    public bool EsHoja => Izquierdo is null && Derecho is null;
    public int CantidadHijos => (Izquierdo is null ? 0 : 1) + (Derecho is null ? 0 : 1);

    public Nodo(string nombre, bool esCarpeta)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre no puede ser nulo ni vacío.", nameof(nombre));

        Nombre = nombre.Trim();
        EsCarpeta = esCarpeta;
    }
}
