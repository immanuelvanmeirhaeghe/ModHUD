using System.Collections.Generic;
using UnityEngine;

public class PlayerAudioModuleExtended : PlayerAudioModule
{
    public override void PlayAttackSound(float volume = 1, bool loop = false)
    {
        if (ModAudio.ModHUD.Get().IsModActiveForSingleplayer || ModAudio.ModHUD.Get().IsModActiveForMultiplayer)
        {
            volume = ModAudio.ModHUD.CustomVolume;
        }
        base.PlayAttackSound(volume, loop);
    }

    public override void PlayBreathingSound(float volume = 1, bool loop = false)
    {
        if (ModAudio.ModHUD.Get().IsModActiveForSingleplayer || ModAudio.ModHUD.Get().IsModActiveForMultiplayer)
        {
            volume = ModAudio.ModHUD.CustomVolume;
        }
        base.PlayBreathingSound(volume, loop);
    }

    public override void PlayDamageSound(float volume = 1, bool loop = false)
    {
        if (ModAudio.ModHUD.Get().IsModActiveForSingleplayer || ModAudio.ModHUD.Get().IsModActiveForMultiplayer)
        {
            volume = ModAudio.ModHUD.CustomVolume;
        }
        base.PlayDamageSound(volume, loop);
    }

    public override void PlayDamageInsectsSound(float volume = 1, bool loop = false)
    {
        if (ModAudio.ModHUD.Get().IsModActiveForSingleplayer || ModAudio.ModHUD.Get().IsModActiveForMultiplayer)
        {
            volume = ModAudio.ModHUD.CustomVolume;
        }
        base.PlayDamageInsectsSound(volume, loop);
    }

    public override void PlaySanityLossSound(float volume = 1)
    {
        if (ModAudio.ModHUD.Get().IsModActiveForSingleplayer || ModAudio.ModHUD.Get().IsModActiveForMultiplayer)
        {
            volume = ModAudio.ModHUD.CustomVolume;
        }
        base.PlaySanityLossSound(volume);
    }

    public override void PlayDialogSound(float volume = 1, bool loop = false)
    {
        if (ModAudio.ModHUD.Get().IsModActiveForSingleplayer || ModAudio.ModHUD.Get().IsModActiveForMultiplayer)
        {
            volume = ModAudio.ModHUD.CustomVolume;
        }
        base.PlayDialogSound(volume, loop);
    }

    public override void PlayDrinkingDisgustingSound(float volume = 1, bool loop = false)
    {
        if (ModAudio.ModHUD.Get().IsModActiveForSingleplayer || ModAudio.ModHUD.Get().IsModActiveForMultiplayer)
        {
            volume = ModAudio.ModHUD.CustomVolume;
        }
        base.PlayDrinkingDisgustingSound(volume, loop);
    }

    public override void PlayDrinkingSound(float volume = 1, bool loop = false)
    {
        if (ModAudio.ModHUD.Get().IsModActiveForSingleplayer || ModAudio.ModHUD.Get().IsModActiveForMultiplayer)
        {
            volume = ModAudio.ModHUD.CustomVolume;
        }
        base.PlayDrinkingSound(volume, loop);
    }

    public override void PlayEatingDisgustingSound(float volume = 1, bool loop = false)
    {
        if (ModAudio.ModHUD.Get().IsModActiveForSingleplayer || ModAudio.ModHUD.Get().IsModActiveForMultiplayer)
        {
            volume = ModAudio.ModHUD.CustomVolume;
        }
        base.PlayEatingDisgustingSound(volume, loop);
    }

    public override void PlayEatingSound(float volume = 1, bool loop = false)
    {
        if (ModAudio.ModHUD.Get().IsModActiveForSingleplayer || ModAudio.ModHUD.Get().IsModActiveForMultiplayer)
        {
            volume = ModAudio.ModHUD.CustomVolume;
        }
        base.PlayEatingSound(volume, loop);
    }

    public override void PlayFeetJumpSound(float volume = 1, bool loop = false)
    {
        if (ModAudio.ModHUD.Get().IsModActiveForSingleplayer || ModAudio.ModHUD.Get().IsModActiveForMultiplayer)
        {
            volume = ModAudio.ModHUD.CustomVolume;
        }
        base.PlayFeetJumpSound(volume, loop);
    }

    public override void PlayFeetLandingSound(float volume = 1, bool loop = false)
    {
        if (ModAudio.ModHUD.Get().IsModActiveForSingleplayer || ModAudio.ModHUD.Get().IsModActiveForMultiplayer)
        {
            volume = ModAudio.ModHUD.CustomVolume;
        }
        base.PlayFeetLandingSound(volume, loop);
    }

    public override void PlayHeartBeatSound(float volume = 1, bool loop = false)
    {
        if (ModAudio.ModHUD.Get().IsModActiveForSingleplayer || ModAudio.ModHUD.Get().IsModActiveForMultiplayer)
        {
            volume = ModAudio.ModHUD.CustomVolume;
        }
        base.PlayHeartBeatSound(volume, loop);
    }

    public override void PlayHitArmorSound(float volume = 1, bool loop = false)
    {
        if (ModAudio.ModHUD.Get().IsModActiveForSingleplayer || ModAudio.ModHUD.Get().IsModActiveForMultiplayer)
        {
            volume = ModAudio.ModHUD.CustomVolume;
        }
        base.PlayHitArmorSound(volume, loop);
    }
}
