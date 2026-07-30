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

        private Vector3? target;
        private Vector3 direction;
        private PlayerAgent playerTarget;
        private bool targetOverriden;

        public void Init(int playerIndex, Vector3? targetOverride = null)
        {
            explosionPrefab = AssetShardManager.GetLoadedAsset<GameObject>("Assets/-CustomStuff/CustomBossfightStuff/Attacks/Explosion.prefab");
            layerMask = LayerManager.MASK_ENEMY_PROJECTILE_COLLIDERS & ~LayerMask.GetMask("PlayerSynced");

            if (targetOverride == null)
            {
                playerTarget = PlayerManager.PlayerAgentsInLevel[playerIndex];
            }
            else
            {
                target = targetOverride.Value;
                direction = (target - transform.position).Value.normalized * 40 * Time.deltaTime;
                transform.localScale *= 4;
                targetOverriden = true;
            }

            previousPos = transform.position;
        }

        public void Update()
        {
            timer += Time.deltaTime;

            if (timer >= 25)
            {
                Destroy(gameObject);
            }

            if (timer > 1)
            {
                transform.position += direction;
            }
            else if (!targetOverriden)
            {
                var layerMask = LayerManager.MASK_ENEMY_PROJECTILE_COLLIDERS & ~LayerMask.GetMask("PlayerSynced") & ~LayerMask.GetMask("PlayerMover");

                if (Physics.Raycast(playerTarget.transform.position + Vector3.up * 0.05f, Vector3.down, out var hitInfo, Mathf.Infinity, layerMask))
                {
                    target = hitInfo.point;
                    direction = (target - transform.position).Value.normalized * 20 * Time.deltaTime;
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

                if (Physics.Raycast(previousPos, PlayerManager.GetLocalPlayerAgent().transform.position - previousPos, out var hitInfo, targetOverriden ? 12f : 3.5f, layerMask))
                {
                    if (hitInfo.collider.gameObject.layer == LayerManager.LAYER_PLAYER_MOVER)
                    {
                        PlayerManager.GetLocalPlayerAgent().Damage.NoAirDamage(targetOverriden ? 8f : 5f);
                    }
                }

                Destroy(gameObject);
            }

            previousPos = currentPosition;
        }
    }
}
