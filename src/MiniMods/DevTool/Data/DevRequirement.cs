using System;

namespace ThunderFury.DevTool.Data
{
    // A single material line for an item or recipe (Jotunn's own
    // RequirementConfig shape, mirrored here as a plain serializable
    // field-only class since UnityEngine.JsonUtility -- used for all
    // DevTool persistence -- can't (de)serialize Jotunn/BepInEx types
    // directly).
    [Serializable]
    public class DevRequirement
    {
        public string ItemName = "";
        public int Amount = 1;
        public int AmountPerLevel = 0;
    }
}
