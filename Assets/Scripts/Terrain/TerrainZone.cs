
using UnityEngine;

//Este script no necesita Update() ni Start(). Solo almacena el tipo de terreno asignado al objeto.
public class TerrainZone : MonoBehaviour
{
    public TerrainType terrainType = TerrainType.Plain;
}
