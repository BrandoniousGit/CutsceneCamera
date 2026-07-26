using AssetShards;
using Player;
using SNetwork;
using UnityEngine;

namespace BossfightLevel.BossfightMain
{
    class Fireball : MonoBehaviour
    {
        public static Fireball Instance { get; private set; }

        private float timer;
        private Vector3 previousPos;
        private GameObject explosionPrefab;
        private LayerMask layerMask;

        private Vector3 target;
        private Vector3 direction;
        private PlayerAgent playerTarget;

        public void Init(int playerIndex)
        {
            explosionPrefab = AssetShardManager.GetLoadedAsset<GameObject>("Assets/-CustomStuff/CustomBossfightStuff/Attacks/Explosion.prefab");
            layerMask = LayerManager.MASK_ENEMY_PROJECTILE_COLLIDERS & ~LayerMask.GetMask("PlayerSynced");
            playerTarget = PlayerManager.PlayerAgentsInLevel[playerIndex];

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
                var layerMask = LayerManager.MASK_ENEMY_PROJECTILE_COLLIDERS & ~LayerMask.GetMask("PlayerSynced") & ~LayerMask.GetMask("PlayerMover");

                if (Physics.Raycast(playerTarget.transform.position, Vector3.down, out var hitInfo, Mathf.Infinity, layerMask))
                {
                    target = hitInfo.point;
                    direction = (target - transform.position).normalized * 20 * Time.deltaTime;
                }
            }

            Vector3 currentPosition = transform.position;
            Vector3 delta = currentPosition - previousPos;

            if (Physics.Raycast(previousPos, delta.normalized, out RaycastHit hit, delta.magnitude, layerMask))
            {
                var explosion = Instantiate(explosionPrefab, hit.point, Quaternion.identity);
                explosion.AddComponent<DespawnEffect>();

                var cellsoundplayer = new CellSoundPlayer();
                cellsoundplayer.Post(704948356u, hit.point);

                if (Physics.Raycast(previousPos, playerTarget.transform.position - previousPos, out var hitInfo, 3f, layerMask))
                {
                    if (hitInfo.collider.gameObject.layer == LayerManager.LAYER_PLAYER_MOVER)
                    {
                        playerTarget.Damage.NoAirDamage(5f);
                    }
                }

                Destroy(gameObject);
            }

            previousPos = currentPosition;
        }
    }
}
