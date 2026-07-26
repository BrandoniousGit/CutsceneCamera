using AssetShards;
using GTFO.API;
using Player;
using SNetwork;
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
        public static FireballAttack Instance { get; private set; }

        private GameObject fireballPrefab;

        public void OnEnable()
        {
            Instance = this;
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
                OnFireballAttackFinished?.Invoke();
                Destroy(gameObject);
            }
        }

        private void SendPulse()
        {
            if (!SNet.IsMaster)
            {
                return;
            }

            var random = UnityEngine.Random.Range(0, PlayerManager.PlayerAgentsInLevel.Count);

            NetworkAPI.InvokeEvent<int>("OnFireballTargetSet", random);

            Debug.Log($"Fireball spawned at {transform.position}");

            var fireball = Instantiate(fireballPrefab, transform.position, Quaternion.identity);
            var fireballComponent = fireball.AddComponent<Fireball>();
            fireballComponent.Init(random);
        }

        public static void OnFireballTargetSet(ulong senderID, int value)
        {
            Instance?.SendNetworkedPulse(value);
        }

        private void SendNetworkedPulse(int playerIndex)
        {
            if (SNet.IsMaster)
            {
                return;
            }

            Debug.Log($"Fireball spawned at {transform.position}");

            var fireball = Instantiate(fireballPrefab, transform.position, Quaternion.identity);
            var fireballComponent = fireball.AddComponent<Fireball>();
            fireballComponent.Init(playerIndex);
        }
    }
}
