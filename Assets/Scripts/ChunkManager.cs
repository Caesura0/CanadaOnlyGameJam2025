using Cinemachine;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    [SerializeField] List<Chunk> chunkPrefabs; 
    [SerializeField] List<Chunk> startingChunkPrefabs; 
    [SerializeField] Chunk bossChunk; 
    [SerializeField] Transform player;
    [SerializeField] int chunksToSpawn = 3;
    [SerializeField] float chunkLength = 10f;
    [SerializeField] CinemachineConfiner2D cinemachineConfiner;
    [SerializeField] PolygonCollider2D cameraConfiner2dCollider;

    public List<GameObject> activeChunks = new List<GameObject>();
    float spawnX = 0f;

    int chunksSpawned;
    bool hasSpawnedBoss = false;


    void Start()
    {
        for (int i = 0; i < 3; i++)
        {
            Chunk chunk = startingChunkPrefabs[i];
            GameObject chunkGO = Instantiate(chunk.gameObject, new Vector3(spawnX, 0, 0), Quaternion.identity);
            activeChunks.Add(chunkGO);
            spawnX += chunkLength;

            chunksSpawned++;
        }

    }



    void Update()
    {
        if (hasSpawnedBoss)
        {
            return;
        }

        if (player.position.x > spawnX - (chunksToSpawn * chunkLength))
        {
            SpawnChunk();
            RemoveOldChunk();
            UpdateBoundary();
        }

    }
    

    void SpawnChunk()
    {
        Chunk chunk = ChooseChunkToSpawn();

        GameObject chunkGO = Instantiate(chunk.gameObject, new Vector3(spawnX, 0, 0), Quaternion.identity);
        activeChunks.Add(chunkGO);
        spawnX += chunkLength;
        //cameraConfiner2dCollider.offset = new Vector2(spawnX / 2, 0);
        chunksSpawned++;
    }

    Chunk ChooseChunkToSpawn()
    {
        if(chunksSpawned >= 10)
        {
            Chunk chunk = SpawnBossChunk();
            return chunk;
        }
        int chunkIndex = Random.Range(0, chunkPrefabs.Count);
        return chunkPrefabs[chunkIndex];
    }

    private Chunk SpawnBossChunk()
    {

        hasSpawnedBoss = true;
        return bossChunk;
    }

    void RemoveOldChunk()
    {
        if (activeChunks.Count > chunksToSpawn +2 )
        {
            Destroy(activeChunks[0]);
            activeChunks.RemoveAt(0);
            //UpdateBoundary(); 
        }
    }



    void UpdateBoundary()
    {
        if (activeChunks.Count > 0)
        {
            // Get the first chunk and activate its back wall
            Chunk firstChunk = activeChunks[0].GetComponent<Chunk>();
            if (firstChunk != null)
            {
                firstChunk.SetBoundaryActive(true);


                Debug.Log("Updating boundary : " + chunkLength + "  offset: " + cameraConfiner2dCollider.offset);
                cameraConfiner2dCollider.offset += new Vector2(chunkLength, 0);
                // Update Cinemachine Confiner
                //cinemachineConfiner.m_BoundingShape2D = firstChunk.GetBoundaryCollider();
                //cinemachineConfiner.InvalidateCache(); // Refresh the confiner
            }
        }
    }
}


