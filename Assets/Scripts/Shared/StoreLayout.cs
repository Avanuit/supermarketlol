using UnityEngine;

/// <summary>
/// Medidas del supermercado. Las usan TANTO el generador del editor como los scripts en tiempo de ejecucion,
/// asi que si cambias un numero aqui, todo se ajusta solo.
/// Origen (0,0,0) = centro del piso. La puerta esta en z = FrontZ (frente) y el fondo en z = BackZ.
/// </summary>
public static class StoreLayout
{
    public const float HalfWidth = 13.5f;     // la tienda mide 27 m de ancho (x de -13.5 a 13.5)
    public const float FrontZ = -19.5f;       // pared de la entrada
    public const float BackZ = 21.5f;         // pared del fondo (donde van los refrigeradores)
    public const float Height = 4f;           // altura del techo

    // 5 filas de repisas dobles (cada una tiene cara izquierda y cara derecha)
    public static readonly float[] RowX = { -9f, -4.5f, 0f, 4.5f, 9f };
    // 6 pasillos: 2.25 m a cada lado de cada fila (los de los extremos pegan con la pared)
    public static readonly float[] AisleX = { -11.25f, -6.75f, -2.25f, 2.25f, 6.75f, 11.25f };
    // 6 cajas registradoras, una al final de cada pasillo
    public static readonly float[] LaneX = { -11.25f, -6.75f, -2.25f, 2.25f, 6.75f, 11.25f };

    public const float CrossZ = 0f;           // "pasillo transversal" por donde la gente cambia de pasillo
    public const float ShelfStartZ = 1.5f;    // donde empiezan las repisas
    public const int UnitsPerRow = 9;         // 9 modulos de repisa por fila
    public const float UnitLength = 2f;       // cada modulo mide 2 m

    public const float CheckoutZ = -15f;      // posicion de los mostradores
    public const float QueueStartZ = -12.4f;  // primer puesto de la fila (el que esta siendo atendido)
    public const float QueueSpacing = 1.8f;   // separacion entre clientes (los carros crecen, necesitan espacio)
    public const int QueueMax = 7;            // maximo de clientes por fila

    public static readonly Vector3 DoorPos = new Vector3(0f, 0f, -19.5f);
    public const float DoorHalfWidth = 2f;    // la puerta es un hueco de 4 m en el centro de la pared frontal

    public static float UnitZ(int i) { return ShelfStartZ + UnitLength * (i + 0.5f); }
}
