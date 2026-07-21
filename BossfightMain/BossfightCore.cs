using Agents;
using AIGraph;
using AssetShards;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Enemies;
using Player;
using StrikerBossfight.BossfightMain;
using System.Collections;
using UnityEngine;
using static RootMotion.FinalIK.IKSolverVR;

namespace BossfightLevel.BossfightMain
{
    class BossfightCore : MonoBehaviourExtended
    {
        public enum PlumePattern
        {
            OnPlayers,
            CircleExpand,
            CircleExpandAlternating,
            Spiral
        }

        private EnemyAgent selectedEnemy;
        private Animator enemyAnim;

        private GameObject sunPrefab;
        private GameObject preFireballEffectPrefab;
        private GameObject flameAuraPrefab;
        private GameObject firePlumeShortPrefab;

        private AudioSource escapeMusic;
        private AudioSource musicLooper;
        private AudioSource musicTransitioner;

        private List<AudioClip> audioClips;
        private AudioClip spotlightSfx;

        private int currentMusicStep;
        private bool fadeMusicLooperOut;
        private bool enteredPhase2;
        private bool enteredPhase3;
        private bool enteredFinal;

        private bool introStarted;
        private bool introFinished;

        private bool canAttack;
        private bool isOnCooldown;
        private float attackCooldown;
        private float targetHeight = 2;

        public void OnEnable()
        {
            AnimationEventReceiver.PunchEventTriggered += OnPunch;
            SunAttack.OnSunAttackFinished += GoToIdleFloating;
            FireballAttack.OnFireballAttackFinished += GoToIdleFloating;
            PlumeAttack.OnPlumeAttackFinished += GoToIdleFloating;
        }        
        
        public void OnDisable()
        {
            AnimationEventReceiver.PunchEventTriggered -= OnPunch;
            SunAttack.OnSunAttackFinished -= GoToIdleFloating;
            FireballAttack.OnFireballAttackFinished -= GoToIdleFloating;
            PlumeAttack.OnPlumeAttackFinished -= GoToIdleFloating;
        }

        public void OnApplicationFocus(bool hasFocus)
        {
            if (escapeMusic != null)
            {
                escapeMusic.mute = !hasFocus;
                musicLooper.mute = !hasFocus;
                musicTransitioner.mute = !hasFocus;
            }
        }

        void OnVolumeChanged(float value)
        {
            escapeMusic.volume = value;
            musicLooper.volume = value;
            musicTransitioner.volume = value;
        }

        public void ProgressMusic()
        {
            currentMusicStep += 1;

            if (currentMusicStep == 6)
            {
                fadeMusicLooperOut = true;
                StartCoroutine(WaitForFinalMusic(6f).WrapToIl2Cpp());
                return;
            }

            Debug.Log($"Current music step is {currentMusicStep}, playing {audioClips[currentMusicStep].name}!");

            if (currentMusicStep % 2 == 0)
            {
                fadeMusicLooperOut = true;
                musicTransitioner.clip = audioClips[currentMusicStep];
                musicTransitioner.Play();

                StartCoroutine(WaitThenStop(audioClips[currentMusicStep].length).WrapToIl2Cpp());
            }
            else
            {
                musicLooper.volume = musicTransitioner.volume;
                musicLooper.clip = audioClips[currentMusicStep];
                musicLooper.Play();
            }
        }

        public void StopAndResetMusic()
        {
            currentMusicStep = -1;
            musicLooper.Stop();
            musicTransitioner.Stop();
        }

        public void Update()
        {
            if (GameStateManager.CurrentStateName == eGameStateName.ExpeditionFail)
            {
                StopAndResetMusic();
            }

            if (!RundownManager.ExpeditionIsStarted)
            {
                return;
            }

            if (fadeMusicLooperOut)
            {
                if (musicLooper.volume > 0)
                {
                    musicLooper.volume -= Time.deltaTime * 0.2f;
                }
                else
                {
                    musicLooper.Stop();
                    fadeMusicLooperOut = false;
                }
            }

            if (selectedEnemy == null || !selectedEnemy.Alive)
            {
                canAttack = false;

                var enemyList = AIG_CourseGraph.GetReachableEnemiesInNodes(PlayerManager.GetLocalPlayerAgent().CourseNode, 100);

                foreach (var enemy in enemyList)
                {
                    if (enemy.EnemyData.persistentID == 150u)
                    {
                        selectedEnemy = enemy;
                        selectedEnemy.EnemyBalancingData.Health.ArmorDamageMulti = 0;
                        selectedEnemy.EnemyBalancingData.Health.WeakspotDamageMulti = 0;
                        targetHeight = selectedEnemy.transform.position.y;
                    }
                }

                if (selectedEnemy != null)
                {
                    var runtimeAnimController = AssetShardManager.GetLoadedAsset<RuntimeAnimatorController>("Assets/-CustomStuff/BuildThisOne/fungus.controller");
                    enemyAnim = selectedEnemy.Anim;
                    enemyAnim.runtimeAnimatorController = runtimeAnimController;

                    enemyAnim.SetTrigger("GoToSitting");

                    var eventReceiver = selectedEnemy.gameObject.AddComponent<AnimationEventReceiver>();
                }
                return;
            }
            
            selectedEnemy.Locomotion.m_maxMovementSpeed = 0;
            selectedEnemy.transform.position = new Vector3(selectedEnemy.transform.position.x, targetHeight, selectedEnemy.transform.position.z);

            if (!introStarted)
            {
                StartCoroutine(PerformIntro().WrapToIl2Cpp());
            }

            if (!introFinished)
            {
                return;
            }

            if (selectedEnemy.Damage.Health < 600 && !enteredPhase2)
            {
                enteredPhase2 = true;
                ProgressMusic();
            }            
            
            if (selectedEnemy.Damage.Health < 300 && !enteredPhase3)
            {
                enteredPhase3 = true;
                ProgressMusic();
            }

            if (canAttack)
            {
                Attack();
            }
            else
            {
                if (isOnCooldown && attackCooldown > 0)
                {
                    attackCooldown -= Time.deltaTime;
                }
                else if (attackCooldown < 0)
                {
                    isOnCooldown = false;
                    canAttack = true;
                }
            }      
        }

        public void OnPunch()
        {
            Debug.Log("PunchPerformed");
        }

        public void Attack()
        {
            canAttack = false;
            enemyAnim.SetTrigger("PraiseSun");

            var random = UnityEngine.Random.Range(0, 3);
            var random2 = UnityEngine.Random.Range(0, 3);

            switch (random)
            {
                case 0:
                    SpawnSunAttack();
                    break;
                case 1:
                    SpawnFireballAttack(10, 0.8f);
                    break;
                case 2:
                    SpawnFirePlumeAttacks((PlumePattern)random2, 5, 1f);
                    break;
            }
        }

        internal void LevelStarted()
        {
            BossfightPatches.OnVolumeChangedAction += OnVolumeChanged;

            sunPrefab = AssetShardManager.GetLoadedAsset<GameObject>("Assets/-CustomStuff/CustomBossfightStuff/Attacks/SunAttack.prefab");
            firePlumeShortPrefab = AssetShardManager.GetLoadedAsset<GameObject>("Assets/-CustomStuff/CustomBossfightStuff/Attacks/FirePlumeShort.prefab");
            preFireballEffectPrefab = AssetShardManager.GetLoadedAsset<GameObject>("Assets/-CustomStuff/CustomBossfightStuff/Attacks/PreFireball.prefab");
            flameAuraPrefab = AssetShardManager.GetLoadedAsset<GameObject>("Assets/-CustomStuff/CustomBossfightStuff/Attacks/FlameAura.prefab");

            Debug.Log("Assets Loaded");

            LoadAudio();
        }

        internal void LevelQuit()
        {
            BossfightPatches.OnVolumeChangedAction -= OnVolumeChanged;
            StopAndResetMusic();

            currentMusicStep = -1;
            introStarted = false;
            introFinished = false;
            enteredPhase2 = false;
            enteredPhase3 = false;

            Destroy(escapeMusic);
            Destroy(musicLooper);
            Destroy(musicTransitioner);
        }

        public void LoadAudio()
        {
            audioClips = new List<AudioClip>();

            escapeMusic = gameObject.AddComponent<AudioSource>();
            musicLooper = gameObject.AddComponent<AudioSource>();
            musicTransitioner = gameObject.AddComponent<AudioSource>();

            escapeMusic.loop = false;
            musicLooper.loop = true;
            musicTransitioner.loop = false;            
            
            escapeMusic.volume = 0.35f;
            musicLooper.volume = 0.35f;
            musicTransitioner.volume = 0.35f;

            audioClips.Add(AssetShardManager.GetLoadedAsset<AudioClip>($"Assets/-CustomStuff/Music/Opening.ogg"));
            audioClips.Add(AssetShardManager.GetLoadedAsset<AudioClip>($"Assets/-CustomStuff/Music/Phase1Loop.ogg"));
            audioClips.Add(AssetShardManager.GetLoadedAsset<AudioClip>($"Assets/-CustomStuff/Music/Phase2Transition.ogg"));
            audioClips.Add(AssetShardManager.GetLoadedAsset<AudioClip>($"Assets/-CustomStuff/Music/Phase2Loop.ogg"));
            audioClips.Add(AssetShardManager.GetLoadedAsset<AudioClip>($"Assets/-CustomStuff/Music/Phase3Transition.ogg"));
            audioClips.Add(AssetShardManager.GetLoadedAsset<AudioClip>($"Assets/-CustomStuff/Music/Phase3Loop.ogg"));
            audioClips.Add(AssetShardManager.GetLoadedAsset<AudioClip>($"Assets/-CustomStuff/Music/FinalDesperation.ogg"));

            Debug.Log("Audio Loaded");
        }

        #region attacks
        public void SpawnFireballAttack(float duration, float pulseInterval = 2)
        {
            attackCooldown = 8;

            var newEffect = Instantiate(preFireballEffectPrefab, Vector3.zero, Quaternion.identity);
            var fireballAttack = newEffect.AddComponent<FireballAttack>();
            fireballAttack.transform.position += selectedEnemy.transform.position + (Vector3.up * 4f);
            fireballAttack.pulseInterval = pulseInterval;
            fireballAttack.duration = duration;
        }

        public void SpawnSunAttack()
        {
            attackCooldown = 8;

            var newEffect = Instantiate(sunPrefab, Vector3.zero, Quaternion.identity);
            newEffect.transform.position += selectedEnemy.transform.position + (Vector3.up * 10f);
            newEffect.AddComponent<SunAttack>();
        }    
        
        public void SpawnFirePlumeAttacks(PlumePattern pattern, int count = 1, float pulseInterval = 1f, bool isShort = false)
        {
            attackCooldown = 12;

            var newEffect = Instantiate(flameAuraPrefab, selectedEnemy.transform.position, Quaternion.identity);
            var plumeAttack = newEffect.AddComponent<PlumeAttack>();
            plumeAttack.Init(pattern, count, pulseInterval, isShort);
        }
        #endregion

        #region states
        public void GoToIdleFloating()
        {
            if (selectedEnemy != null && selectedEnemy.Alive)
            {
                enemyAnim.SetTrigger("GoToIdleFloating");
                isOnCooldown = true;
            }
        }
        #endregion

        private void PlayFinalMusic()
        {
            musicTransitioner.clip = audioClips[6];
        }

        public IEnumerator WaitThenStop(float timeToWait)
        {
            yield return new WaitForSeconds(timeToWait);
            ProgressMusic();
        }        
        
        public IEnumerator WaitForFinalMusic(float timeToWait)
        {
            yield return new WaitForSeconds(timeToWait);
            PlayFinalMusic();
        }        
        
        public IEnumerator PerformIntro()
        {
            introStarted = true;

            enemyAnim.SetTrigger("GetUp");
            CellSound.StopAll();
            StopAndResetMusic();

            yield return new WaitForSeconds(3);

            ProgressMusic();

            yield return new WaitForSeconds(audioClips[0].length / 2);
            selectedEnemy.AI.Mode = AgentMode.Agressive;

            enemyAnim.SetTrigger("PraiseSun");
            targetHeight += Time.deltaTime / 4;

            yield return new WaitForSeconds(audioClips[0].length / 2);

            enemyAnim.SetTrigger("GoToIdleFloating");
            canAttack = true;

            introFinished = true;
        }
    }
}