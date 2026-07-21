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
        private Light pointLight;

        public void Init(bool isShort = false)
        {
            this.isShort = isShort;
            timer = 10;
            pointLight = GetComponentInChildren<Light>();

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

            if (timer < 1)
            {
                pointLight.intensity = Mathf.Lerp(pointLight.intensity, 0, 0.1f);
                pointLight.range = Mathf.Lerp(pointLight.range, 0, 0.1f);
            }
        }

        private void SendPulse()
        {
            var layerMask = LayerMask.GetMask("PlayerMover");

            var colliders = Physics.OverlapSphere(new Vector3(transform.position.x, transform.position.y + 0.2f, transform.position.z), 1f, layerMask);

            if (colliders.Any())
            {
                foreach (var collider in colliders)
                {
                    if (collider.gameObject.layer == LayerManager.LAYER_PLAYER_MOVER)
                    {
                        var player = collider.GetComponent<PlayerAgent>();
                        player.Damage.NoAirDamage(0.3f);
                    }
                }
            }
        }
    }
}
