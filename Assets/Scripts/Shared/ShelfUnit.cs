using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Una CARA de un modulo de repisa (la que mira a un pasillo). Guarda sus productos para que en la escena 2
/// los clientes puedan "llevarse" productos (se desactivan).
/// </summary>
public class ShelfUnit : MonoBehaviour
{
    public int rowIndex;
    public int side = 1;          // +1 = la cara mira hacia +X, -1 = hacia -X
    public float aisleX;          // X del pasillo al que mira esta cara
    public float zCenter;         // Z del centro del modulo
    public bool essential;        // true si vende productos "de panico"
    public List<GameObject> items = new List<GameObject>();
    public int remaining;         // items[0 .. remaining-1] siguen en la repisa
    public int total;

    public void Finish()
    {
        total = items.Count;
        remaining = total;
    }

    /// <summary>Quita un producto al azar. Devuelve el id de la marca (nombre del objeto) o null si esta vacio.</summary>
    public string TryTake()
    {
        if (remaining <= 0) return null;
        int idx = Random.Range(0, remaining);
        GameObject go = items[idx];
        string id = go.name;
        go.SetActive(false);
        items[idx] = items[remaining - 1];
        items[remaining - 1] = go;
        remaining--;
        return id;
    }
}
