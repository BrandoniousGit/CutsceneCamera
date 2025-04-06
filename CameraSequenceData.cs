using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CutsceneCamera
{
    public class CameraSequenceData
    {
        public string CutsceneName { get; set; } = string.Empty;
        public float FadeInOutTime { get; set; } = 1.5f;
        public List<CameraPositionData> SequenceData { get; set; }
        public uint PersistentId { get; set; }
    }
}
