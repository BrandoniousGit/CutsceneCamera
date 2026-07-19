using Player;
using SNetwork;
using UnityEngine;

namespace BossfightLevel.BossfightMain
{
    class SunAttack : MonoBehaviour
    {
        private Light sunlight;
        private float timer;
        private float pulseTimer;

        public static event Action? OnSunAttackFinished;

        public void OnEnable()
        {
            timer = 10;
        }

        public void Update()
        {
            if (sunlight == null)
            {
                sunlight = GetComponentInChildren<Light>();
                return;
            }

            timer -= Time.deltaTime;

            if (timer > 0)
            {
                sunlight.intensity = Mathf.Lerp(0.2f, 1.5f, 1 - (timer / 10));
                sunlight.range = Mathf.Lerp(0.5f, 75, 1 - (timer / 10));
                transform.localScale = Vector3.Lerp(Vector3.one * 0.05f, Vector3.one * 2f, 1 - (timer / 10));
            }

            if (timer < 6 && timer > -4)
            {
                pulseTimer += Time.deltaTime;

                if (pulseTimer > 0.66f)
                {
                    pulseTimer = 0;
                    SendPulse();
                }
            }

            if (timer <= -4)
            {
                sunlight.intensity = Mathf.Lerp(sunlight.intensity, 0, 0.05f);
                sunlight.range = Mathf.Lerp(sunlight.range, 0, 0.05f);
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 0.05f);

                if (timer <= -5)
                {
                    OnSunAttackFinished?.Invoke();
                    Destroy(gameObject);
                }
            }
        }

        private void SendPulse()
        {
            var player = PlayerManager.GetLocalPlayerAgent();
            var layerMask = LayerManager.MASK_ENEMY_PROJECTILE_COLLIDERS & ~LayerMask.GetMask("PlayerSynced");

            if (Physics.Raycast(transform.position, player.PlayerCharacterController.m_characterController.bounds.center - transform.position, out var hitInfo, Mathf.Infinity, layerMask))
            {
                if (hitInfo.collider.gameObject.layer == LayerManager.LAYER_PLAYER_MOVER)
                {
                    player.Damage.NoAirDamage(1.2f * (1 + (1 - (timer / 10))));
                }
            }
        }
    }
}
