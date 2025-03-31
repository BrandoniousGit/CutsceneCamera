using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using Expedition;
using Player;

namespace CutsceneCamera
{
    public class CutsceneCameraLogic : MonoBehaviourExtended
    {
        public bool cutsceneActive;

        public GameObject cutsceneCanvasObject;
        public GameObject Image1, Image2;
        public PlayerAgent player;
        public Transform fpsCamera;
        public GameObject cameraParent;

        public float moveSpeed = 0.05f;

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
                cutsceneActive = !cutsceneActive;

                if (cutsceneActive)
                {
                    player.PlayerSyncModel.SetAllVisible(true);
                    player.PlayerSyncModel.SetHeadVisible(true, true);
                    cutsceneCanvasObject.SetActive(true);
                    player.Locomotion.enabled = false;
                    fpsCamera.parent = cameraParent.transform;
                    fpsCamera.GetComponent<UI_Apply>().enabled = false;
                    player.FPItemHolder.ItemHiddenTrigger = true;
                    GuiManager.CrosshairLayer.SetVisible(false);
                }
                else
                {
                    player.PlayerSyncModel.SetAllVisible(false);
                    player.PlayerSyncModel.SetHeadVisible(false, false);
                    cutsceneCanvasObject.SetActive(false);
                    player.Locomotion.enabled = true;
                    fpsCamera.GetComponent<UI_Apply>().enabled = true;
                    fpsCamera.parent = player.FPItemHolder.transform.parent;
                    player.FPItemHolder.ItemHiddenTrigger = false;
                    GuiManager.CrosshairLayer.SetVisible(true);
                }
            }

            if (cutsceneActive)
            {
                player.m_movingCuller.UpdatePosition(player.DimensionIndex, cameraParent.transform.position);
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
            }
        }
    }
}
