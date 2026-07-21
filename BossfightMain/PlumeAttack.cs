using AssetShards;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using FluffyUnderware.DevTools.Extensions;
using Player;
using System.Collections;
using UnityEngine;
using static BossfightLevel.BossfightMain.BossfightCore;

namespace BossfightLevel.BossfightMain
{
    class PlumeAttack : MonoBehaviour
    {
        public static event Action? OnPlumeAttackFinished;
        private GameObject firePlumePrefab;

        public void Init(PlumePattern pattern, int plumeCount, float pulseInterval, bool isShort = false)
        {
            this.isShort = isShort;
            this.pattern = pattern;
            this.plumeCount = plumeCount;
            iteration = 1;
            firePlumePrefab = AssetShardManager.GetLoadedAsset<GameObject>("Assets/-CustomStuff/CustomBossfightStuff/Attacks/FirePlume.prefab");

            StartCoroutine(WaitBetweenPulse(pulseInterval).WrapToIl2Cpp());
        }

        private bool isShort;
        private int plumeCount;
        private int iteration;
        private PlumePattern pattern;

        private void SendPulse(List<Vector3> positions)
        {
            foreach(var position in positions)
            {
                var newEffect = Instantiate(firePlumePrefab, position, Quaternion.identity);
                var plumeAttack = newEffect.AddComponent<Plume>();
                plumeAttack.Init(isShort);
            }
        }

        public IEnumerator WaitBetweenPulse(float pulseInterval)
        {
            var positions = new List<Vector3>();
            var layerMask = LayerManager.MASK_ENEMY_PROJECTILE_COLLIDERS & ~LayerMask.GetMask("PlayerSynced") & ~LayerMask.GetMask("PlayerMover");

            switch (pattern)
            {
                case PlumePattern.OnPlayers:

                    var playerPositions = PlayerManager.PlayerAgentsInLevel;

                    foreach (var player in playerPositions)
                    {
                        if (Physics.Raycast(player.PlayerCharacterController.m_characterController.bounds.center, Vector3.down, out var hitInfo, Mathf.Infinity, layerMask))
                        {
                            positions.Add(hitInfo.point);
                        }
                    }

                    SendPulse(positions);
                    break;

                case PlumePattern.CircleExpand:

                    for (int i = 0; i < 6 * iteration; i++)
                    {
                        float angle = i * (360f / (6f * iteration));
                        float radians = angle * Mathf.Deg2Rad;

                        if (Physics.Raycast(transform.position, Vector3.down, out var hitInfo, Mathf.Infinity, layerMask))
                        {
                            positions.Add(hitInfo.point + ((new Vector3(Mathf.Sin(radians), 0, Mathf.Cos(radians)) * 5 * iteration)));
                        }
                    }

                    SendPulse(positions);
                    break;                
                
                case PlumePattern.CircleExpandAlternating:

                    for (int i = 0; i < 6 * iteration; i++)
                    {
                        float angle = i * (360f / (6f * iteration));
                        float radians = angle * Mathf.Deg2Rad;

                        if (Physics.Raycast(transform.position, Vector3.down, out var hitInfo, Mathf.Infinity, layerMask))
                        {
                            positions.Add(hitInfo.point + ((new Vector3(Mathf.Sin(radians), 0, Mathf.Cos(radians)) * 5 * iteration)));
                        }
                    }

                    SendPulse(positions);
                    break;
            }

            yield return new WaitForSeconds(pulseInterval);

            if (plumeCount > 0)
            {
                plumeCount -= 1;
                iteration += 1;
                StartCoroutine(WaitBetweenPulse(pulseInterval).WrapToIl2Cpp());
            }
            else
            {
                Debug.Log("PlumeAttackFinished");

                OnPlumeAttackFinished?.Invoke();
                gameObject.Destroy();
            }
        }
    }
}
