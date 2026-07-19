using Player;
using UnityEngine;

namespace BossfightLevel.BossfightMain
{
    class PlumeAttack : MonoBehaviour
    {
        private float timer;
        private float pulseTimer;

        public bool isShort;

        private float maxTimer => isShort ? 4 : 11;

        public static event Action? OnPlumeAttackFinished;

        public void Update()
        {
            timer += Time.deltaTime;

            if (timer >= maxTimer)
            {
                Destroy(gameObject);
            }

            if (timer > (isShort ? 1 : 3) && timer < (isShort ? 9.5f : 3.5f))
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

            var colliders = Physics.OverlapSphere(new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z), 0.6f, layerMask);

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
