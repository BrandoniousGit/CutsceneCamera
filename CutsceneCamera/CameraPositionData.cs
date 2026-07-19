using UnityEngine;

namespace StrikerBossfight.CutsceneCamera
{
    public class CameraPositionData
    {
        public Vector3 StartPos { get; set; }
        public Vector3 EndPos { get; set; }
        public Vector3 StartRot { get; set; }
        public Vector3 EndRot { get; set; }
        public float ShotDuration { get; set; } = 5;
        public float WaitAfterMove { get; set; }
        public EasingType CamEasingType { get; set; } = EasingType.None;
        public bool LerpTowardsThisPoint { get; set; }

        #region enums
        public enum EasingType
        {
            EaseIn,
            EaseOut,
            EaseInOut,
            None
        }
        #endregion
    }
}
