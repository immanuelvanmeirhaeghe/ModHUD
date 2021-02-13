// MainMenuOptionsAudio
using CJTools;
using Enums;
using UnityEngine.UI;

public class MainMenuOptionsAudio : MenuScreen, IYesNoDialogOwner
{
	public enum OptionsAudioQuestion
	{
		None,
		Back,
		Accept
	}

	public Button m_AcceptButton;

	public Button m_BackButton;

	public Slider m_Slider;

	public Slider m_DialogsSlider;

	public Slider m_MusicSlider;

	public Slider m_EnviroSlider;

	public Slider m_GeneralSlider;

	public Slider m_MenuMusicSlider;

	private OptionsAudioQuestion m_Question;

	public override void OnShow()
	{
		base.OnShow();
		ApplySliders();
	}

	private void ApplySliders()
	{
		m_Slider.value = GreenHellGame.Instance.m_Settings.m_Volume;
		m_DialogsSlider.value = GreenHellGame.Instance.m_Settings.m_DialogsVolume;
		m_MusicSlider.value = GreenHellGame.Instance.m_Settings.m_MusicVolume;
		m_EnviroSlider.value = GreenHellGame.Instance.m_Settings.m_EnviroVolume;
		m_GeneralSlider.value = GreenHellGame.Instance.m_Settings.m_GeneralVolume;
		if ((bool)m_MenuMusicSlider)
		{
			m_MenuMusicSlider.value = GreenHellGame.Instance.m_Settings.m_MenuMusicVolume;
		}
	}

	protected override void Update()
	{
		base.Update();
		GreenHellGame.Instance.m_Settings.m_Volume = m_Slider.value;
		GreenHellGame.Instance.m_Settings.m_MusicVolume = m_MusicSlider.value;
		if ((bool)m_MenuMusicSlider)
		{
			GreenHellGame.Instance.m_Settings.m_MenuMusicVolume = m_MenuMusicSlider.value;
		}
		GreenHellGame.Instance.GetAudioMixerGroup(AudioMixerGroupGame.Master).audioMixer.SetFloat("MasterVolume", General.LinearToDecibel(m_Slider.value));
	}

	public override bool IsMenuButtonEnabled(Button b)
	{
		if (b == m_AcceptButton)
		{
			return IsAnyOptionModified();
		}
		return base.IsMenuButtonEnabled(b);
	}

	public override void OnBack()
	{
		if (IsAnyOptionModified())
		{
			m_Question = OptionsAudioQuestion.Back;
			GreenHellGame.GetYesNoDialog().Show(this, DialogWindowType.YesNo, GreenHellGame.Instance.GetLocalization().Get("YNDialog_OptionsGame_BackTitle"), GreenHellGame.Instance.GetLocalization().Get("YNDialog_OptionsGame_Back"), !m_IsIngame);
		}
		else
		{
			base.OnBack();
		}
	}

	public override void OnAccept()
	{
		if (IsAnyOptionModified())
		{
			m_Question = OptionsAudioQuestion.Accept;
			GreenHellGame.GetYesNoDialog().Show(this, DialogWindowType.YesNo, GreenHellGame.Instance.GetLocalization().Get("YNDialog_OptionsGame_AcceptTitle"), GreenHellGame.Instance.GetLocalization().Get("YNDialog_OptionsGame_Accept"), !m_IsIngame);
		}
		else
		{
			ShowPreviousScreen();
		}
	}

	public void OnYesFromDialog()
	{
		if (m_Question == OptionsAudioQuestion.Back)
		{
			RevertOptionValues();
		}
		GreenHellGame.Instance.m_Settings.m_Volume = m_Slider.value;
		GreenHellGame.Instance.m_Settings.m_DialogsVolume = m_DialogsSlider.value;
		GreenHellGame.Instance.m_Settings.m_MusicVolume = m_MusicSlider.value;
		GreenHellGame.Instance.m_Settings.m_EnviroVolume = m_EnviroSlider.value;
		GreenHellGame.Instance.m_Settings.m_GeneralVolume = m_GeneralSlider.value;
		if ((bool)m_MenuMusicSlider)
		{
			GreenHellGame.Instance.m_Settings.m_MenuMusicVolume = m_MenuMusicSlider.value;
		}
		GreenHellGame.Instance.m_Settings.SaveSettings();
		GreenHellGame.Instance.m_Settings.ApplySettings(apply_resolution: false);
		ShowPreviousScreen();
	}

	public void OnNoFromDialog()
	{
	}

	public void OnOkFromDialog()
	{
	}

	public void OnCloseDialog()
	{
	}
}
