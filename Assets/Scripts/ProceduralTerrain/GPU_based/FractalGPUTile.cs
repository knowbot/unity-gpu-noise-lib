using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

/*
 inspiration for this implementation:
 https://github.com/cinight/MinimalCompute/blob/master/Assets/AsyncGPUReadbackMesh/AsyncGPUReadbackMesh.compute
 https://github.com/cinight/MinimalCompute/blob/master/Assets/AsyncGPUReadbackMesh/AsyncGPUReadbackMesh.cs
 *
 * Earlier implementation made use of ASyncGPUReadback to access data from the buffer, but it did not end up
 * being necessary as reading directly from the buffer is sufficient (no performance change) and avoids the
 * frame latency introduced by asynchronous reading. However, the previous code is left as comments since it
 * still has some learning value.
 */

namespace ProceduralTerrain.GPU_based
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class FractalGPUTile : MonoBehaviour, IObserver
    {
        public Transform spider;
        [Header("Tile settings")] 
        public int xSize;
        public int zSize;
        public int density = 1;

        [Header("Compute shader")]
        public ComputeShader computeShader;

        private NoiseManager settings;
    
        // Compute shader stuff
        private ComputeBuffer vertexBuffer;
        private int kernel;
        private int dispatchCount;
        // private NativeArray<Vector3> vertData;
        // private AsyncGPUReadbackRequest request;
        private Vector3[] vertices;
        private Mesh mesh;
        private int xVerts, zVerts;
   
    
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;

        private Vector2 localOffset = Vector2.zero;
        private Vector2 oldOffset = Vector2.zero;

        [SerializeField] private bool isMeshUpdatable = true;
        private bool needsUpdate = true;

        public void Receive(ISubject subject)
        {
            needsUpdate = true;
        }

        // Start is called before the first frame update
        private void Awake()
        {
            if(!SystemInfo.supportsAsyncGPUReadback) { gameObject.SetActive(false); return; }
            settings = NoiseManager.Instance;
            settings.Attach(this);
            xVerts = xSize * density;
            zVerts = zSize * density;
            meshFilter = GetComponent<MeshFilter>();
            meshCollider = GetComponent<MeshCollider>();
            Generate();
            InitComputeShader();
        }
    
        private void Generate()
        {
            meshFilter.mesh = mesh = new Mesh();
            mesh.name = "Tile";
            vertices = new Vector3[(xVerts + 1) * (zVerts + 1)];
            Vector2[] uv = new Vector2[vertices.Length];
            int index = 0;
            for (int z = 0; z <= zVerts; z++) {
                for (int x = 0; x <= xVerts; x++) {
                    vertices[index] = new Vector3((float)x / density,0, (float)z / density);
                    uv[index] = new Vector2((float)x / xVerts, (float)z / zVerts);
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
            meshCollider.sharedMesh = mesh;
        }

        private void InitComputeShader()
        {
            // init compute shader
            // computeShader = Instantiate(computeShader);
            kernel = computeShader.FindKernel("heightmap_" + settings.function.ToString().ToLower());
            computeShader.GetKernelThreadGroupSizes(kernel, out uint threadX, out uint _, out uint _);
            dispatchCount = Mathf.CeilToInt(mesh.vertexCount / threadX + 1);
            UpdateNoiseSettings();
        
            // Set vertex buffer
            Vector3[] meshVerts = mesh.vertices;
            NativeArray<Vector3> vertData = new NativeArray<Vector3>(mesh.vertexCount, Allocator.Temp);
            for (int i = 0; i < mesh.vertexCount; ++i)
                vertData[i] = meshVerts[i];
            vertexBuffer = new ComputeBuffer(mesh.vertexCount, 12);
            if(vertData.IsCreated) vertexBuffer.SetData(vertData);
            computeShader.SetBuffer(kernel, "vertex_buffer", vertexBuffer);
        
            computeShader.SetInt("x_size", xSize);
            computeShader.SetInt("z_size", zSize);
            vertData.Dispose();
            // request = AsyncGPUReadback.Request(vertexBuffer);
            // print(request.hasError ? "Invalid request?" : "First update launched correctly!");
        }

        private void Update()
        {
            if(isMeshUpdatable)
                UpdateMesh();
        }

        private void UpdateMesh(bool force = false)
        {
            Transform transform1 = transform;
            Vector3 position = transform1.position;
            Vector3 localScale = transform1.localScale;
            localOffset.x = (position.x / (xSize * localScale.x));
            localOffset.y = (position.z / (zSize * localScale.z));
        
            // some optimization: only update the mesh if it should be changed!
            if (!force && oldOffset == localOffset && !needsUpdate) return;
            // stuff is gonna be updated baby
            needsUpdate = false;
            oldOffset = localOffset;
            
            UpdateNoiseSettings();
            computeShader.Dispatch(kernel, dispatchCount, 1, 1);
            Vector3[] vData = new Vector3[mesh.vertexCount];
            mesh.MarkDynamic();
            vertexBuffer.GetData(vData);
            mesh.SetVertices(vData);
            mesh.RecalculateNormals();
            new BakeJob(mesh.GetInstanceID()).Execute();
            meshCollider.sharedMesh = mesh;
        }

        private void UpdateNoiseSettings()
        {
            kernel = computeShader.FindKernel("heightmap_" + settings.function.ToString().ToLower());
            computeShader.SetFloat("multiplier", settings.Multiplier);
            computeShader.SetFloat("octaves", settings.Octaves);
            computeShader.SetFloat("lacunarity", settings.Lacunarity);
            computeShader.SetFloat("gain", settings.Gain);
            computeShader.SetFloat("amplitude", settings.Amplitude);
            computeShader.SetFloat("frequency", settings.Frequency);
            computeShader.SetFloat("local_offset_x", localOffset.x);
            computeShader.SetFloat("local_offset_z", localOffset.y);
            computeShader.SetFloat("scale", settings.Scale);
            computeShader.SetFloat("exponent", settings.Exponent);
            computeShader.SetVector("offset", settings.Offset);
        }
        
        public readonly struct BakeJob : IJob
        {
            private readonly int meshId;
            public BakeJob(int meshId)
            {
                this.meshId = meshId;
            }

            public void Execute()
            {
                Physics.BakeMesh(meshId, false);
            }
        }

        private void CleanUp()
        {
            vertexBuffer?.Release();
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
}
