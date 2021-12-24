using System;
using ProceduralTerrain;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;


[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class FractalCPUTile : MonoBehaviour
{
    [Header("Tile settings")] 
    public int xSize;
    public int zSize;
    public int density = 1;
    

    private NoiseManager noiseManager;

    // Compute shader stuff
    private Vector3[] vertices;
    private Mesh mesh;
    private int xVerts, zVerts;


    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    [SerializeField] private float offXLocal;
    [SerializeField] private float offZLocal;
    // Start is called before the first frame update
    private void Start()
    {
        if (!SystemInfo.supportsAsyncGPUReadback)
        {
            gameObject.SetActive(false);
            return;
        }

        xVerts = xSize * density;
        zVerts = zSize * density;
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        noiseManager = NoiseManager.Instance;
        Generate();
    }

    private void Generate()
    {
        Transform transform1 = transform;
        Vector3 position = transform1.position;
        Vector3 localScale = transform1.localScale;
        offXLocal = (position.x / (xSize * localScale.x));
        offZLocal = (position.z / (zSize * localScale.z));
        
        meshFilter.mesh = mesh = new Mesh();
        mesh.name = "Tile";
        vertices = new Vector3[(xVerts + 1) * (zVerts + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        int index = 0;
        for (int z = 0; z <= zVerts; z++)
        {
            for (int x = 0; x <= xVerts; x++)
            {
                vertices[index] = new Vector3((float) x / density, 0, (float) z / density);
                uv[index] = new Vector2((float) x / xVerts, (float) z / zVerts);
                vertices[index].y = PerlinNoise.Fractal2D(uv[index], new Vector2(offXLocal, offZLocal));
                index++;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;

        int[] triangles = new int[xVerts * zVerts * 6];
        int tIndex = 0;
        int vIndex = 0;
        for (int z = 0; z < zVerts; z++)
        {
            for (int x = 0; x < xVerts; x++)
            {
                triangles[tIndex] = vIndex;
                triangles[tIndex + 3] = triangles[tIndex + 2] = vIndex + 1;
                triangles[tIndex + 4] = triangles[tIndex + 1] = vIndex + xVerts + 1;
                triangles[tIndex + 5] = vIndex + xVerts + 2;
                tIndex += 6;
                vIndex++;
            }

            vIndex++;
        }

        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    private void Update()
    {
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            mesh.vertices[i].y = PerlinNoise.Fractal2D(mesh.uv[i], new Vector2(offXLocal, offZLocal));
        }
        
    }


    private void CleanUp()
    {
    }

    void OnDisable()
    {
        CleanUp();
    }

    void OnDestroy()
    {
        CleanUp();
    }
}