using Agents;
using AIGraph;
using AK;
using AssetShards;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Enemies;
using GTFO.API;
using LevelGeneration;
using Player;
using SNetwork;
using StrikerBossfight.BossfightMain;
using StrikerBossfight.CutsceneCamera;
using System.Collections;
using UnityEngine;

namespace BossfightLevel.BossfightMain
{
    class BossfightCore : MonoBehaviourExtended
    {
        public static BossfightCore Instance { get; private set; }

        public enum PlumePattern
        {
            OnPlayers,
            CircleExpand,
            CircleExpandAlternating,
            Spiral
        }

        private EnemyAgent selectedEnemy;
        private EnemyAgent finalBoss;
        private Animator enemyAnim;

        private GameObject sunPrefab;
        private GameObject preFireballEffectPrefab;
        private GameObject flameAuraPrefab;

        private AudioSource spotlightEmitter;
        private AudioSource musicLooper;
        private AudioSource musicTransitioner;

        private List<AudioClip> audioClips;
        private AudioClip spotlightSfx;
        private Light spotlight;

        private int currentMusicStep;
        private bool fadeMusicLooperOut;
        private bool enteredPhase2;
        private bool enteredPhase3;
        private bool enteredFinal;

        private bool introStarted;
        private bool introFinished;
        private bool youRaiseMeUp;

        private bool canAttack;
        private bool isOnCooldown;
        private float attackCooldown;
        private float targetHeight = 2;

        private List<Vector3> AllSpawnPoints = new List<Vector3>() { new Vector3(-62, 0, 127), new Vector3(62, 0, 127) };

        public void OnEnable()
        {
            Instance = this;

            AnimationEventReceiver.PunchEventTriggered += OnPunch;
            SunAttack.OnSunAttackFinished += GoToIdleFloating;
            FireballAttack.OnFireballAttackFinished += GoToIdleFloating;
            PlumeAttack.OnPlumeAttackFinished += GoToIdleFloating;
            BossfightPatches.OnVolumeChangedAction += OnVolumeChanged;
        }

        public void OnDisable()
        {
            AnimationEventReceiver.PunchEventTriggered -= OnPunch;
            SunAttack.OnSunAttackFinished -= GoToIdleFloating;
            FireballAttack.OnFireballAttackFinished -= GoToIdleFloating;
            PlumeAttack.OnPlumeAttackFinished -= GoToIdleFloating;
            BossfightPatches.OnVolumeChangedAction -= OnVolumeChanged;
        }

        public void OnApplicationFocus(bool hasFocus)
        {
            if (!RundownManager.ExpeditionIsStarted)
            {
                return;
            }

            spotlightEmitter.mute = !hasFocus;
            musicLooper.mute = !hasFocus;
            musicTransitioner.mute = !hasFocus;
        }

        public void OnVolumeChanged(float value)
        {
            if (!RundownManager.ExpeditionIsStarted)
            {
                return;
            }

            spotlightEmitter.volume = value;
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
            if (musicLooper != null)
            {
                currentMusicStep = -1;
                musicLooper.Stop();
                musicTransitioner.Stop();
            }
        }

        public void Update()
        {
            if (GameStateManager.CurrentStateName == eGameStateName.ExpeditionFail || GameStateManager.CurrentStateName == eGameStateName.ExpeditionSuccess)
            {
                StopAndResetMusic();
            }

            if (SNet.IsMaster && finalBoss != null)
            {
                finalBoss.Locomotion.m_maxMovementSpeed = 0;
                finalBoss.transform.position = new Vector3(120, 228.5f, 292);
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

            if (selectedEnemy != null && !selectedEnemy.Alive && !fadeMusicLooperOut && !enteredFinal)
            {
                enteredFinal = true;
                fadeMusicLooperOut = true;
                StartCoroutine(WaitForFinalMusic().WrapToIl2Cpp());
                return;
            }

            if (selectedEnemy == null)
            {
                canAttack = false;

                var enemyList = AIG_CourseGraph.GetReachableEnemiesInNodes(PlayerManager.GetLocalPlayerAgent().CourseNode, 100);

                foreach (var enemy in enemyList)
                {
                    if (enemy.EnemyData.persistentID == 150u)
                    {
                        selectedEnemy = enemy;
                        selectedEnemy.Damage.HealthMax = 600 * PlayerManager.PlayerAgentsInLevel.Count;
                        selectedEnemy.Damage.Health = 600 * PlayerManager.PlayerAgentsInLevel.Count;
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

            if (youRaiseMeUp)
            {
                targetHeight += Time.deltaTime / 1.75f;
            }

            if (!introFinished)
            {
                return;
            }

            if (selectedEnemy.Damage.Health < (600 * PlayerManager.PlayerAgentsInLevel.Count * 0.66f) && !enteredPhase2)
            {
                enteredPhase2 = true;
                ProgressMusic();
            }

            if (selectedEnemy.Damage.Health < (600 * PlayerManager.PlayerAgentsInLevel.Count * 0.33f) && !enteredPhase3)
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
            if (!SNet.IsMaster)
            {
                return;
            }

            canAttack = false;
            enemyAnim.SetTrigger("PraiseSun");
            attackCooldown = enteredPhase3 ? 6.5f : enteredPhase2 ? 7 : 8;

            var random = UnityEngine.Random.Range(0, 5);
            var random2 = UnityEngine.Random.Range(0, 4);
            var random3 = UnityEngine.Random.Range(0, 6);
            var random4 = UnityEngine.Random.Range(0, 2);

            NetworkAPI.InvokeEvent<int>("OnNetworkedAttack", (random * 10) + random2);
            Debug.Log($"Sending value of {(random * 10) + random2} across network");

            switch (random)
            {
                case 0:
                    SpawnSunAttack(10, 10);
                    break;
                case 1:
                    SpawnFireballAttack(8, enteredPhase3 ? 0.5f : enteredPhase2 ? 0.75f : 1);
                    break;
                case 2:
                case 3:
                    switch (random2)
                    {
                        case 0:
                            SpawnFirePlumeAttacks(PlumePattern.CircleExpand, 5, enteredPhase3 ? 0 : enteredPhase2 ? 0.25f : 0.6f);
                            break;
                        case 1:
                            SpawnFirePlumeAttacks(PlumePattern.OnPlayers, enteredPhase3 ? 12 : enteredPhase2 ? 8 : 4, enteredPhase3 ? 0.33f : enteredPhase2 ? 0.5f : 1);
                            break;
                        case 2:
                            SpawnFirePlumeAttacks(PlumePattern.Spiral, 16, enteredPhase3 ? 0.1f : enteredPhase2 ? 0.2f : 0.3f);
                            break;
                        case 3:
                            SpawnFirePlumeAttacks(PlumePattern.CircleExpandAlternating, 5, enteredPhase3 ? 0 : enteredPhase2 ? 0.25f : 0.6f);
                            break;
                    }
                    break;
                case 4:
                    attackCooldown += 6 - PlayerManager.PlayerAgentsInLevel.Count;
                    isOnCooldown = true;
                    SpawnWave(WaveDatas.AllWaveDatas[random3], AllSpawnPoints[random4]);
                    break;
            }
        }

        public static void OnNetworkedAttack(ulong senderID, int value)
        {
            Debug.Log("Recieved Networked Attack");

            Instance?.NetworkedAttack(value);
        }

        public void NetworkedAttack(int value)
        {
            if (SNet.IsMaster)
            {
                return;
            }

            Debug.Log("Running Networked Attack");

            string stringValue = value.ToString();

            canAttack = false;
            enemyAnim.SetTrigger("PraiseSun");
            attackCooldown = enteredPhase3 ? 7.5f : enteredPhase2 ? 9 : 10;

            switch (stringValue[0])
            {
                case '0':
                    SpawnSunAttack(10, 10);
                    break;
                case '1':
                    SpawnFireballAttack(8, enteredPhase3 ? 0.5f : enteredPhase2 ? 0.75f : 1);
                    break;
                case '2':
                    switch (stringValue[1])
                    {
                        case '0':
                            SpawnFirePlumeAttacks(PlumePattern.CircleExpand, 5, enteredPhase3 ? 0 : enteredPhase2 ? 0.25f : 0.6f);
                            break;
                        case '1':
                            SpawnFirePlumeAttacks(PlumePattern.OnPlayers, enteredPhase3 ? 12 : enteredPhase2 ? 8 : 4, enteredPhase3 ? 0.33f : enteredPhase2 ? 0.5f : 1);
                            break;
                        case '2':
                            SpawnFirePlumeAttacks(PlumePattern.Spiral, 16, enteredPhase3 ? 0.1f : enteredPhase2 ? 0.2f : 0.3f);
                            break;
                        case '3':
                            SpawnFirePlumeAttacks(PlumePattern.CircleExpandAlternating, 5, enteredPhase3 ? 0 : enteredPhase2 ? 0.25f : 0.6f);
                            break;
                    }
                    break;
                case '4':
                    attackCooldown += 20;
                    isOnCooldown = true;
                    break;
            }
        }

        internal void LevelStarted()
        {
            sunPrefab = AssetShardManager.GetLoadedAsset<GameObject>("Assets/-CustomStuff/CustomBossfightStuff/Attacks/SunAttack.prefab");
            preFireballEffectPrefab = AssetShardManager.GetLoadedAsset<GameObject>("Assets/-CustomStuff/CustomBossfightStuff/Attacks/PreFireball.prefab");
            flameAuraPrefab = AssetShardManager.GetLoadedAsset<GameObject>("Assets/-CustomStuff/CustomBossfightStuff/Attacks/FlameAura.prefab");

            Debug.Log("Assets Loaded");

            if (SNet.IsMaster)
            {
                EnemyAllocator.Current.SpawnEnemy(150u, Builder.CurrentFloor.m_dimensions[0].Layers[1].m_zones[0].m_areas[0].m_courseNode, AgentMode.Hibernate, new Vector3(0, 0.25f, 125.4f), Quaternion.EulerAngles(new Vector3(0, 180 * Mathf.Deg2Rad, 0)));
                finalBoss = EnemyAllocator.Current.SpawnEnemy(151u, Builder.CurrentFloor.m_dimensions[1].Layers[0].m_zones[0].m_areas[0].m_courseNode, AgentMode.Hibernate, new Vector3(120, 228.5f, 292), Quaternion.EulerAngles(new Vector3(0, 200 * Mathf.Deg2Rad, 0)));

                var runtimeAnimController = AssetShardManager.GetLoadedAsset<RuntimeAnimatorController>("Assets/-CustomStuff/BuildThisOne/fungus.controller");
                finalBoss.Anim.runtimeAnimatorController = runtimeAnimController;
            }

            Debug.Log("Boss Spawned");

            LoadAudio();
        }

        internal void Cleanup()
        {
            StopAndResetMusic();

            currentMusicStep = -1;
            introStarted = false;
            introFinished = false;
            enteredPhase2 = false;
            enteredPhase3 = false;
            youRaiseMeUp = false;
            enteredFinal = false;

            StopAllCoroutines();

            Destroy(spotlight);
            Destroy(spotlightEmitter);
            Destroy(musicLooper);
            Destroy(musicTransitioner);
        }

        public void LoadAudio()
        {
            audioClips = new List<AudioClip>();

            spotlightEmitter = gameObject.AddComponent<AudioSource>();
            musicLooper = gameObject.AddComponent<AudioSource>();
            musicTransitioner = gameObject.AddComponent<AudioSource>();

            spotlightEmitter.loop = false;
            musicLooper.loop = true;
            musicTransitioner.loop = false;

            spotlightEmitter.volume = 0.35f;
            musicLooper.volume = 0.35f;
            musicTransitioner.volume = 0.35f;

            audioClips.Add(AssetShardManager.GetLoadedAsset<AudioClip>($"Assets/-CustomStuff/Music/Opening.ogg"));
            audioClips.Add(AssetShardManager.GetLoadedAsset<AudioClip>($"Assets/-CustomStuff/Music/Phase1Loop.ogg"));
            audioClips.Add(AssetShardManager.GetLoadedAsset<AudioClip>($"Assets/-CustomStuff/Music/Phase2Transition.ogg"));
            audioClips.Add(AssetShardManager.GetLoadedAsset<AudioClip>($"Assets/-CustomStuff/Music/Phase2Loop.ogg"));
            audioClips.Add(AssetShardManager.GetLoadedAsset<AudioClip>($"Assets/-CustomStuff/Music/Phase3Transition.ogg"));
            audioClips.Add(AssetShardManager.GetLoadedAsset<AudioClip>($"Assets/-CustomStuff/Music/Phase3Loop.ogg"));
            audioClips.Add(AssetShardManager.GetLoadedAsset<AudioClip>($"Assets/-CustomStuff/Music/FinalDesperation.ogg"));
            spotlightSfx = AssetShardManager.GetLoadedAsset<AudioClip>($"Assets/-CustomStuff/Music/Spotlight.mp3");

            Debug.Log("Audio Loaded");
        }

        #region attacks
        public void SpawnFireballAttack(float duration, float pulseInterval = 2)
        {
            var newEffect = Instantiate(preFireballEffectPrefab, Vector3.zero, Quaternion.identity);
            var fireballAttack = newEffect.AddComponent<FireballAttack>();
            fireballAttack.transform.position += selectedEnemy.transform.position + (Vector3.up * 4f);
            fireballAttack.pulseInterval = pulseInterval;
            fireballAttack.duration = duration;
        }

        public void SpawnFireballAttacks(List<Vector3> positions, float pulseInterval = 2)
        {
            var newEffect = Instantiate(preFireballEffectPrefab, Vector3.zero, Quaternion.identity);
            var fireballAttack = newEffect.AddComponent<FireballAttack>();
            fireballAttack.transform.position += new Vector3(122, 370, 290);
            fireballAttack.pulseInterval = pulseInterval;
            fireballAttack.duration = 60;
            fireballAttack.Init(positions);
        }

        public void SpawnSunAttack(float duration, float spawnHeight, float maxSizeOverTime = 1.5f, float startSize = 0.05f, bool doesDamage = true, bool isFinal = false)
        {
            var newEffect = Instantiate(sunPrefab, Vector3.zero, Quaternion.identity);

            if (isFinal)
            {
                newEffect.transform.position += finalBoss.transform.position + (Vector3.up * spawnHeight);
            }
            else
            {
                newEffect.transform.position += selectedEnemy.transform.position + (Vector3.up * spawnHeight);
            }

            var sunAttack = newEffect.AddComponent<SunAttack>();
            sunAttack.Init(duration, maxSizeOverTime, startSize, doesDamage);
        }

        public void SpawnFirePlumeAttacks(PlumePattern pattern, int count = 1, float pulseInterval = 1f, bool isShort = false)
        {
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
            musicTransitioner.Play();
        }

        public void SpawnWave(WaveData waveData, Vector3 spawnPoint)
        {
            var cellSoundPlayer = new CellSoundPlayer();

            var newEffect = Instantiate(flameAuraPrefab, selectedEnemy.transform.position, Quaternion.identity);
            newEffect.AddComponent<DespawnEffect>();

            if (SNet.IsMaster)
            {
                StartCoroutine(SpawnEnemies(waveData.EnemyList, spawnPoint).WrapToIl2Cpp());
            }

            if (waveData.ScreamType != null)
            {
                cellSoundPlayer.SetSwitch(SWITCHES.ENEMY_TYPE.GROUP, waveData.ScreamType.Value);

                var screamSize = waveData.ScreamSize != null ? waveData.ScreamSize.Value : 1;
                cellSoundPlayer.SetSwitch(SWITCHES.ROAR_SIZE.GROUP, screamSize);

                cellSoundPlayer.SetSwitch(SWITCHES.ENVIROMENT.GROUP, SWITCHES.ENVIROMENT.SWITCH.COMPLEX);
                cellSoundPlayer.Post(EVENTS.PLAY_WAVE_DISTANT_ROAR, spawnPoint);
            }
            else { Debug.Log("Scream type is null"); cellSoundPlayer.Post(EVENTS.PLAY_WAVE_DISTANT_ROAR_R8E1); }

            StartCoroutine(Cleanup(cellSoundPlayer, 10).WrapToIl2Cpp());
        }

        private IEnumerator SpawnEnemies(Dictionary<uint, int> enemyIDs, Vector3 spawnPos)
        {
            foreach (var val in enemyIDs)
            {
                for (int i = 0; i < val.Value; i++)
                {
                    EnemyAllocator.Current.SpawnEnemy(val.Key, PlayerManager.GetLocalPlayerAgent().CourseNode, AgentMode.Agressive, spawnPos, Quaternion.identity);
                    yield return new WaitForSeconds(0.05f);
                }
            }
        }

        public IEnumerator WaitThenStop(float timeToWait)
        {
            yield return new WaitForSeconds(timeToWait);
            ProgressMusic();
        }

        public IEnumerator WaitForFinalMusic()
        {
            Debug.Log("Beginning final phase");

            yield return new WaitForSeconds(6);

            foreach (var player in PlayerManager.PlayerAgentsInLevel)
            {
                WorldEventManager.ExecuteEvent(new GameData.WardenObjectiveEventData()
                {
                    Type = GameData.eWardenObjectiveEventType.DimensionWarpTeam,
                    DimensionIndex = eDimensionIndex.Dimension_1,
                    Delay = 0
                });
            }

            var enemyList = AIG_CourseGraph.GetReachableEnemiesInNodes(PlayerManager.GetLocalPlayerAgent().CourseNode, 100);

            foreach (var enemy in enemyList)
            {
                if (enemy.EnemyData.persistentID == 151u)
                {
                    finalBoss = enemy;
                }
            }

            if (finalBoss != null)
            {
                finalBoss.AI.Mode = AgentMode.Agressive;
                finalBoss.Locomotion.ChangeState(ES_StateEnum.HibernateWakeUp);
                finalBoss.Anim.SetTrigger("GoToIdleFloating");

                yield return new WaitForSeconds(5);
                finalBoss.Anim.SetTrigger("PraiseSun");
            }
            else
            {
                yield return new WaitForSeconds(5);
            }

            List<Vector3> fireballPositionOverrides = new List<Vector3>()
            {
                new Vector3(130, 211, 187),
                new Vector3(97, 210, 200),
                new Vector3(65, 210, 193),
                new Vector3(97, 214, 259),
                new Vector3(87, 213, 164),
                new Vector3(83, 212, 138),
                new Vector3(59, 211, 150),
                new Vector3(53, 208, 98),
                new Vector3(151, 215, 242),
                new Vector3(49, 208, 90),
                new Vector3(69, 212, 124),
                new Vector3(90, 211, 110),
                new Vector3(46, 208, 36),
                new Vector3(80, 209, 77),
                new Vector3(55, 209, 62),
                new Vector3(86, 207, 28)
            };

            SpawnSunAttack(65, 150, 30, 1, false, true);
            SpawnFireballAttacks(fireballPositionOverrides, 0.5f);

            PlayFinalMusic();

            yield return new WaitForSeconds(65);

            if (GameStateManager.CurrentStateName != eGameStateName.ExpeditionSuccess)
            {
                foreach(var player in PlayerManager.PlayerAgentsInLevel)
                {
                    player.Damage.NoAirDamage(100);
                }

                StopAndResetMusic();
            }
        }

        private IEnumerator Cleanup(CellSoundPlayer csPlayer, float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            csPlayer.Stop();
            csPlayer.Recycle();
        }

        public IEnumerator PerformIntro()
        {
            introStarted = true;

            foreach (var player in PlayerManager.PlayerAgentsInLevel)
            {
                player.TeleportTo(new Vector3(0, 0, 110));
            }

            yield return new WaitForSeconds(2);

            CutsceneCameraLogic.Instance.ToggleCutscene(true);

            var wallPrefab = AssetShardManager.GetLoadedAsset("Assets/-CustomStuff/Wall/BP_Gardens_Rocks_4x4M.prefab");
            Instantiate(wallPrefab, new Vector3(0, 0, 96), Quaternion.identity);

            CellSound.StopAll();
            StopAndResetMusic();
            spotlightEmitter.clip = spotlightSfx;
            spotlightEmitter.Play();

            var spotlightObject = new GameObject();
            spotlight = spotlightObject.AddComponent<Light>();
            spotlight.color = new Color(1, 0.2f, 0.2f, 1);
            spotlight.transform.position = new Vector3(selectedEnemy.transform.position.x, 15, selectedEnemy.transform.position.z);
            spotlight.range = 25;
            spotlight.intensity = 0.7f;
            spotlight.shape = LightShape.Cone;
            spotlight.transform.LookAt(selectedEnemy.transform);

            WorldEventManager.ExecuteEvent(new GameData.WardenObjectiveEventData()
            {
                Type = GameData.eWardenObjectiveEventType.OpenSecurityDoor,
                DimensionIndex = eDimensionIndex.Reality,
                Layer = LG_LayerType.SecondaryLayer,
                LocalIndex = GameData.eLocalZoneIndex.Zone_2,
                Delay = 0
            });

            WorldEventManager.ExecuteEvent(new GameData.WardenObjectiveEventData()
            {
                Type = GameData.eWardenObjectiveEventType.OpenSecurityDoor,
                DimensionIndex = eDimensionIndex.Reality,
                Layer = LG_LayerType.SecondaryLayer,
                LocalIndex = GameData.eLocalZoneIndex.Zone_3,
                Delay = 0
            });

            yield return new WaitForSeconds(1);

            enemyAnim.SetTrigger("GetUp");

            yield return new WaitForSeconds(1);

            ProgressMusic();
            youRaiseMeUp = true;

            yield return new WaitForSeconds(audioClips[0].length / 2);
            selectedEnemy.AI.Mode = AgentMode.Agressive;
            selectedEnemy.Locomotion.ChangeState(ES_StateEnum.HibernateWakeUp);
            youRaiseMeUp = false;

            enemyAnim.SetTrigger("PraiseSun");

            yield return new WaitForSeconds(audioClips[0].length / 2);

            enemyAnim.SetTrigger("GoToIdleFloating");
            canAttack = true;

            CutsceneCameraLogic.Instance.ToggleCutscene(false);

            introFinished = true;

            StartCoroutine(UnlimitedStrikers().WrapToIl2Cpp());
            StartCoroutine(UnlimitedGiants().WrapToIl2Cpp());
        }

        public IEnumerator UnlimitedStrikers()
        {
            if (SNet.IsMaster)
            {
                EnemyAllocator.Current.SpawnEnemy(13u, PlayerManager.GetLocalPlayerAgent().CourseNode, AgentMode.Agressive, new Vector3(-62, 0, 127), Quaternion.identity);
                EnemyAllocator.Current.SpawnEnemy(13u, PlayerManager.GetLocalPlayerAgent().CourseNode, AgentMode.Agressive, new Vector3(62, 0, 127), Quaternion.identity);
                EnemyAllocator.Current.SpawnEnemy(11u, PlayerManager.GetLocalPlayerAgent().CourseNode, AgentMode.Agressive, new Vector3(-62, 0, 127), Quaternion.identity);
                EnemyAllocator.Current.SpawnEnemy(11u, PlayerManager.GetLocalPlayerAgent().CourseNode, AgentMode.Agressive, new Vector3(62, 0, 127), Quaternion.identity);

                var spawnTimer = enteredPhase3 ? 12 : enteredPhase2 ? 16 : 20;

                yield return new WaitForSeconds(spawnTimer / PlayerManager.PlayerAgentsInLevel.Count);

                if (!enteredFinal)
                {
                    StartCoroutine(UnlimitedStrikers().WrapToIl2Cpp());
                }
            }
        }        
        
        public IEnumerator UnlimitedGiants()
        {
            if (SNet.IsMaster)
            {
                EnemyAllocator.Current.SpawnEnemy(16u, PlayerManager.GetLocalPlayerAgent().CourseNode, AgentMode.Agressive, new Vector3(-62, 0, 127), Quaternion.identity);
                EnemyAllocator.Current.SpawnEnemy(16u, PlayerManager.GetLocalPlayerAgent().CourseNode, AgentMode.Agressive, new Vector3(62, 0, 127), Quaternion.identity);

                yield return new WaitForSeconds(40);

                if (!enteredFinal)
                {
                    StartCoroutine(UnlimitedGiants().WrapToIl2Cpp());
                }
            }
        }
    }
}