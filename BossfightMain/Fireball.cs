using AirNavigation;
using AssetShards;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Il2CppSystem.Globalization.CultureInfo;

namespace BossfightLevel.BossfightMain
{
    class Fireball : MonoBehaviour
    {
        private float timer;
        private Vector3 previousPos;
        private GameObject explosionPrefab;
        private LayerMask layerMask;
        private LayerMask hitLayerMask;

        private Vector3 target;
        private Vector3 direction;
        private PlayerAgent player;

        public void OnEnable()
        {
            explosionPrefab = AssetShardManager.GetLoadedAsset<GameObject>("Assets/-CustomStuff/CustomBossfightStuff/Attacks/Explosion.prefab");
            layerMask = LayerManager.MASK_ENEMY_PROJECTILE_COLLIDERS & ~LayerMask.GetMask("PlayerSynced");
            hitLayerMask = LayerMask.GetMask("PlayerMover");

            var random = UnityEngine.Random.Range(0, PlayerManager.PlayerAgentsInLevel.Count - 1);

            player = PlayerManager.PlayerAgentsInLevel[random];
            target = player.PlayerCharacterController.m_characterController.bounds.center;

            previousPos = transform.position;
        }

        public void Update()
        {
            timer += Time.deltaTime;

            if (timer >= 10)
            {
                Destroy(gameObject);
            }

            if (timer > 1)
            {
                transform.position += direction;
            }
            else
            {
                direction = (target - transform.position).normalized * 25 * Time.deltaTime;
            }

            Vector3 currentPosition = transform.position;
            Vector3 delta = currentPosition - previousPos;

            if (Physics.Raycast(previousPos, delta.normalized, out RaycastHit hit, delta.magnitude, layerMask))
            {
                var explosion = Instantiate(explosionPrefab, hit.point, Quaternion.identity);
                explosion.AddComponent<DespawnEffect>();

                var cellsoundplayer = new CellSoundPlayer();
                cellsoundplayer.Post(704948356u, hit.point);

                if (Physics.Raycast(previousPos, player.PlayerCharacterController.m_characterController.bounds.center - previousPos, out var hitInfo, 3f, hitLayerMask))
                {
                    if (hitInfo.collider.gameObject.layer == LayerManager.LAYER_PLAYER_MOVER)
                    {
                        player.Damage.NoAirDamage(4.8f);
                    }
                }

                Destroy(gameObject);
            }

            previousPos = currentPosition;
        }
    }
}
