using AIGraph;
using LevelGeneration;
using Player;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerBossfight.CutsceneCamera
{
    public class CutsceneCameraLogic : MonoBehaviourExtended
    {
        public bool cutsceneActive, debugMode = false;

        public GameObject cutsceneCanvasObject;
        public GameObject Image1, Image2;
        public PlayerAgent player;
        public Transform fpsCamera;
        public GameObject cameraParent;

        public float moveSpeed = 0.05f;
        public float rotationSpeed = 300f;

        public float rotX, rotY;

        public float timer = 0;
        public float waitTimer;
        public int currentShot = 0;
        public CameraSequenceData data;
        public CameraPositionData current;

        //public float alignTimer = 0;
        //public bool aligningToPath;

        public AnimationCurve easeInCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2f),   // Flat start
            new Keyframe(1f, 1f, 0f, 0f)    // Fast end
        );

        public AnimationCurve easeOutCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 2f, 0f),   // Fast start
            new Keyframe(1f, 1f, 0f, 0f)    // Flat end
        );

        public AnimationCurve easeInOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public void Awake()
        {
            var sequenceData = new List<CameraPositionData>
            {
                new CameraPositionData
                {
                    StartPos = new Vector3(-4.1845903f, 4.845959f, 20.96401f),
                    EndPos = new Vector3(-8.809374f, 5.344844f, 20.55906f),
                    StartRot = new Vector3(0f, 270f, 0f),
                    EndRot = new Vector3(0f, 270f, 0f),
                    CamEasingType = CameraPositionData.EasingType.EaseOut,
                    WaitAfterMove = 2.2f
                },
                new CameraPositionData
                {
                    StartPos = new Vector3(-1.4201511f, 6.7959437f, 114.20211f),
                    EndPos = new Vector3(-10.149621f, 6.265077f, 114.101974f),
                    StartRot = new Vector3(0f, 270f, 0f),
                    EndRot = new Vector3(0f, 270f, 0f),
                    CamEasingType = CameraPositionData.EasingType.EaseOut,
                    WaitAfterMove = 2.2f
                }
            };

            data = new CameraSequenceData()
            {
                CutsceneName = "Test",
                PersistentId = 1,
                SequenceData = sequenceData
            };
        }

        public void LevelStarted()
        {
            SetupCameraCanvas();
            player = PlayerManager.GetLocalPlayerAgent();
            fpsCamera = player.FPSCamera.transform;
            cameraParent = new GameObject("Camera Parent");
        }

        public void SetupCameraCanvas()
        {
            cutsceneCanvasObject = new GameObject("Cutscene Canvas");

            Canvas cutsceneCanvas = cutsceneCanvasObject.AddComponent<Canvas>();
            cutsceneCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cutsceneCanvasObject.AddComponent<CanvasScaler>();
            cutsceneCanvasObject.AddComponent<GraphicRaycaster>();

            cutsceneCanvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            Image1 = new GameObject("Top Bar");
            Image1.transform.parent = cutsceneCanvasObject.transform;

            Image m_image1 = Image1.AddComponent<Image>();

            m_image1.rectTransform.anchorMin = new Vector2(0.5f, 1);
            m_image1.rectTransform.anchorMax = new Vector2(0.5f, 1);
            m_image1.rectTransform.pivot = new Vector2(0.5f, 1);
            m_image1.rectTransform.sizeDelta = new Vector2(2000, 60);
            m_image1.rectTransform.anchoredPosition = new Vector2(0, 0);
            m_image1.color = Color.black;

            Image2 = new GameObject("Bottom Bar");
            Image2.transform.parent = cutsceneCanvasObject.transform;

            Image m_image2 = Image2.AddComponent<Image>();

            m_image2.rectTransform.anchorMin = new Vector2(0.5f, 0);
            m_image2.rectTransform.anchorMax = new Vector2(0.5f, 0);
            m_image2.rectTransform.pivot = new Vector2(0.5f, 0);
            m_image2.rectTransform.sizeDelta = new Vector2(2000, 60);
            m_image2.rectTransform.anchoredPosition = new Vector2(0, 0);
            m_image2.color = Color.black;

            cutsceneCanvasObject.SetActive(false);
        }

        public void Update()
        {
            if (!RundownManager.ExpeditionIsStarted)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                timer = 0;
                currentShot = 0;
                waitTimer = 0;

                current = data.SequenceData.First();

                ToggleCutscene(!cutsceneActive);
            }

            if (cutsceneActive && !debugMode)
            {
                PlayCutscene();
            }

            UpdateCuller();

            #region debugcam
            if (cutsceneActive && debugMode)
            {
                player.m_movingCuller.UpdatePosition(player.DimensionIndex, cameraParent.transform.position);

                if (Input.GetKey(KeyCode.W))
                {
                    cameraParent.transform.position += fpsCamera.forward * moveSpeed;
                }
                if (Input.GetKey(KeyCode.S))
                {
                    cameraParent.transform.position -= fpsCamera.forward * moveSpeed;
                }
                if (Input.GetKey(KeyCode.A))
                {
                    cameraParent.transform.position -= fpsCamera.right * moveSpeed;
                }
                if (Input.GetKey(KeyCode.D))
                {
                    cameraParent.transform.position += fpsCamera.right * moveSpeed;
                }
                if (Input.GetKey(KeyCode.Q))
                {
                    cameraParent.transform.position += fpsCamera.up * moveSpeed;
                }
                if (Input.GetKey(KeyCode.E))
                {
                    cameraParent.transform.position -= fpsCamera.up * moveSpeed;
                }

                if (Input.GetKeyDown(KeyCode.Z))
                {
                    var rot = cameraParent.transform.rotation.eulerAngles;

                    Debug.Log($"\n{cameraParent.transform.position.x}f, {cameraParent.transform.position.y}f, {cameraParent.transform.position.z}f\n" +
                              $"{rot.x}f, {rot.y}f, {rot.z}f\n");
                }

                float mouseX = Input.GetAxis("MouseX");
                float mouseY = Input.GetAxis("MouseY");

                rotX += mouseX * rotationSpeed * Time.deltaTime;
                rotY -= mouseY * rotationSpeed * Time.deltaTime;

                _ = Mathf.Clamp(mouseY, -90f, 90f);

                cameraParent.transform.rotation = Quaternion.Euler(rotY, rotX, 0);
            }
            #endregion
        }

        public void ToggleCutscene(bool isActive)
        {
            if (!isActive)
            {
                cameraParent.transform.position = player.FPItemHolder.transform.position;
            }

            cutsceneActive = isActive;
            player.PlayerSyncModel.SetHeadVisible(isActive, isActive);
            cutsceneCanvasObject.SetActive(isActive);
            player.Locomotion.enabled = !isActive;
            fpsCamera.GetComponent<UI_Apply>().enabled = !isActive;
            fpsCamera.parent = isActive ? cameraParent.transform : player.FPItemHolder.transform.parent;
            player.FPSCamera.LookSpeedModifier = isActive ? 0 : 1;
            player.FPItemHolder.ItemHiddenTrigger = isActive;
            GuiManager.CrosshairLayer.SetVisible(!isActive);
        }

        private void UpdateCuller()
        {
            if (!cutsceneActive)
            {
                return;
            }

            var cullPos = fpsCamera.transform.position;

            if (Physics.Raycast(cullPos, Vector3.down, out var hit, 50f, LayerManager.MASK_WORLD))
            {
                cullPos = hit.m_Point;
            }

            if (hit.transform != null)
            {
                var node = FindCourseNodeInParent(hit.transform);

                if (node != null && player.m_movingCuller.CurrentNode.CourseNode != node)
                {
                    player.m_movingCuller.SetCurrentNode(node.m_cullNode);
                }
            }

            player.m_movingCuller.UpdatePosition(player.m_dimensionIndex, cullPos);
        }

        public static AIG_CourseNode? FindCourseNodeInParent(Transform nodeItem)
        {
            var current = nodeItem.parent;

            while (current != null)
            {
                LG_Area? area = current.GetComponent<LG_Area>() ?? null;

                if (area != null)
                {
                    return area.m_courseNode;
                }
                else
                {
                    current = current.parent;
                }
            }

            return null;
        }

        public void PlayCutscene()
        {
            timer += 1 * Time.deltaTime;

            var newCullPos = Physics.Raycast(cameraParent.transform.position, Vector3.down, out var hit) ? hit.point : cameraParent.transform.position;
            player.m_movingCuller.UpdatePosition(player.m_dimensionIndex, newCullPos);

            if (timer <= current.ShotDuration)
            {
                var t = timer / current.ShotDuration;
                switch (current.CamEasingType)
                {
                    case CameraPositionData.EasingType.None:
                        break;
                    case CameraPositionData.EasingType.EaseIn:
                        t = easeInCurve.Evaluate(t);
                        break;
                    case CameraPositionData.EasingType.EaseOut:
                        t = easeOutCurve.Evaluate(t);
                        break;
                    case CameraPositionData.EasingType.EaseInOut:
                        t = easeInOutCurve.Evaluate(t);
                        break;
                }

                var targetPosition = Vector3.Lerp(current.StartPos, current.EndPos, t);
                var targetAngle = Quaternion.Euler(current.StartRot);

                #region aligning
                //if (aligningToPath)
                //{
                //    alignTimer += Time.deltaTime;

                //    float t2 = Mathf.Clamp01(alignTimer / 4.0f);
                //    cameraParent.transform.position = Vector3.Lerp(cameraParent.transform.position, targetPosition, t2);
                //    cameraParent.transform.rotation = Quaternion.Lerp(cameraParent.transform.rotation, targetAngle, t2);

                //    if (t2 >= 1f)
                //    {
                //        aligningToPath = false;
                //    }

                //    return;
                //}
                #endregion

                cameraParent.transform.position = targetPosition;
                cameraParent.transform.rotation = targetAngle;

                return;
            }

            if (waitTimer <= current.WaitAfterMove)
            {
                waitTimer += 1 * Time.deltaTime;
                return;
            }

            if (currentShot + 1 < data.SequenceData.Count) current = UpdateShot();
            else
            {
                ToggleCutscene(false);
            }
        }

        public CameraPositionData UpdateShot()
        {
            currentShot++;
            waitTimer = 0;
            timer = 0;
            //alignTimer = 0;
            //aligningToPath = false;

            var next = data.SequenceData[currentShot];

            //if (next.LerpTowardsThisPoint)
            //{
            //    aligningToPath = true;
            //}

            return next;
        }

        public IEnumerator FadeOut(float fadeTime)
        {
            yield return new WaitForSeconds(fadeTime);
        }
    }
}
