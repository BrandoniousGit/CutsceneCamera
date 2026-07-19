using AssetShards;
using Player;
using UnityEngine;

namespace BossfightLevel.BossfightMain
{
    class FireballAttack : MonoBehaviour
    {
        private float timer;
        private float pulseTimer;
        public float pulseInterval = 2;
        public float duration = 10;

        public static event Action? OnFireballAttackFinished;

        private GameObject fireballPrefab;

        public void OnEnable()
        {
            fireballPrefab = AssetShardManager.GetLoadedAsset<GameObject>("Assets/-CustomStuff/CustomBossfightStuff/Attacks/Fireball.prefab");
        }

        public void Update()
        {
            timer += Time.deltaTime;

            if (timer <= duration && pulseInterval != 0)
            {
                pulseTimer += Time.deltaTime;

                if (pulseTimer > pulseInterval)
                {
                    pulseTimer = 0;
                    SendPulse();
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void SendPulse()
        {
            Debug.Log($"Fireball spawned at {transform.position}");

            var fireball = Instantiate(fireballPrefab, transform.position, Quaternion.identity);
            fireball.AddComponent<Fireball>();
        }
    }
}
