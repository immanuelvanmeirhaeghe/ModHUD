// MenuDebugSounds
using UnityEngine.UI;

public class MenuDebugSounds : MenuDebugScreen
{
	public Toggle m_ShowSounds3D;

	public Toggle m_ShowSoundsCurrentlyPlaying;

	protected override void Update()
	{
		base.Update();
		GreenHellGame.Instance.m_ShowSounds3D = m_ShowSounds3D.isOn;
		GreenHellGame.Instance.m_ShowSoundsCurrentlyPlaying = m_ShowSoundsCurrentlyPlaying.isOn;
	}
}
