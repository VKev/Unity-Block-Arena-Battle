using System.Collections;
using System.Collections.Generic;
using Player;
using Unity.Netcode;
using UnityEngine;

namespace Skill
{
    public class OrbSpawner : NetworkBehaviour
    {
        public List<GameObject> orbPrefabs;
        private List<GameObject> spawnedOrbs = new List<GameObject>();
        public int maxOrbsOnMap = 10;

        [Header("Spawn Settings")] public Vector3 centerSpawnPoint = new Vector3(0f, 5f, 0f);
        public float spawnRadiusXZ = 30f;
        public float spawnOffsetYMin = 5f;
        public float spawnOffsetYMax = 10f;

        public float spawnCheckRadius = 0.5f;
        public LayerMask obstacleLayerMask;

        public Vector3 spawnAreaMin;
        public Vector3 spawnAreaMax;

        private Coroutine spawnRoutine;

        private void Awake()
        {
            CalculateSpawnBounds();
        }

        private void CalculateSpawnBounds()
        {
            spawnAreaMin.x = centerSpawnPoint.x - spawnRadiusXZ;
            spawnAreaMax.x = centerSpawnPoint.x + spawnRadiusXZ;

            spawnAreaMin.z = centerSpawnPoint.z - spawnRadiusXZ;
            spawnAreaMax.z = centerSpawnPoint.z + spawnRadiusXZ;

            spawnAreaMin.y = centerSpawnPoint.y + spawnOffsetYMin;
            spawnAreaMax.y = centerSpawnPoint.y + spawnOffsetYMax;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                GameEvents.OnOrbSpawnRequested += StartOrbSpawning;
                GameEvents.OnStopOrbSpawnRequested += StopAndClearOrbs;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                GameEvents.OnOrbSpawnRequested -= StartOrbSpawning;
                GameEvents.OnStopOrbSpawnRequested -= StopAndClearOrbs;
            }
        }

        public void StopAndClearOrbs()
        {
            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }

            for (int i = spawnedOrbs.Count - 1; i >= 0; i--)
            {
                var orb = spawnedOrbs[i];
                if (orb != null)
                {
                    var netObj = orb.GetComponent<NetworkObject>();
                    if (netObj != null && netObj.IsSpawned)
                    {
                        netObj.Despawn();
                    }
                    else
                    {
                        Destroy(orb);
                    }
                }
            }
            foreach (var player in FindObjectsOfType<PlayerSkillE>())
            {
                player.ClearAllOrbs();
            }

            spawnedOrbs.Clear();
            
        }
        
        


        private void StartOrbSpawning()
        {
            if (spawnRoutine == null)
            {
                spawnRoutine = StartCoroutine(SpawnOrbRoutine());
            }
        }

        private IEnumerator SpawnOrbRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(2f);

                if (GetTotalOrbCount() < maxOrbsOnMap)
                {
                    SpawnOrb();
                }
            }
        }

        void SpawnOrb()
        {
            if (orbPrefabs.Count == 0) return;
            if (spawnedOrbs.Count >= maxOrbsOnMap) return;

            int orbIndex = Random.Range(0, orbPrefabs.Count);
            Vector3 spawnPos = Vector3.zero;
            bool canSpawn = false;

            for (int i = 0; i < 20; i++)
            {
                float randomX = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
                float randomZ = Random.Range(spawnAreaMin.z, spawnAreaMax.z);
                float randomY = Random.Range(spawnAreaMin.y, spawnAreaMax.y);

                spawnPos = new Vector3(randomX, randomY, randomZ);

                if (!Physics.CheckSphere(spawnPos, spawnCheckRadius, obstacleLayerMask))
                {
                    canSpawn = true;
                    break;
                }
            }

            if (!canSpawn)
            {
                Debug.LogWarning("Không tìm được vị trí spawn orb hợp lệ.");
                return;
            }

            GameObject orbInstance = Instantiate(orbPrefabs[orbIndex], spawnPos, Quaternion.identity);

            NetworkObject netObj = orbInstance.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Debug.LogError("Orb prefab thiếu NetworkObject component!");
                Destroy(orbInstance);
                return;
            }

            netObj.Spawn();

            spawnedOrbs.Add(orbInstance);

            OrbPickup orbPickup = orbInstance.GetComponent<OrbPickup>();
            if (orbPickup != null)
            {
                orbPickup.OnDespawned += () => { spawnedOrbs.Remove(orbInstance); };
            }

            Rigidbody rb = orbInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                StartCoroutine(CheckAndStopAtGround(rb));
            }
        }

        private int GetTotalOrbCount()
        {
            int total = spawnedOrbs.Count;

            var players = FindObjectsOfType<Player.PlayerSkillE>();
            foreach (var player in players)
            {
                total += player.GetOrbCount();
            }

            return total;
        }

        private IEnumerator CheckAndStopAtGround(Rigidbody rb)
        {
            if (Terrain.activeTerrain == null)
            {
                Debug.LogWarning("Không tìm thấy Terrain!");
                yield break;
            }

            Terrain terrain = Terrain.activeTerrain;

            while (rb != null)
            {
                float groundY = terrain.SampleHeight(rb.transform.position);

                if (rb.transform.position.y <= groundY + 0.01f)
                {
                    rb.isKinematic = true;

                    Vector3 finalPos = rb.transform.position;
                    finalPos.y = groundY;
                    rb.transform.position = finalPos;
                    yield break;
                }

                yield return null;
            }
        }
    }
}