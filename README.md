# DocuTrack – Taller Integrador: Árboles Binarios (MVC en C#)

Universidad de Manizales · Facultad de Ciencias e Ingeniería · Programación III
Actividad colaborativa I – Unidad 1

Módulo interno de **DocuTrack S.A.** para gestionar su repositorio de documentos con un
**Árbol Binario de Búsqueda (BST)** simple, sin balanceo (decisión pedagógica de la Unidad 1).

## Integrantes y responsabilidades

| Integrante | Responsabilidades |
|---|---|
| _Sofia Giraldo Valencia_ | Trabajo individual: diseño e implementación del Model (BST, CRUD, recorridos, métricas), View (árbol ASCII y mensajes), Controller, pruebas y documentación |

## Requisitos para ejecutar

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) o superior (el proyecto apunta a `net10.0` en `DocuTrack.csproj`; con otra versión del SDK, cambia `TargetFramework`, p. ej. a `net8.0`).
- Una consola con soporte UTF-8 para ver los conectores `├── └── │`.

```bash
git clone https://github.com/1206sofiagiraldo/DocuTrack-S.A..git
cd DocuTrack-S.A.
dotnet run
```

El programa ejecuta de principio a fin todos los casos de uso del taller, sin pedir datos.
Etiqueta del commit final recomendada: `release-unidad1`.

## Arquitectura MVC

```
DocuTrack/
├── Program.cs                      # Punto de entrada: solo llama al Controller
├── Models/
│   ├── Nodo.cs                     # Nombre, EsCarpeta, Izquierdo, Derecho
│   ├── ArbolBinario.cs             # CRUD, recorridos, métricas (sin Console)
│   └── Resultados.cs               # Resultados de cada operación (records y enums)
├── Views/
│   └── VistaConsola.cs             # Toda la presentación: árbol ASCII, mensajes, métricas
└── Controllers/
    └── DocuTrackController.cs      # Orquesta y secuencia los casos de uso
```

| Capa | Qué hace | Qué NO hace |
|---|---|---|
| **Model** | Reglas del BST, CRUD, recorridos, altura, conteo de comparaciones | No usa `Console`; no imprime |
| **View** | Imprime árbol, resultados, recorridos y métricas | No decide ni modifica el árbol |
| **Controller** | Decide el orden de los casos y conecta Model con View | No recorre ni reenlaza nodos |
| **Program** | Crea el Controller y lo ejecuta | Sin lógica de negocio |

## Decisiones de diseño

- **Clave:** `Nombre`, comparación ordinal sin distinguir mayúsculas (`StringComparison.OrdinalIgnoreCase`).
- **Duplicados:** se rechazan (ignorando mayúsculas) y la View informa el intento. Ejemplo: `contratos` es duplicado de `Contratos`.
- **Un archivo siempre es hoja:** `Insertar` rechaza cualquier nodo que quedaría como hijo de un archivo.
- **Comparaciones:** se cuenta 1 por cada nodo contra el que se compara la clave (en `Buscar` e `Insertar`).
- **Actualizar:** `Eliminar(antiguo)` + `Insertar(nuevo)`, conservando si es carpeta o archivo. Valida que el nombre nuevo no exista. Es atómico: si la inserción falla, se restaura el árbol (respaldo).
- **Eliminar con dos hijos:** siempre se usa el **sucesor inorden** (mínimo del subárbol derecho), de forma consistente. Como el sucesor ocupa un lugar con dos hijos, si fuera un archivo se rompería la regla "archivo = hoja"; en ese caso la operación se rechaza e informa el motivo.
- **Altura:** medida en **niveles** (árbol vacío = 0, solo raíz = 1). Aristas = niveles − 1; la View muestra ambos valores.

## Casos de uso que ejecuta el programa

1. Construcción con 14 nombres (carpetas y archivos) + impresión del árbol inicial.
   - 1.1 Intentos inválidos: duplicado exacto, duplicado con otras mayúsculas, hijo de un archivo y nombre vacío.
2. 6 búsquedas (2 en el subárbol izquierdo, 2 en el derecho, 2 inexistentes) con número de comparaciones.
3. 3 actualizaciones (hoja, nodo con un hijo, raíz), con el árbol impreso después de cada una.
4. 3 eliminaciones (hoja, nodo con un hijo, raíz con dos hijos), con el árbol impreso después de cada una.
5. Recorridos: preorden, inorden, postorden y por niveles (con `Queue`), más verificaciones.
6. Árbol final y altura.

### Árbol inicial (salida esperada)

```
  [C] Documentos   <- raíz
  ├── (I) [C] Contratos
  │   ├── (I) [C] Balances
  │   │   └── (I) [A] Actas.pdf
  │   └── (D) [A] Datos.csv
  └── (D) [C] Reportes
      ├── (I) [C] Facturas
      │   ├── (I) [C] Estados
      │   └── (D) [C] Proyectos
      │       └── (I) [C] Manuales
      │           └── (I) [A] Informe.docx
      └── (D) [C] Usuarios
          ├── (I) [C] Temp
          └── (D) [A] Videos.mp4
```

Leyenda: `[C]` carpeta, `[A]` archivo, `(I)` hijo izquierdo, `(D)` hijo derecho.

## ¿Qué recorrido confirma que el BST sigue ordenado?

El **inorden** (izquierdo → raíz → derecho). En un BST válido todo lo que está a la izquierda de un nodo es menor
y todo lo que está a la derecha es mayor, así que visitar izquierda–raíz–derecha produce los nombres
**en orden estrictamente creciente**. Si después de insertar, actualizar o eliminar el inorden sigue ordenado
(sin repetidos), la propiedad del BST se conserva. El programa lo comprueba con `InordenEstaOrdenado()`
y lo muestra en la sección 5. Preorden, postorden y por niveles dependen de la forma del árbol y no sirven como prueba de orden.
