// MenuOptions
using UnityEngine.UI;

public class MenuOptions : MenuScreen
{
	public Button m_Graphics;

	public Button m_Game;

	public Button m_Controls;

	public Button m_Audio;

	public Button m_Troubleshooting;

	public Text m_AudioText;

	public Text m_GraphicsText;

	public Text m_ControlsText;

	public Text m_GameText;

	public Text m_TroubleshootingText;

	public Button m_BackButton;

	public Text m_BackText;

	private float m_ButtonTextStartX;

	private float m_SelectedButtonX = 10f;

	public override bool IsMenuButtonEnabled(Button b)
	{
		return base.IsMenuButtonEnabled(b);
	}

	public override void OnBack()
	{
		m_MenuInGameManager.ShowScreen(typeof(MenuInGame));
	}

	public void OnGame()
	{
		m_MenuInGameManager.ShowScreen(typeof(MainMenuOptionsGame));
	}

	public void OnAudio()
	{
		m_MenuInGameManager.ShowScreen(typeof(MainMenuOptionsAudio));
	}

	public void OnGraphics()
	{
		m_MenuInGameManager.ShowScreen(typeof(MainMenuOptionsGraphics));
	}

	public void OnControls()
	{
		m_MenuInGameManager.ShowScreen(typeof(MainMenuOptionsControls));
	}

	public void OnThroubleShooting()
	{
		m_MenuInGameManager.ShowScreen(typeof(MainMenuOptionsTroubleshooting));
	}
}
