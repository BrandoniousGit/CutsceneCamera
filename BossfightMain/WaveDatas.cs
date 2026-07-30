using AK;

namespace BossfightLevel.BossfightMain
{
    class WaveDatas
    {
        public static List<WaveData> AllWaveDatas = new()
        {
            new WaveData(new Dictionary<uint, int> { [13] = 4, [11] = 2, [16] = 1 }, 2, SWITCHES.ENEMY_TYPE.SWITCH.STRIKER),
            new WaveData(new Dictionary<uint, int> { [30] = 2, [13] = 2, [11] = 2 }, 1, SWITCHES.ENEMY_TYPE.SWITCH.STRIKER),
            new WaveData(new Dictionary<uint, int> { [30] = 3, [39] = 1 }, 2, SWITCHES.ENEMY_TYPE.SWITCH.BULLRUSHER),
            new WaveData(new Dictionary<uint, int> { [13] = 2, [30] = 1, [39] = 1 }, 3, SWITCHES.ENEMY_TYPE.SWITCH.BULLRUSHER),
        };
    }
}
