using AK;

namespace BossfightLevel.BossfightMain
{
    class WaveDatas
    {
        public static List<WaveData> AllWaveDatas = new()
        {
            new WaveData(new Dictionary<uint, int> { [13] = 8, [11] = 6, [16] = 2 }, 2, SWITCHES.ENEMY_TYPE.SWITCH.STRIKER),
            new WaveData(new Dictionary<uint, int> { [30] = 6, [13] = 4, [11] = 6 }, 1, SWITCHES.ENEMY_TYPE.SWITCH.STRIKER),
            new WaveData(new Dictionary<uint, int> { [30] = 6, [39] = 1 }, 2, SWITCHES.ENEMY_TYPE.SWITCH.BULLRUSHER),
            new WaveData(new Dictionary<uint, int> { [13] = 3, [30] = 4, [39] = 2 }, 3, SWITCHES.ENEMY_TYPE.SWITCH.BULLRUSHER),
            new WaveData(new Dictionary<uint, int> { [29] = 1 }, 1, SWITCHES.ENEMY_TYPE.SWITCH.TANK),
            new WaveData(new Dictionary<uint, int> { [36] = 1 }, 3, SWITCHES.ENEMY_TYPE.SWITCH.BIRTHER),
        };
    }
}
