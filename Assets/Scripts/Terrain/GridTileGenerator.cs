
using UnityEngine;

public class GridTileGenerator : MonoBehaviour
{
    public GridManager gridManager;

    [Header("Configuracion")]
    public float thickness = 0.08f;

    [Header("Materiales opcionales")]
    public Material plainMaterial;
    public Material desertMaterial;
    public Material snowMaterial;
    public Material waterMaterial;

    [ContextMenu("Generar Casillas")]
    public void GenerateTiles()
    {
        if (Application.isPlaying) return;

        if (gridManager == null || gridManager.ground == null)
        {
            Debug.LogError("Asigna el GridManager.");
            return;
        }

        int terrainLayer = LayerMask.NameToLayer("Terrain");

        if (terrainLayer == -1)
        {
            Debug.LogError("Falta la Layer Terrain.");
            return;
        }

        Transform previous = transform.Find("Casillas");

        if (previous != null)
            DestroyImmediate(previous.gameObject);

        GameObject parent = new GameObject("Casillas");
        parent.transform.SetParent(transform);

        float size = gridManager.cellSize;

        float startX = gridManager.ground.position.x
            - gridManager.width * size / 2f;

        float startZ = gridManager.ground.position.z
            - gridManager.depth * size / 2f;

        for (int x = 0; x < gridManager.width; x++)
        {
            for (int z = 0; z < gridManager.depth; z++)
            {
                GameObject tile =
                    GameObject.CreatePrimitive(PrimitiveType.Cube);

                tile.name = "Casilla_" + x + "_" + z;
                tile.transform.SetParent(parent.transform);

                tile.transform.position = new Vector3(
                    startX + (x + 0.5f) * size,
                    gridManager.ground.position.y,
                    startZ + (z + 0.5f) * size
                );

                tile.transform.localScale = new Vector3(
                    size * 0.98f,
                    thickness,
                    size * 0.98f
                );

                tile.layer = terrainLayer;

                BoxCollider collider =
                    tile.GetComponent<BoxCollider>();

                collider.isTrigger = true;

                TerrainZone zone =
                    tile.AddComponent<TerrainZone>();

                zone.terrainType = TerrainType.Plain;

                if (plainMaterial != null)
                    tile.GetComponent<Renderer>().sharedMaterial =
                        plainMaterial;
            }
        }

        Debug.Log("Casillas generadas.");
    }

    [ContextMenu("Actualizar Materiales")]
    public void UpdateMaterials()
    {
        Transform parent = transform.Find("Casillas");

        if (parent == null) return;

        foreach (Transform child in parent)
        {
            TerrainZone zone =
                child.GetComponent<TerrainZone>();

            Renderer renderer =
                child.GetComponent<Renderer>();

            if (zone == null || renderer == null)
                continue;

            Material material = null;

            switch (zone.terrainType)
            {
                case TerrainType.Plain:
                    material = plainMaterial;
                    break;

                case TerrainType.Desert:
                    material = desertMaterial;
                    break;

                case TerrainType.Snow:
                    material = snowMaterial;
                    break;

                case TerrainType.Water:
                    material = waterMaterial;
                    break;
            }

            if (material != null)
                renderer.sharedMaterial = material;
        }
    }
}
