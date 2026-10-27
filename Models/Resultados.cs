namespace DocuTrack.Models;

/// <summary>Caso de eliminación que se ejecutó en el BST.</summary>
public enum CasoEliminacion
{
    Ninguno,
    Hoja,
    UnHijo,
    DosHijos
}

/// <summary>Razón por la que el Model rechazó una operación.</summary>
public enum MotivoRechazo
{
    Ninguno,
    NombreInvalido,
    Duplicado,
    PadreEsArchivo,
    NoExiste,
    SucesorEsArchivo
}

public sealed record ResultadoBusqueda(bool Encontrado, Nodo? Nodo, int Comparaciones);

/// <param name="NombreRelacionado">Nodo que provocó el rechazo (el duplicado o el archivo "padre").</param>
public sealed record ResultadoInsercion(
    bool Exito,
    MotivoRechazo Motivo,
    int Comparaciones,
    string? NombreRelacionado = null);

/// <param name="NombreReemplazo">En el caso de dos hijos: nombre del sucesor que ocupó el lugar.</param>
public sealed record ResultadoEliminacion(
    bool Exito,
    CasoEliminacion Caso,
    MotivoRechazo Motivo,
    string? NombreReemplazo = null);

/// <param name="NombreRelacionado">
/// Si hubo éxito: sucesor usado en la eliminación (si aplica).
/// Si falló: nodo que provocó el rechazo.
/// </param>
public sealed record ResultadoActualizacion(
    bool Exito,
    MotivoRechazo Motivo,
    CasoEliminacion CasoEliminacion,
    int ComparacionesInsercion,
    string? NombreRelacionado = null);
