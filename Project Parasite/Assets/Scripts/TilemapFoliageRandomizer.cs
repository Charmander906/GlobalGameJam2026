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

    [System.Serializable]
    public class TreeDefinition
    {
        public TileBase trunkTile;
        public TileBase topTile;
    }

    public List<TreeDefinition> treeTypes = new List<TreeDefinition>();

    [Header("Settings")]
    [Range(0f, 1f)] public float replaceChance = 0.7f;
    [Range(0f, 1f)] public float treeChance = 0.25f;
    [Range(0f, 1f)] public float foliageInForestChance = 0.2f;
    public int minTreeHeight = 2;
    public int maxTreeHeight = 5;
    public bool background = false;

    [Header("Noise Settings")]
    public int seed = 42;
    public float noiseScale = 0.1f;
    public float forestThreshold = 0.5f;

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
            Debug.LogWarning("oops dun fuked up: No tile specified to replace.");
            return;
        }

        if (foliageTiles.Count == 0 && treeTypes.Count == 0)
        {
            Debug.LogWarning("oops dun fuked up: No foliage or tree types specified for replacement.");
            return;
        }

        BoundsInt bounds = tilemap.cellBounds;
        System.Random rng = new System.Random(seed);

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            TileBase existingTile = tilemap.GetTile(pos);
            if (existingTile != replaceThisTile) continue;

            float noiseValue = Mathf.PerlinNoise((pos.x + seed) * noiseScale, (pos.y + seed) * noiseScale);

            if (noiseValue > replaceChance)
            {
                tilemap.SetTile(pos, null);
                continue;
            }

            if (treeTypes.Count > 0 && noiseValue < forestThreshold)
            {
                float roll = (float)rng.NextDouble();

                if (roll < treeChance)
                {
                    PlaceTree(pos, rng);
                }
                else if (roll < treeChance + foliageInForestChance && foliageTiles.Count > 0)
                {
                    TileBase randomTile = foliageTiles[rng.Next(foliageTiles.Count)];
                    tilemap.SetTile(pos, randomTile);
                }
                else
                {
                    tilemap.SetTile(pos, null);
                }
            }
            else if (foliageTiles.Count > 0)
            {
                TileBase randomTile = foliageTiles[rng.Next(foliageTiles.Count)];
                tilemap.SetTile(pos, randomTile);
            }
            else
            {
                tilemap.SetTile(pos, null);
            }
        }

        tilemap.RefreshAllTiles();
    }

    void PlaceTree(Vector3Int basePos, System.Random rng)
    {
        TreeDefinition tree = treeTypes[rng.Next(treeTypes.Count)];
        if (tree.trunkTile == null || tree.topTile == null) return;

        int height = rng.Next(minTreeHeight, maxTreeHeight + 1);

        Vector3Int startPos = basePos;
        if (background)
        {
            tilemap.SetTile(startPos, null);
            startPos += new Vector3Int(0, -1, 0);
        }

        for (int i = 0; i < height - 1; i++)
        {
            Vector3Int trunkPos = startPos + new Vector3Int(0, i * 2, 0);
            tilemap.SetTile(trunkPos, tree.trunkTile);
        }

        Vector3Int topPos = startPos + new Vector3Int(0, (height - 1) * 2, 0);
        tilemap.SetTile(topPos, tree.topTile);
    }
}
