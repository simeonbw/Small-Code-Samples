using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TerrainType
{
    public string name;
    [Range(0f, 1f)]
    public float height;
    public Color color;
}

[System.Serializable]
public class Wave
{
    public float Seed;
    public float Frequency;
    public float Amplitude;
}
    

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class BasicTerrainGenerator : MonoBehaviour
{
    [SerializeField] private int _xSize = 20;
    [SerializeField] private int _ySize = 20;

    [SerializeField] private Vector3[] _vertexBuffer;
    [SerializeField] private int[] _indexBuffer;
    [SerializeField] private Vector2[] _uvBuffer;
    private Mesh _mesh;
    private MeshFilter _filter;
    [SerializeField] private int _perlinScale = 4;
    [SerializeField] private float _heightScale = 1f;

    [SerializeField] private AnimationCurve _heightCurve;

    public Wave[] _waves;
    [SerializeField] private Wave[] _heatWaves;

    [SerializeField] private TerrainType[] _heightTerrainTypes;

    private void Awake()
    {
        if (_filter == null)
        {
            _filter = GetComponent<MeshFilter>();
        }
    }

    /// <summary>
    /// Build the texture to be applied to the terrain from the noise map and specified terrain types
    /// </summary>
    private Texture2D BuildTexture(float[,] noiseMap, TerrainType[] terrainTypes)
    {
        Color[] colorMap = new Color[noiseMap.GetLength(0) * noiseMap.GetLength(1)];

        for (int zIndex = 0; zIndex < noiseMap.GetLength(0); zIndex++)
        {
            for (int xIndex = 0; xIndex < noiseMap.GetLength(1); xIndex++)
            {
                int colorIndex = zIndex * noiseMap.GetLength(1) + xIndex;
                float height = noiseMap[zIndex, xIndex];
                height = _heightCurve.Evaluate(height);
                TerrainType terrainType = SelectTerrainType(height, terrainTypes);
                colorMap[colorIndex] = terrainType.color;
            }
        }

        Texture2D output = new Texture2D(noiseMap.GetLength(0), noiseMap.GetLength(1));
        output.wrapMode = TextureWrapMode.Clamp;
        output.SetPixels(colorMap);
        output.Apply();

        return output;
    }

    /// <summary>
    /// Get the correct terrain type by the sampled height
    /// </summary>
    private TerrainType SelectTerrainType(float height, TerrainType[] terrainTypes)
    {
        foreach(TerrainType type in terrainTypes)
        {
            if (height < type.height) return type;
        }
        return terrainTypes[terrainTypes.Length - 1];
    }

    /// <summary>
    /// Build the heightmap for the terrain from the provided noise map
    /// </summary>
    private Texture2D BuildHeightTexture(float[,] noiseMap)
    {
        Color[] colorMap = new Color[noiseMap.GetLength(0) * noiseMap.GetLength(1)];

        for (int zIndex = 0; zIndex < noiseMap.GetLength(0); zIndex++)
        {
            for (int xIndex = 0; xIndex < noiseMap.GetLength(1); xIndex++)
            {
                int colorIndex = zIndex * noiseMap.GetLength(1) + xIndex;
                float height = noiseMap[zIndex, xIndex];

                colorMap[colorIndex] = Color.Lerp(Color.black, Color.white, height);
            }
        }

        Texture2D output = new Texture2D(noiseMap.GetLength(0), noiseMap.GetLength(1));
        output.wrapMode = TextureWrapMode.Clamp;
        output.SetPixels(colorMap);
        output.Apply();

        return output;
    }

    /// <summary>
    /// Create a perlin noise map from the data provided
    /// </summary>
    public float[,] PerlinNoiseMap(int Depth, int Height, float scale, Wave[] waves)
    {
        float[,] noiseMap = new float[Depth, Height];

        for (int zIndex = 0; zIndex < Depth; zIndex++)
        {
            for (int xIndex = 0; xIndex < Height; xIndex++)
            {
                float sampleX = xIndex / scale;
                float sampleZ = zIndex / scale;

                float noise = 0;
                float normalization = 0;

                foreach(Wave wave in waves)
                {
                    noise += wave.Amplitude * Mathf.PerlinNoise(sampleX * wave.Frequency + wave.Seed, sampleZ * wave.Frequency + wave.Seed);
                    normalization += wave.Amplitude;
                }

                noise /= normalization;
                noiseMap[zIndex, xIndex] = _heightCurve.Evaluate(noise);
            }
        }

        return noiseMap;
    }


    /// <summary>
    /// Generate a procedural terrain mesh and texture
    /// </summary>
    [ContextMenu("Generate Terrain")]
    private void Generator()
    {
        Vector3 tileDimension = GetComponent<MeshFilter>().sharedMesh.bounds.size;
        float DistanceBetweenVertices = tileDimension.z / _ySize;
        float vertexOffset = transform.position.z / DistanceBetweenVertices;

        float[,] noiseMap = PerlinNoiseMap(_xSize + 1, _ySize + 1, _perlinScale, _waves);

        Texture2D heightmap = BuildTexture(noiseMap, _heightTerrainTypes);

        GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_SplatMap", heightmap);

        GetComponent<MeshFilter>().mesh = _mesh = new Mesh();

        _mesh.MarkDynamic();

        _vertexBuffer = new Vector3[(_xSize + 1) * (_ySize + 1)];
        _uvBuffer = new Vector2[(_xSize + 1) * (_ySize + 1)];

        for (int i = 0, y = 0; y <= _ySize; y++)
        {
            for (int x = 0; x <= _xSize; x++, i++)
            {
                _vertexBuffer[i] = new Vector3(x, noiseMap[x,y] * _heightScale, y);
                _uvBuffer[i] = new Vector2(y * 1.0f / _ySize, x * 1.0f / _xSize);
            }
        }

        _mesh.vertices = _vertexBuffer;
        _mesh.uv = _uvBuffer;

        int[] _indexBuffer = new int[6 * _xSize * _ySize];
        for (int ti = 0, vi = 0, y = 0; y < _ySize; y++, vi++)
        {
            for (int x = 0; x < _xSize; x++, ti += 6, vi++)
            {
                _indexBuffer[ti] = vi;
                _indexBuffer[ti + 4] = _indexBuffer[ti + 1] = vi + _xSize + 1;
                _indexBuffer[ti + 3] = _indexBuffer[ti + 2] = vi + 1;
                _indexBuffer[ti + 5] = vi + _xSize + 2;
            }
        }

        _mesh.triangles = _indexBuffer;

        _mesh.RecalculateNormals();
        GetComponent<MeshCollider>().sharedMesh = _mesh;
    }

    /// <summary>
    /// Destroy the generated terrain
    /// </summary>
    [ContextMenu("Destroy Terrain")]
    private void DestroyTerrain()
    {
        DestroyImmediate(_mesh);
        _filter.mesh = null;
        _indexBuffer = new int[0];
        _vertexBuffer = new Vector3[0];
        
    }
}