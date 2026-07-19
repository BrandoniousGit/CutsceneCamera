using AIGraph;
using AssetShards;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Enemies;
using Player;
using StrikerBossfight.BossfightMain;
using System.Collections;
using UnityEngine;

namespace BossfightLevel.BossfightMain
{
    class BossfightCore : MonoBehaviourExtended
    {
        private EnemyAgent selectedEnemy;
        private Animator enemyAnim;

        private GameObject sunPrefab;
        private GameObject firePlumePrefab;
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

        private bool introCompleted;
        private bool musicStarted;

        public string assetToSpawn = "Assets/ParticleEffects/Effect/FantasySun.prefab";

        public void OnEnable()
        {
            AnimationEventReceiver.PunchEventTriggered += OnPunch;
            SunAttack.OnSunAttackFinished += GoToIdleFloating;
            PlumeAttack.OnPlumeAttackFinished += GoToIdleFloating;
            FireballAttack.OnFireballAttackFinished += GoToIdleFloating;
        }        
        
        public void OnDisable()
        {
            AnimationEventReceiver.PunchEventTriggered -= OnPunch;
            SunAttack.OnSunAttackFinished -= GoToIdleFloating;
            PlumeAttack.OnPlumeAttackFinished -= GoToIdleFloating;
            FireballAttack.OnFireballAttackFinished -= GoToIdleFloating;
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
            musicStarted = false;
            currentMusicStep = -1;
            musicLooper.Stop();
            musicTransitioner.Stop();
        }

        public void Update()
        {
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

            if (selectedEnemy == null)
            {
                var enemyList = AIG_CourseGraph.GetReachableEnemiesInNodes(PlayerManager.GetLocalPlayerAgent().CourseNode, 100);

                foreach (var enemy in enemyList)
                {
                    if (enemy.EnemyData.persistentID == 150u)
                    {
                        selectedEnemy = enemy;
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

            if (!introCompleted)
            {
                PerformIntro();
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                Debug.Log($"MusicTransitioner is {musicTransitioner.isPlaying}");
                Debug.Log($"MusicLooper is {musicLooper.isPlaying}");

                CellSound.StopAll();
                musicStarted = true;
            }              
            
            if (Input.GetKeyDown(KeyCode.M))
            {
                ProgressMusic();
            }                        
            
            if (Input.GetKeyDown(KeyCode.N))
            {
                StopAndResetMusic();
            }             
            
            if (Input.GetKeyDown(KeyCode.C))
            {
                SpawnSinglePlumeAttack(PlayerManager.GetLocalPlayerAgent().FPSCamera.m_camRayHit.point);
            }             
            
            if (Input.GetKeyDown(KeyCode.V))
            {
                SpawnSunAttack();
            }
        }

        public async void PerformIntro()
        {
            introCompleted = true;
        }

        public void OnPunch()
        {
            Debug.Log("PunchPerformed");
        }

        internal void LevelStarted()
        {
            BossfightPatches.OnVolumeChangedAction += OnVolumeChanged;

            currentMusicStep = -1;

            sunPrefab = AssetShardManager.GetLoadedAsset<GameObject>("Assets/-CustomStuff/CustomBossfightStuff/Attacks/SunAttack.prefab");
            firePlumePrefab = AssetShardManager.GetLoadedAsset<GameObject>("Assets/-CustomStuff/CustomBossfightStuff/Attacks/FirePlume.prefab");
            firePlumeShortPrefab = AssetShardManager.GetLoadedAsset<GameObject>("Assets/-CustomStuff/CustomBossfightStuff/Attacks/FirePlumeShort.prefab");
            preFireballEffectPrefab = AssetShardManager.GetLoadedAsset<GameObject>("Assets/-CustomStuff/CustomBossfightStuff/Attacks/PreFireball.prefab");
            flameAuraPrefab = AssetShardManager.GetLoadedAsset<GameObject>("Assets/-CustomStuff/CustomBossfightStuff/Attacks/FlameAura.prefab");

            Debug.Log("Assets Loaded");

            LoadAudio();
        }

        internal void LevelQuit()
        {
            BossfightPatches.OnVolumeChangedAction -= OnVolumeChanged;
            musicStarted = false;
        }

        public void LoadAudio()
        {
            audioClips = new List<AudioClip>();

            Debug.Log("Audio Loading");

            escapeMusic = Instantiate(AssetShardManager.GetLoadedAsset<GameObject>($"Assets/-CustomStuff/CustomBossfightStuff/Moosic.prefab")).GetComponent<AudioSource>();
            musicLooper = Instantiate(AssetShardManager.GetLoadedAsset<GameObject>($"Assets/-CustomStuff/Music/MusicLooper.prefab")).GetComponent<AudioSource>();
            musicTransitioner = Instantiate(AssetShardManager.GetLoadedAsset<GameObject>($"Assets/-CustomStuff/Music/MusicTransitioner.prefab")).GetComponent<AudioSource>();

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
            var newEffect = Instantiate(preFireballEffectPrefab, Vector3.zero, Quaternion.identity);
            var fireballAttack = newEffect.AddComponent<FireballAttack>();
            fireballAttack.transform.position += selectedEnemy.transform.position + Vector3.up * 1.2f;
            fireballAttack.pulseInterval = pulseInterval;
            fireballAttack.duration = duration;

            enemyAnim.SetTrigger("PraiseSun");
        }

        public void SpawnSunAttack()
        {
            var newEffect = Instantiate(sunPrefab, Vector3.zero, Quaternion.identity);
            newEffect.transform.position += selectedEnemy.transform.position + Vector3.up * 8f;
            newEffect.AddComponent<SunAttack>();

            enemyAnim.SetTrigger("PraiseSun");
        }        
        
        public async void SpawnFirePlumeAttacks(List<Vector3> plumePositions, int delayBetweenSpawns = 0, bool isShort = false)
        {
            foreach (var pos in plumePositions)
            {
                var newEffect = Instantiate(isShort ? firePlumeShortPrefab : firePlumePrefab, pos, Quaternion.identity);
                newEffect.transform.position += Vector3.up * 0.5f;
                var plumeAttack = newEffect.AddComponent<PlumeAttack>();
                plumeAttack.isShort = isShort;

                await Task.Delay(delayBetweenSpawns);
            }
        }        
        
        public void SpawnSinglePlumeAttack(Vector3 pos, bool isShort = false)
        {
            var newEffect = Instantiate(isShort ? firePlumeShortPrefab : firePlumePrefab, pos, Quaternion.identity);
            newEffect.transform.position += Vector3.up * 0.5f;
            var plumeAttack = newEffect.AddComponent<PlumeAttack>();
            plumeAttack.isShort = isShort;
        }
        #endregion

        #region states
        public void GoToIdleFloating()
        {
            enemyAnim.SetTrigger("GoToIdleFloating");
        }
        #endregion

        public IEnumerator WaitThenStop(float timeToWait)
        {
            yield return new WaitForSeconds(timeToWait);
            ProgressMusic();
        }
    }
}