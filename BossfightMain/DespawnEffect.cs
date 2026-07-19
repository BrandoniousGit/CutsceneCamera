using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BossfightLevel.BossfightMain
{
    class DespawnEffect : MonoBehaviour
    {
        private float timer;

        public void Update()
        {
            timer += Time.deltaTime;

            if (timer > 10)
            {
                Destroy(gameObject);
            }
        }
    }
}
