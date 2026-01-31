using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[RequireComponent(typeof(Tilemap))]
public class TilemapFoliageRandomizer : MonoBehaviour
{
    [Header("Replacement Rules")]
    public TileBase replaceThisTile;

    [Header("Foliage Settings")]
    public List<TileBase> foliageTiles = new List<TileBase>();

    [Range(0f, 1f)]
    public float replaceChance = 0.7f;

    private Tilemap tilemap;

    void Awake()
    {
        tilemap = GetComponent<Tilemap>();
    }

    void Start()
    {
        RandomizeTiles();
    }

    public void RandomizeTiles()
    {
        if (replaceThisTile == null)
        {
            Debug.LogWarning("oops dun fuked up: No target tile to replace specified.");
            return;
        }

        if (foliageTiles == null || foliageTiles.Count == 0)
        {
            Debug.LogWarning("oops dun fuked up: No foliage tiles specified.");
            return;
        }

        BoundsInt bounds = tilemap.cellBounds;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            TileBase existingTile = tilemap.GetTile(pos);
            if (existingTile == null) continue;

            if (existingTile != replaceThisTile) continue;

            if (Random.value <= replaceChance)
            {
                TileBase randomTile = foliageTiles[Random.Range(0, foliageTiles.Count)];
                tilemap.SetTile(pos, randomTile);
            }
            else
            {
                tilemap.SetTile(pos, null);
            }
        }

        tilemap.RefreshAllTiles();
    }
}
