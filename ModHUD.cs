using ModHUD.Enums;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModAudio
{
    /// <summary>
    /// ModHUD is a mod for Green Hell that allows a player to adjust some audio effects volume.
    /// Enable the mod UI by pressing 7.
    /// </summary>
    public class ModHUD : MonoBehaviour
    {
        private static ModHUD Instance;

        private static readonly string ModName = nameof(ModHUD);
        private static readonly float ModScreenTotalWidth = Screen.width;
        private static readonly float ModScreenTotalHeight = 200f;
        private static readonly float ModScreenMinWidth = Screen.width;
        private static readonly float ModScreenMaxWidth = Screen.width;
        private static readonly float ModScreenMinHeight = 200f;
        private static readonly float ModScreenMaxHeight = 200f;
        private static float ModScreenStartPositionX { get; set; } = 0f;
        private static float ModScreenStartPositionY { get; set; } = 0f;
        private static bool IsMinimized { get; set; } = false;
        private bool ShowUI = false;
        public static Rect ModHUDScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);

        private static Player LocalPlayer;
        private static HUDManager LocalHUDManager;
        private static Watch LocalWatch;
        private static PlayerConditionModule LocalPlayerConditionModule;

        // Based on Watch
        public GameObject LocalHUDCanvas = new GameObject();
        private Dictionary<int, WatchData> LocalHUDCanvasDatas = new Dictionary<int, WatchData>();
        public static float LocalProteinsValue;

        public ModHUD()
        {
            useGUILayout = true;
            Instance = this;
        }

        public static ModHUD Get()
        {
            return Instance;
        }

        public static string OnlyForSinglePlayerOrHostMessage() => $"Only available for single player or when host. Host can activate using ModManager.";
        public static string PlayerAudioVolumeSetMessage(float volume) => $"Player audio volume set to {volume}";
        public static string PermissionChangedMessage(string permission) => $"Permission to use mods and cheats in multiplayer was {permission}";
        public static string HUDBigInfoMessage(string message, MessageType messageType, Color? headcolor = null)
            => $"<color=#{ (headcolor != null ? ColorUtility.ToHtmlStringRGBA(headcolor.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))  }>{messageType}</color>\n{message}";

        public void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
        }

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModName}:{methodName}] throws exception:\n{exc.Message}";
            ModAPI.Log.Write(info);
            ShowHUDBigInfo(HUDBigInfoMessage(info, MessageType.Error, Color.red));
        }

        private void ModManager_onPermissionValueChanged(bool optionValue)
        {
            IsModActiveForMultiplayer = optionValue;
            ShowHUDBigInfo(
                          (optionValue ?
                            HUDBigInfoMessage(PermissionChangedMessage($"granted"), MessageType.Info, Color.green)
                            : HUDBigInfoMessage(PermissionChangedMessage($"revoked"), MessageType.Info, Color.yellow))
                            );
        }

        public bool IsModActiveForMultiplayer { get; private set; }
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public void ShowHUDBigInfo(string text)
        {
            string header = $"{ModName} Info";
            string textureName = HUDInfoLogTextureType.Count.ToString();

            HUDBigInfo bigInfo = (HUDBigInfo)LocalHUDManager.GetHUD(typeof(HUDBigInfo));
            HUDBigInfoData.s_Duration = 2f;
            HUDBigInfoData bigInfoData = new HUDBigInfoData
            {
                m_Header = header,
                m_Text = text,
                m_TextureName = textureName,
                m_ShowTime = Time.time
            };
            bigInfo.AddInfo(bigInfoData);
            bigInfo.Show(true);
        }

        public void ShowHUDInfoLog(string itemID, string localizedTextKey)
        {
            Localization localization = GreenHellGame.Instance.GetLocalization();
            ((HUDMessages)LocalHUDManager.GetHUD(typeof(HUDMessages))).AddMessage(localization.Get(localizedTextKey) + "  " + localization.Get(itemID));
        }

        private void EnableCursor(bool blockPlayer = false)
        {
            CursorManager.Get().ShowCursor(blockPlayer, false);

            if (blockPlayer)
            {
                LocalPlayer.BlockMoves();
                LocalPlayer.BlockRotation();
                LocalPlayer.BlockInspection();
            }
            else
            {
                LocalPlayer.UnblockMoves();
                LocalPlayer.UnblockRotation();
                LocalPlayer.UnblockInspection();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                if (!ShowUI)
                {
                    InitData();
                }
                ToggleShowUI();
            }
        }

        private void ToggleShowUI()
        {
            ShowUI = !ShowUI;
        }

        private void OnGUI()
        {
            if (ShowUI)
            {
                InitData();
                InitSkinUI();
                InitWindow();
            }
        }

        private void InitData()
        {
            LocalHUDManager = HUDManager.Get();
            LocalPlayer = Player.Get();
            LocalWatch = Watch.Get();
            LocalPlayerConditionModule = PlayerConditionModule.Get();
            InitLocalHUDCanvas();
        }

        private void InitLocalHUDCanvas()
        {
            LocalHUDCanvas = new GameObject(nameof(LocalHUDCanvas));
        }

        private void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void InitWindow()
        {
            try
            {
                int wid = GetHashCode();
                ModHUDScreen = GUILayout.Window(wid, ModHUDScreen, InitModHUDScreen, ModName,
                                                                                        GUI.skin.window,
                                                                                        GUILayout.ExpandWidth(true),
                                                                                        GUILayout.MinWidth(ModScreenMinWidth),
                                                                                        GUILayout.MaxWidth(ModScreenMaxWidth),
                                                                                        GUILayout.ExpandHeight(true),
                                                                                        GUILayout.MinHeight(ModScreenMinHeight),
                                                                                        GUILayout.MaxHeight(ModScreenMaxHeight)
                                                                                       );
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(InitWindow));
            }
        }

        private void ScreenMenuBox()
        {
            if (GUI.Button(new Rect(ModHUDScreen.width - 40f, 0f, 20f, 20f), "-", GUI.skin.button))
            {
                CollapseWindow();
            }

            if (GUI.Button(new Rect(ModHUDScreen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                CloseWindow();
            }
        }

        private void CollapseWindow()
        {
            if (!IsMinimized)
            {
                ModHUDScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenMinHeight);
                IsMinimized = true;
            }
            else
            {
                ModHUDScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
                IsMinimized = false;
            }
            InitWindow();
        }

        private void CloseWindow()
        {
            ShowUI = false;
        }

        private void InitModHUDScreen(int windowID)
        {
            ModScreenStartPositionX = ModHUDScreen.x;
            ModScreenStartPositionY = ModHUDScreen.y;

            using (var modContentScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
               // CompassBox();
                MacronutrientsBox();
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void MacronutrientsBox()
        {
            try
            {
                Color defaultBG = GUI.backgroundColor;
                if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                {
                    using (var macroNutrientsScope = new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        GUI.backgroundColor = IconColors.GetColor(IconColors.Icon.Proteins);
                        using (var proteinScope = new GUILayout.HorizontalScope(GUI.skin.box))
                        {
                            Image proteinsImage = (Image)LocalWatch.m_Canvas.transform.Find("Proteins").GetComponent<Image>();
                            Image proteinsBGImage = (Image)LocalWatch.m_Canvas.transform.Find("ProteinsBG").GetComponent<Image>();
                            float fillAmount = LocalPlayer.GetNutritionProtein() / LocalPlayer.GetMaxNutritionProtein();
                            proteinsImage.fillAmount = fillAmount;
                            float proteinsMinValue = 0f;
                            float proteinsMaxValue = LocalPlayerConditionModule.GetMaxNutritionProtein();
                            float proteinsValue = LocalPlayerConditionModule.GetNutritionProtein();
                            GUILayout.HorizontalSlider(proteinsValue, proteinsMinValue, proteinsMaxValue);
                            GUILayout.Box(proteinsImage.mainTexture);
                            GUILayout.Box(proteinsBGImage.mainTexture);
                        }

                        GUI.backgroundColor = IconColors.GetColor(IconColors.Icon.Fat);
                        using (var fatScope = new GUILayout.HorizontalScope(GUI.skin.box))
                        {
                            Image fatImage = (Image)LocalWatch.m_Canvas.transform.Find("Fat").GetComponent<Image>();
                            Image fatBGImage = (Image)LocalWatch.m_Canvas.transform.Find("FatBG").GetComponent<Image>();
                            float fillAmount2 = LocalPlayer.GetNutritionFat() / LocalPlayer.GetMaxNutritionFat();
                            fatImage.fillAmount = fillAmount2;
                            float fatMinValue = 0f;
                            float fatMaxValue = LocalPlayerConditionModule.GetMaxNutritionFat();
                            float fatValue = LocalPlayerConditionModule.GetNutritionFat();
                            GUILayout.HorizontalSlider(fatValue, fatMinValue, fatMaxValue);
                            GUILayout.Box(fatImage.mainTexture);
                            GUILayout.Box(fatBGImage.mainTexture);
                        }

                        GUI.backgroundColor = IconColors.GetColor(IconColors.Icon.Carbo);
                        using (var carboScope = new GUILayout.HorizontalScope(GUI.skin.box))
                        {
                            Image carboImage = (Image)LocalWatch.m_Canvas.transform.Find("Carbo").GetComponent<Image>();
                            Image carboBGImage = (Image)LocalWatch.m_Canvas.transform.Find("CarboBG").GetComponent<Image>();
                            float fillAmount3 = LocalPlayer.GetNutritionFat() / LocalPlayer.GetMaxNutritionFat();
                            carboImage.fillAmount = fillAmount3;
                            float carboMinValue = 0f;
                            float carboMaxValue = LocalPlayerConditionModule.GetMaxNutritionCarbo();
                            float carboValue = LocalPlayerConditionModule.GetNutritionCarbo();
                            GUILayout.HorizontalSlider(carboValue, carboMinValue, carboMaxValue);
                            GUILayout.Box(carboImage.mainTexture);
                            GUILayout.Box(carboBGImage.mainTexture);
                        }

                        GUI.backgroundColor = IconColors.GetColor(IconColors.Icon.Hydration);
                        using (var hydrationScope = new GUILayout.HorizontalScope(GUI.skin.box))
                        {
                            Image hydrationImage = (Image)LocalWatch.m_Canvas.transform.Find("Hydration").GetComponent<Image>();
                            Image hydrationBGImage = (Image)LocalWatch.m_Canvas.transform.Find("HydrationBG").GetComponent<Image>();
                            float fillAmount4 = LocalPlayer.GetHydration() / LocalPlayer.GetMaxHydration();
                            hydrationImage.fillAmount = fillAmount4;
                            float hydrationMinValue = 0f;
                            float hydrationMaxValue = LocalPlayerConditionModule.GetMaxHydration();
                            float hydrationValue = LocalPlayerConditionModule.GetHydration();
                            GUILayout.HorizontalSlider(hydrationValue, hydrationMinValue, hydrationMaxValue);
                            GUILayout.Box(((Image)LocalWatch.m_Canvas.transform.Find("Hydration").GetComponent<Image>()).mainTexture);
                            GUILayout.Box(((Image)LocalWatch.m_Canvas.transform.Find("HydrationBG").GetComponent<Image>()).mainTexture);
                        }
                    }
                }
                else
                {
                    using (var infoScope = new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        GUI.color = Color.yellow;
                        GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), GUI.skin.label);
                        GUI.color = Color.white;
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(MacronutrientsBox));
            }
        }

        private void CompassBox()
        {
            try
            {
                if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                {
                    using (var compassScope = new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        LocalPlayer.GetGPSCoordinates(out int gps_lat, out int gps_long);
                        string GPSCoordinatesW = gps_lat.ToString();
                        string GPSCoordinatesS = gps_long.ToString();
                        GUILayout.Label($"South: { GPSCoordinatesS}", GUI.skin.label);
                        GUILayout.Label($"West: { GPSCoordinatesW}", GUI.skin.label);
                    }
                }
                else
                {
                    using (var infoScope = new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        GUI.color = Color.yellow;
                        GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), GUI.skin.label);
                        GUI.color = Color.white;
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(CompassBox));
            }
        }
    }
}
