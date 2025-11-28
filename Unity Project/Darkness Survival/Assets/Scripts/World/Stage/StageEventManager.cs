using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageEventManager : MonoBehaviour
{
    [SerializeField] StageData stageData;
    [SerializeField] EnemiesManager enemiesManager;
    [Header("Object Spawn Settings")]
    [SerializeField] bool spawnObjectsOffScreen = true;
    [SerializeField] float objectOffScreenBuffer = 2f;
    [SerializeField] float objectSpawnRingThickness = 3f;
    [SerializeField] float objectMinSeparation = 1.5f;
    [SerializeField] int objectSpawnMaxAttempts = 12;

    StageTimer stageTimer;
    int eventIndexer;

    PlayerWinManager playerWin;

    private void Awake()
    {
        stageTimer = GetComponent<StageTimer>();
    }

    private void Start()
    {
        playerWin = FindObjectOfType<PlayerWinManager>();
    }

    private void Update()
    {
        if (eventIndexer >= stageData.stageEvents.Count) { return; }

        if (stageTimer.time > stageData.stageEvents[eventIndexer].time)
        {
            switch (stageData.stageEvents[eventIndexer].eventType)
            {
                case StageEventType.SpawnEnemy:
                    SpawnEnemy();
                    break;

                case StageEventType.SpawnObject:
                    SpawnObject();
                    break;

                case StageEventType.WinStage:
                    WinStage();
                    break;

                case StageEventType.SpawnEnemyBoss:
                    SpawnEnemyBoss();
                    break;
            }

            Debug.Log(stageData.stageEvents[eventIndexer].message);
            eventIndexer += 1;
        }
    }

    private void SpawnEnemyBoss()
    {
        for (int i = 0; i < stageData.stageEvents[eventIndexer].count; ++i)
        {
            enemiesManager.SpawnBoss(stageData.stageEvents[eventIndexer].bossToSpawn);
        }
    }

    private void WinStage()
    {
        playerWin.Win();
    }

    private void SpawnEnemy()
    {
        for (int i = 0; i < stageData.stageEvents[eventIndexer].count; ++i)
        {
            enemiesManager.SpawnEnemy(stageData.stageEvents[eventIndexer].monstersToSpawn);
        }
    }

    private void SpawnObject()
    {
        List<Vector3> generatedPositions = new List<Vector3>();
        for (int i = 0; i < stageData.stageEvents[eventIndexer].count; ++i)
        {
            Vector3 positionToSpawn = spawnObjectsOffScreen
                ? GenerateOffScreenSpawnPosition(generatedPositions)
                : GameManager.instance.playerTransform.position + UtilityTools.GenerateRandomPositionSquarePattern(new Vector2(5f, 5f), Vector2.zero);

            generatedPositions.Add(positionToSpawn);

            SpawnManager.instance.SpawnObject(
                positionToSpawn,
                stageData.stageEvents[eventIndexer].objectToSpawn
                );
        }
    }

    Vector3 GenerateOffScreenSpawnPosition(List<Vector3> existingPositions)
    {
        Transform playerTransform = GameManager.instance.playerTransform;
        Vector3 playerPosition = playerTransform != null ? playerTransform.position : Vector3.zero;

        Camera mainCamera = Camera.main;
        float verticalExtent = mainCamera != null ? mainCamera.orthographicSize : 5f;
        float horizontalExtent = mainCamera != null ? mainCamera.orthographicSize * mainCamera.aspect : 5f;
        float minDistance = Mathf.Max(horizontalExtent, verticalExtent) + objectOffScreenBuffer;
        float maxDistance = minDistance + objectSpawnRingThickness;

        Vector3 candidate = playerPosition;
        for (int attempt = 0; attempt < objectSpawnMaxAttempts; attempt++)
        {
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float distance = UnityEngine.Random.Range(minDistance, maxDistance);
            candidate = playerPosition + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * distance;

            bool overlaps = false;
            foreach (Vector3 existing in existingPositions)
            {
                if (Vector2.Distance(existing, candidate) < objectMinSeparation)
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                return candidate;
            }
        }

        return candidate;
    }
}
