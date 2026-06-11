using System;
using UnityEngine;

namespace IdleOn.Core
{
    [Serializable]
    public class StatSheet
    {
        [Header("Primary")]
        public int STR = 1;
        public int AGI = 1;
        public int WIS = 1;
        public int LUK = 1;

        [Header("Secondary")]
        public float MaxHP = 100f;
        public float MaxMP = 50f;
        public float ATKMin = 5f;
        public float ATKMax = 10f;
        public float DEF = 2f;
        public float ACC = 90f;
        public float CRITChance = 5f;
        public float MoveSpeed = 3f;
    }
}
