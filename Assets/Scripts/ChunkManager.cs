using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    [SerializeField] List<Chunk> chunkPrefabs; 
    [SerializeField] Transform player;
    [SerializeField] int chunksToSpawn = 3;
    [SerializeField] float chunkLength = 10f;

    private List<GameObject> activeChunks = new List<GameObject>();
    private float spawnZ = 0f;
    private float safeZone = 15f;

    void Start()
    {
        for (int i = 0; i < chunksToSpawn; i++)
        {
            SpawnChunk();
        }
    }

    void Update()
    {
        if (player.position.y > spawnZ - (chunksToSpawn * chunkLength))
        {
            SpawnChunk();
            RemoveOldChunk();
        }
    }

    void SpawnChunk()
    {
        int chunkIndex = Random.Range(0, chunkPrefabs.Count);
        GameObject chunk = Instantiate(chunkPrefabs[chunkIndex].gameObject, new Vector3(0, spawnZ, 0), Quaternion.identity);
        activeChunks.Add(chunk);
        spawnZ += chunkLength;
    }

    void RemoveOldChunk()
    {
        if (activeChunks.Count > chunksToSpawn)
        {
            Destroy(activeChunks[0]);
            activeChunks.RemoveAt(0);
        }
    }
}
