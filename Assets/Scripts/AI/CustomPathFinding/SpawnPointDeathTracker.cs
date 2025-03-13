using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPointDeathTracker : MonoBehaviour
{
    public EnemySpawnPoint spawnPoint;

    private void OnDestroy()
    {
        if (spawnPoint != null)
        {
            spawnPoint.OnEnemyDestroyed();
        }
    }
}
