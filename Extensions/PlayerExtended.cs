using ModHUD.Managers;
using UnityEngine;

namespace ModHUD.Extensions
{
    class PlayerExtended : Player
    {
        protected override void Start()
        {
            base.Start();
            new GameObject($"__{nameof(ModHUD)}__").AddComponent<ModHUD>();
            new GameObject($"__{nameof(StylingManager)}__").AddComponent<StylingManager>();
        }
    }
}
