using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BossfightLevel.BossfightMain
{
    public class WaveData
    {
        public Dictionary<uint, int> EnemyList { get; set; }
        public uint? ScreamSize { get; set; }
        public uint? ScreamType { get; set; }

        public WaveData(Dictionary<uint, int> enemyList, uint screamSize, uint screamType)
        {
            EnemyList = enemyList;
            ScreamSize = screamSize;
            ScreamType = screamType;
        }

        public WaveData(Dictionary<uint, int> enemyList)
        {
            EnemyList = enemyList;
        }
    }
}
