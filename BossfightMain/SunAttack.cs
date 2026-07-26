using Player;
using SNetwork;
using UnityEngine;

namespace BossfightLevel.BossfightMain
{
    class SunAttack : MonoBehaviour
    {
        private Light sunlight;
        private float timer;
        private float maxDuration;
        private float pulseTimer;
        private float maxSizeOverTime;
        private float startSize;
        private bool initialized;
        private bool doesDamage;

        public static event Action? OnSunAttackFinished;

        public void Init(float duration, float maxSizeOverTime = 1.5f, float startSize = 0.05f, bool doesDamage = true)
        {
            timer = duration;
            maxDuration = duration;
            this.doesDamage = doesDamage;
            this.startSize = startSize;
            this.maxSizeOverTime = maxSizeOverTime;
            initialized = true;
        }

        public void Update()
        {
            if (!initialized)
            {
                return;
            }

            if (sunlight == null)
            {
                sunlight = GetComponentInChildren<Light>();
                return;
            }

            timer -= Time.deltaTime;

            if (timer > 0)
            {
                sunlight.intensity = Mathf.Lerp(0.2f, 0.9f, 1 - (timer / maxDuration));
                sunlight.range = Mathf.Lerp(0.5f, transform.localScale.x * 50, 1 - (timer / maxDuration));
                transform.localScale = Vector3.Lerp(Vector3.one * startSize, Vector3.one * maxSizeOverTime, 1 - (timer / maxDuration));
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
            if (!doesDamage)
            {
                return;
            }

            var player = PlayerManager.GetLocalPlayerAgent();
            var layerMask = LayerManager.MASK_ENEMY_PROJECTILE_COLLIDERS & ~LayerMask.GetMask("PlayerSynced");

            if (Physics.Raycast(transform.position, player.PlayerCharacterController.m_characterController.bounds.center - transform.position, out var hitInfo, Mathf.Infinity, layerMask))
            {
                if (hitInfo.collider.gameObject.layer == LayerManager.LAYER_PLAYER_MOVER)
                {
                    player.Damage.NoAirDamage(1.25f * (1 + (1 - (timer / 10))));
                }
            }
        }
    }
}
