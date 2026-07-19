using UnityEngine;

namespace BossfightLevel.BossfightMain
{
    public class AnimationEventReceiver : MonoBehaviour
    {
        public static event Action PunchEventTriggered;

        public void PunchEvent()
        {
            PunchEventTriggered?.Invoke();
        }
    }
}
