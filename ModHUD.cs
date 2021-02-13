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
        private static readonly float ModScreenTotalWidth = 200f;
        private static readonly float ModScreenTotalHeight = 200f;
        private static readonly float ModScreenMinWidth = 200f;
        private static readonly float ModScreenMaxWidth = 200f;
        private static readonly float ModScreenMinHeight = 200f;
        private static readonly float ModScreenMaxHeight = 200f;
        private static float ModScreenStartPositionX { get; set; } = 0f;
        private static float ModScreenStartPositionY { get; set; } = Screen.height - ModScreenTotalHeight;
        private static bool IsMinimized { get; set; } = false;
        private bool ShowUI = false;
        public static Rect ModHUDScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);

        private static Player LocalPlayer;
        private static HUDManager LocalHUDManager;
        private static Watch LocalWatch;
        private static PlayerConditionModule LocalPlayerConditionModule;

        // Based on Watch
        public static GameObject LocalHUDCanvas;
        public static Dictionary<int, WatchData> LocalHUDCanvasDatas = new Dictionary<int, WatchData>();
        public static Image LocalProteins;
        public static Image LocalProteinsBG;
        public static Image LocalFat;
        public static Image LocalFatBG;
        public static Image LocalCarbs;
        public static Image LocalCarbsBG;
        public static Image LocalHydration;
        public static Image LocalHydrationBG;

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
            LocalHUDCanvas = LocalWatch?.m_Canvas?.transform.Find("Macronutrients").gameObject;
            if (LocalHUDCanvas != null)
            {
                LocalFatBG = LocalHUDCanvas?.transform.Find("FatBG").GetComponent<Image>();
                LocalFat = LocalHUDCanvas?.transform.Find("Fat").GetComponent<Image>();
                if (LocalFat != null)
                {
                    LocalFat.type = Image.Type.Filled;
                    LocalFat.color = IconColors.GetColor(IconColors.Icon.Fat);
                }

                LocalCarbsBG = LocalHUDCanvas?.transform.Find("CarboBG").GetComponent<Image>();
                LocalCarbs = LocalHUDCanvas?.transform.Find("Carbo").GetComponent<Image>();
                if (LocalCarbs != null)
                {
                    LocalCarbs.type = Image.Type.Filled;
                    LocalCarbs.color = IconColors.GetColor(IconColors.Icon.Carbo);
                }

                LocalProteinsBG = LocalHUDCanvas?.transform.Find("ProteinsBG").GetComponent<Image>();
                LocalProteins = LocalHUDCanvas?.transform.Find("Proteins").GetComponent<Image>();
                if (LocalProteins != null)
                {
                    LocalProteins.type = Image.Type.Filled;
                    LocalProteins.color = IconColors.GetColor(IconColors.Icon.Proteins);
                }

                LocalHydrationBG = LocalHUDCanvas?.transform.Find("HydrationBG").GetComponent<Image>();
                LocalHydration = LocalHUDCanvas?.transform.Find("Hydration").GetComponent<Image>();
                if (LocalHydration != null)
                {
                    LocalHydration.type = Image.Type.Filled;
                    LocalHydration.color = IconColors.GetColor(IconColors.Icon.Hydration);
                }
            }
            else
            {
                LocalHUDCanvas = new GameObject(nameof(LocalHUDCanvas));
            }
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
                                                                                        GUI.skin.label,
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

        private void InitModHUDScreen(int windowID)
        {
            ModScreenStartPositionX = ModHUDScreen.x;
            ModScreenStartPositionY = ModHUDScreen.y;

            using (var modContentScope = new GUILayout.VerticalScope(GUI.skin.label))
            {
                CompassBox();
                MacronutrientsBox();
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void MacronutrientsBox()
        {
            try
            {
                Color defaultC = GUI.color;
                Color defaultCBG = GUI.backgroundColor;
                if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                {
                    using (var macroNutrientsScope = new GUILayout.VerticalScope(GUI.skin.label))
                    {
                        using (var fatScope = new GUILayout.HorizontalScope(GUI.skin.label))
                        {
                            GUI.backgroundColor = IconColors.GetColor(IconColors.Icon.Fat);
                            GUI.color = IconColors.GetColor(IconColors.Icon.Fat);
                            float fatMinValue = 0f;
                            float fatMaxValue = LocalPlayerConditionModule.GetMaxNutritionFat();
                            float fatValue = LocalPlayerConditionModule.GetNutritionFat();
                            GUILayout.Label("fats");
                            GUILayout.HorizontalSlider(fatValue, fatMinValue, fatMaxValue);
                        }

                        using (var carboScope = new GUILayout.HorizontalScope(GUI.skin.label))
                        {
                            GUI.backgroundColor = IconColors.GetColor(IconColors.Icon.Carbo);
                            GUI.color = IconColors.GetColor(IconColors.Icon.Carbo);
                            float carboMinValue = 0f;
                            float carboMaxValue = LocalPlayerConditionModule.GetMaxNutritionCarbo();
                            float carboValue = LocalPlayerConditionModule.GetNutritionCarbo();
                            GUILayout.Label("carbs");
                            GUILayout.HorizontalSlider(carboValue, carboMinValue, carboMaxValue);
                        }

                        using (var hydrationScope = new GUILayout.HorizontalScope(GUI.skin.label))
                        {
                            GUI.backgroundColor = IconColors.GetColor(IconColors.Icon.Hydration);
                            GUI.color = IconColors.GetColor(IconColors.Icon.Hydration);
                            float hydrationMinValue = 0f;
                            float hydrationMaxValue = LocalPlayerConditionModule.GetMaxHydration();
                            float hydrationValue = LocalPlayerConditionModule.GetHydration();
                            GUILayout.Label("hydration");
                            GUILayout.HorizontalSlider(hydrationValue, hydrationMinValue, hydrationMaxValue);
                        }

                        using (var proteinScope = new GUILayout.HorizontalScope(GUI.skin.label))
                        {
                            GUI.backgroundColor = IconColors.GetColor(IconColors.Icon.Proteins);
                            GUI.color = IconColors.GetColor(IconColors.Icon.Proteins);
                            float proteinsMinValue = 0f;
                            float proteinsMaxValue = LocalPlayerConditionModule.GetMaxNutritionProtein();
                            float proteinsValue = LocalPlayerConditionModule.GetNutritionProtein();
                            GUILayout.Label("proteins");
                            GUILayout.HorizontalSlider(proteinsValue, proteinsMinValue, proteinsMaxValue);
                        }

                        using (var fatImageScope = new GUILayout.HorizontalScope(GUI.skin.label))
                        {
                            float fillAmount2 = LocalPlayer.GetNutritionFat() / LocalPlayer.GetMaxNutritionFat();
                            LocalFat.fillAmount = fillAmount2;
                            GUILayout.Box(LocalFat.mainTexture);
                            GUILayout.Box(LocalFatBG.mainTexture);
                        }

                        using (var carbsImageScope = new GUILayout.HorizontalScope(GUI.skin.label))
                        {
                            float fillAmount3 = LocalPlayer.GetNutritionCarbo() / LocalPlayer.GetMaxNutritionCarbo();
                            LocalCarbs.fillAmount = fillAmount3;
                            GUILayout.Box(LocalCarbs.mainTexture);
                            GUILayout.Box(LocalCarbsBG.mainTexture);
                        }

                        using (var hydrationImageScope = new GUILayout.HorizontalScope(GUI.skin.label))
                        {
                            float fillAmount4 = LocalPlayer.GetHydration() / LocalPlayer.GetMaxHydration();
                            LocalHydration.fillAmount = fillAmount4;
                            GUILayout.Box(LocalHydration.mainTexture);
                            GUILayout.Box(LocalHydrationBG.mainTexture);
                        }

                        using (var proteinImageScope = new GUILayout.HorizontalScope(GUI.skin.label))
                        {
                            float fillAmount = LocalPlayer.GetNutritionProtein() / LocalPlayer.GetMaxNutritionProtein();
                            LocalProteins.fillAmount = fillAmount;
                            GUILayout.Box(LocalProteins.mainTexture);
                            GUILayout.Box(LocalProteinsBG.mainTexture);
                        }
                    }
                }
                else
                {
                    using (var infoScope = new GUILayout.VerticalScope(GUI.skin.label))
                    {
                        GUI.color = Color.yellow;
                        GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), GUI.skin.label);
                        GUI.color = Color.white;
                    }
                }
                GUI.color = defaultC;
                GUI.backgroundColor = defaultCBG;
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
                Color defaultC = GUI.color;
                if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                {
                    using (var compassScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        LocalPlayer.GetGPSCoordinates(out int gps_lat, out int gps_long);
                        string GPSCoordinatesW = gps_lat.ToString();
                        string GPSCoordinatesS = gps_long.ToString();
                        GUI.color = Color.red;
                        GUILayout.Label($"South: { GPSCoordinatesS}", GUI.skin.label);
                        GUI.color = Color.white;
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
                GUI.color = defaultC;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(CompassBox));
            }
        }
    }
}
