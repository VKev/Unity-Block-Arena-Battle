// using Player;
// using Unity.Netcode;
// using UnityEngine;
//
// namespace Skill
// {
//     public class StunProjectile : NetworkBehaviour
//     {
//         public float lifeTime = 1.2f;
//
//         private void Start()
//         {
//             if (IsServer)
//             {
//                 Destroy(gameObject, lifeTime);
//             }
//         }
//
//         private void OnCollisionEnter(Collision collision)
//         {
//             if (!IsServer) return;
//
//             if (collision.gameObject.CompareTag("Player"))
//             {
//                 var playerSkillE = collision.gameObject.GetComponent<PlayerSkillE>();
//                 if (playerSkillE != null)
//                 {
//                     playerSkillE.ApplyStunServerRpc(3f); // Ví dụ 3 giây
//                 }
//             }
//
//             DespawnSelf();
//         }
//
//
//
//         private void DespawnSelf()
//         {
//             var netObj = GetComponent<NetworkObject>();
//             if (netObj != null && netObj.IsSpawned)
//                 netObj.Despawn();
//             else
//                 Destroy(gameObject);
//         }
//     }
// }