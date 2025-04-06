using UnityEngine;

namespace CutsceneCamera
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
