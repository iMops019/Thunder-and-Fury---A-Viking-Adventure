using System;
using System.Collections.Generic;

namespace ThunderFury.DevTool.Data
{
    // A single location's overridden spawn quantity, keyed by its prefab
    // name (confirmed unique per location type -- ZoneSystem itself
    // rejects duplicate prefab-name locations).
    [Serializable]
    public class DevZoneQuantityOverride
    {
        public string PrefabName = "";
        public int Quantity;
    }

    [Serializable]
    public class DevZoneSettingsList
    {
        public List<DevZoneQuantityOverride> Overrides = new List<DevZoneQuantityOverride>();
    }
}
