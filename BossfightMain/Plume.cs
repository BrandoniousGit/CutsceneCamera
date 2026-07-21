using Player;
using UnityEngine;

namespace BossfightLevel.BossfightMain
{
    class Plume : MonoBehaviour
    {
        private bool plumeInitialized;
        private float timer;
        private float pulseTimer;
        private bool isShort;

        public void Init(bool isShort = false)
        {
            this.isShort = isShort;
            timer = 10;

            Debug.Log($"Plume Spawned at {transform.position}");

            plumeInitialized = true;
        }

        public void Update()
        {
            if (!plumeInitialized)
            {
                return;
            }

            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                Destroy(gameObject);
            }

            if (timer < (isShort ? 9 : 7) && timer > (isShort ? 6.5f : 0.5f))
            {
                pulseTimer += Time.deltaTime;

                if (pulseTimer > 0.2f)
                {
                    pulseTimer = 0;
                    SendPulse();
                }
            }
        }

        private void SendPulse()
        {
            var player = PlayerManager.GetLocalPlayerAgent();
            var layerMask = LayerMask.GetMask("PlayerMover");

            var colliders = Physics.OverlapSphere(new Vector3(transform.position.x, transform.position.y + 0.2f, transform.position.z), 1f, layerMask);

            if (colliders.Any())
            {
                foreach (var collider in colliders)
                {
                    if (collider.gameObject.layer == LayerManager.LAYER_PLAYER_MOVER)
                    {
                        player.Damage.NoAirDamage(0.3f);
                    }
                }
            }
        }
    }
}
