using UnityEngine;

namespace ModHUD
{
    class PlayerExtended : Player
    {
        protected override void Start()
        {
            base.Start();
            new GameObject($"__{nameof(ModHUD)}__").AddComponent<ModHUD>();
        }
    }
}
