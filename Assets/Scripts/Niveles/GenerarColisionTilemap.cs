using UnityEngine;
using UnityEngine.Tilemaps;

// Asegura que la colisión del Tilemap exista al iniciar el nivel
// (genera las formas del TilemapCollider2D y las une en el CompositeCollider2D).
[DefaultExecutionOrder(-100)]
public class GenerarColisionTilemap : MonoBehaviour
{
    void Awake()
    {
        TilemapCollider2D colTiles = GetComponent<TilemapCollider2D>();
        if (colTiles != null) colTiles.ProcessTilemapChanges();
        CompositeCollider2D compuesto = GetComponent<CompositeCollider2D>();
        if (compuesto != null) compuesto.GenerateGeometry();
    }
}
