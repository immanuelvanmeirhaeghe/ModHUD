using AIs;
using ModHUD.Data.Enums;
using ModHUD.Managers;
using ModManager.Data.Interfaces;
using ModManager.Data.Modding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace ModHUD
{

    /// <summary>
    /// ModHUD is a mod for Green Hell that adds a player HUD which displays player stats and compass.
    /// Press Keypad0 (default) or the key configurable in ModAPI to open the mod screen.
    /// </summary>
    public class ModHUD : MonoBehaviour
    {
        private static ModHUD Instance;
        private static readonly string RuntimeConfiguration = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), "RuntimeConfiguration.xml");
     
        private static readonly string ModName = nameof(ModHUD);
        private static readonly float ModHUDScreenTotalWidth = 300f;
        private static readonly float ModHUDScreenTotalHeight = 300f;
        private static readonly float ModHUDScreenMinWidth = 300f;
        private static readonly float ModHUDScreenMaxWidth = 300f;
        private static readonly float ModHUDScreenMinHeight = 300f;
        private static readonly float ModHUDScreenMaxHeight = 300f;
        private static float ModHUDScreenStartPositionX { get; set; } = 0f;
        private static float ModHUDScreenStartPositionY { get; set; } = Screen.height - ModHUDScreenTotalHeight - 75f;
        private static bool IsModHUDScreenMinimized { get; set; } = false;
        private bool ShowModHUDScreen = true;
        private static int ModHUDScreenId { get; set; } = 0;

        public static Rect ModHUDScreen = new Rect(ModHUDScreenStartPositionX, ModHUDScreenStartPositionY, ModHUDScreenTotalWidth, ModHUDScreenTotalHeight);

        private static Player LocalPlayer;
        private static ItemsManager LocalItemsManager;
        private static HUDManager LocalHUDManager;
        private static Watch LocalWatch;
        private static PlayerConditionModule LocalPlayerConditionModule;
        private static StylingManager LocalStylingManager;

        public bool IsModActiveForMultiplayer { get; private set; }
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public IConfigurableMod SelectedMod { get; set; }
        // Based on Watch
        public static GameObject LocalCompass = new GameObject(nameof(LocalCompass));
        public static GameObject LocalHUDCanvas = new GameObject(nameof(LocalHUDCanvas));
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

        public static string OnlyForSinglePlayerOrHostMessage()
            => $"Only available for single player or when host. Host can activate using ModManager.";
        private string PermissionChangedMessage(string permission, string reason)
               => $"Permission to use mods and cheats in multiplayer was {permission} because {reason}.";
        private string HUDBigInfoMessage(string message, MessageType messageType, Color? headcolor = null)
            => $"<color=#{(headcolor != null ? ColorUtility.ToHtmlStringRGBA(headcolor.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))}>{messageType}</color>\n{message}";
        
        private KeyCode ShortcutKey { get; set; } = KeyCode.Keypad0;

        protected virtual void ModManager_onPermissionValueChanged(bool optionValue)
        {
            string reason = optionValue ? "the game host allowed usage" : "the game host did not allow usage";
            IsModActiveForMultiplayer = optionValue;

            ShowHUDBigInfo(
                          (optionValue ?
                            HUDBigInfoMessage(PermissionChangedMessage($"granted", $"{reason}"), MessageType.Info, Color.green)
                            : HUDBigInfoMessage(PermissionChangedMessage($"revoked", $"{reason}"), MessageType.Info, Color.yellow))
                            );
        }

        public void ShowHUDBigInfo(string text, float duration = 3f)
        {
            string header = $"{ModName} Info";
            string textureName = HUDInfoLogTextureType.Count.ToString();
            HUDBigInfo obj = (HUDBigInfo)LocalHUDManager.GetHUD(typeof(HUDBigInfo));
            HUDBigInfoData.s_Duration = duration;
            HUDBigInfoData data = new HUDBigInfoData
            {
                m_Header = header,
                m_Text = text,
                m_TextureName = textureName,
                m_ShowTime = Time.time
            };
            obj.AddInfo(data);
            obj.Show(show: true);
        }

        public void ShowHUDInfoLog(string itemID, string localizedTextKey)
        {
            Localization localization = GreenHellGame.Instance.GetLocalization();
            var messages = ((HUDMessages)LocalHUDManager.GetHUD(typeof(HUDMessages)));
            messages.AddMessage($"{localization.Get(localizedTextKey)}  {localization.Get(itemID)}");
        }
        
        public KeyCode GetShortcutKey(string buttonID)
        {
            var ConfigurableModList = GetModList();
            if (ConfigurableModList != null && ConfigurableModList.Count > 0)
            {
                SelectedMod = ConfigurableModList.Find(cfgMod => cfgMod.ID == ModName);
                return SelectedMod.ConfigurableModButtons.Find(cfgButton => cfgButton.ID == buttonID).ShortcutKey;
            }
            else
            {
                if (buttonID==nameof(ShortcutKey))
                {
                    return KeyCode.Keypad7;
                }
            }
            return KeyCode.None;
        }

        private List<IConfigurableMod> GetModList()
        {
            List<IConfigurableMod> modList = new List<IConfigurableMod>();
            try
            {
                if (File.Exists(RuntimeConfiguration))
                {
                    using (XmlReader configFileReader = XmlReader.Create(new StreamReader(RuntimeConfiguration)))
                    {
                        while (configFileReader.Read())
                        {
                            configFileReader.ReadToFollowing("Mod");
                            do
                            {
                                string gameID = GameID.GreenHell.ToString();
                                string modID = configFileReader.GetAttribute(nameof(IConfigurableMod.ID));
                                string uniqueID = configFileReader.GetAttribute(nameof(IConfigurableMod.UniqueID));
                                string version = configFileReader.GetAttribute(nameof(IConfigurableMod.Version));

                                var configurableMod = new ConfigurableMod(gameID, modID, uniqueID, version);

                                configFileReader.ReadToDescendant("Button");
                                do
                                {
                                    string buttonID = configFileReader.GetAttribute(nameof(IConfigurableModButton.ID));
                                    string buttonKeyBinding = configFileReader.ReadElementContentAsString();

                                    configurableMod.AddConfigurableModButton(buttonID, buttonKeyBinding);

                                } while (configFileReader.ReadToNextSibling("Button"));

                                if (!modList.Contains(configurableMod))
                                {
                                    modList.Add(configurableMod);
                                }

                            } while (configFileReader.ReadToNextSibling("Mod"));
                        }
                    }
                }
                return modList;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(GetModList));
                modList = new List<IConfigurableMod>();
                return modList;
            }
        }

        protected virtual void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModName}:{methodName}] throws exception -  {exc.TargetSite?.Name}:\n{exc.Message}\n{exc.InnerException}\n{exc.Source}\n{exc.StackTrace}";
            ModAPI.Log.Write(info);
            Debug.Log(info);
            //ShowHUDBigInfo(HUDBigInfoMessage(info, MessageType.Error, Color.red));
        }

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
           
        protected virtual void EnableCursor(bool blockPlayer = false)
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

        protected virtual void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
            ShortcutKey = GetShortcutKey(nameof(ShortcutKey));
        }

        protected virtual void Update()
        {
            if (Input.GetKeyDown(ShortcutKey))
            {
                if (!ShowModHUDScreen)
                {
                    InitData();
                }
                ToggleShowUI();
            }
        }

        protected virtual void ToggleShowUI()
        {
            ShowModHUDScreen = !ShowModHUDScreen;
        }

        protected virtual void OnGUI()
        {
            if (ShowModHUDScreen)
            {
                InitData();
                InitSkinUI();
                ShowModHUDWindow();
            }
        }

        protected virtual void InitData()
        {
            LocalHUDManager = HUDManager.Get();
            LocalPlayer = Player.Get();
            LocalWatch = Watch.Get();
            LocalPlayerConditionModule = PlayerConditionModule.Get();
            LocalItemsManager = ItemsManager.Get();
            LocalStylingManager = StylingManager.Get();
            InitLocalCompass();
        }

        protected virtual void InitLocalCompass()
        {
            LocalCompass = LocalWatch?.m_Canvas.transform.Find("Compass").gameObject.transform.Find("CompassIcon").gameObject;
            if (LocalCompass == null)
            {
                Input.compass.enabled = true;
                LocalCompass = new GameObject(nameof(LocalCompass));
            }
        }

        protected virtual void InitLocalHUDCanvas()
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

        protected virtual void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        protected virtual void ShowModHUDWindow()
        {
            try
            {
                if (ModHUDScreenId <= 0)
                {
                    ModHUDScreenId = ModHUDScreen.GetHashCode();
                }                
                ModHUDScreen = GUILayout.Window(ModHUDScreenId, ModHUDScreen, InitModHUDScreen, string.Empty,
                                                                                        GUI.skin.label,
                                                                                        GUILayout.ExpandWidth(true),
                                                                                        GUILayout.MinWidth(ModHUDScreenMinWidth),
                                                                                        GUILayout.MaxWidth(ModHUDScreenMaxWidth),
                                                                                        GUILayout.ExpandHeight(true),
                                                                                        GUILayout.MinHeight(ModHUDScreenMinHeight),
                                                                                        GUILayout.MaxHeight(ModHUDScreenMaxHeight)
                                                                                       );
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(ShowModHUDWindow));
            }
        }

        protected virtual void InitModHUDScreen(int windowID)
        {
            ModHUDScreenStartPositionX = ModHUDScreen.x;
            ModHUDScreenStartPositionY = ModHUDScreen.y;

            using (new GUILayout.VerticalScope(GUI.skin.label))
            {
                CompassBox();
                MacronutrientsBox();
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        protected virtual void MacronutrientsBox()
        {
            try
            {
                if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                {
                    using (new GUILayout.VerticalScope(GUI.skin.label))
                    {
                        using (new GUILayout.HorizontalScope(GUI.skin.label))
                        {
                            GUI.backgroundColor = IconColors.GetColor(IconColors.Icon.Fat);
                            GUI.color = IconColors.GetColor(IconColors.Icon.Fat);
                            float fatMinValue = 0f;
                            float fatMaxValue = LocalPlayerConditionModule.GetMaxNutritionFat();
                            float fatValue = LocalPlayerConditionModule.GetNutritionFat();
                            //if(LocalItemsManager.m_ItemIconsSprites.TryGetValue("Watch_fat_icon", out Sprite localIcon))
                            //{
                            //    GUILayout.Box(localIcon.texture, GUI.skin.label);
                            //}
                            GUILayout.Label("fats");
                            GUILayout.HorizontalSlider(fatValue, fatMinValue, fatMaxValue, GUILayout.Width(175f));
                        }

                        using (new GUILayout.HorizontalScope(GUI.skin.label))
                        {
                            GUI.backgroundColor = IconColors.GetColor(IconColors.Icon.Carbo);
                            GUI.color = IconColors.GetColor(IconColors.Icon.Carbo);
                            float carboMinValue = 0f;
                            float carboMaxValue = LocalPlayerConditionModule.GetMaxNutritionCarbo();
                            float carboValue = LocalPlayerConditionModule.GetNutritionCarbo();
                            //if(LocalItemsManager.m_ItemIconsSprites.TryGetValue("Watch_carbo_icon", out Sprite localIcon))
                            //{
                            //    GUILayout.Box(localIcon.texture, GUI.skin.label);
                            //}
                            GUILayout.Label("carbs");
                            GUILayout.HorizontalSlider(carboValue, carboMinValue, carboMaxValue, GUILayout.Width(175f));
                        }

                        using (new GUILayout.HorizontalScope(GUI.skin.label))
                        {
                            GUI.backgroundColor = IconColors.GetColor(IconColors.Icon.Hydration);
                            GUI.color = IconColors.GetColor(IconColors.Icon.Hydration);
                            float hydrationMinValue = 0f;
                            float hydrationMaxValue = LocalPlayerConditionModule.GetMaxHydration();
                            float hydrationValue = LocalPlayerConditionModule.GetHydration();
                            //if(LocalItemsManager.m_ItemIconsSprites.TryGetValue("Watch_water_icon", out Sprite localIcon))
                            //{
                            //    GUILayout.Box(localIcon.texture, GUI.skin.label);
                            //}
                            GUILayout.Label("hydration");
                            GUILayout.HorizontalSlider(hydrationValue, hydrationMinValue, hydrationMaxValue, GUILayout.Width(175f));
                        }

                        using (new GUILayout.HorizontalScope(GUI.skin.label))
                        {
                            GUI.backgroundColor = IconColors.GetColor(IconColors.Icon.Proteins);
                            GUI.color = IconColors.GetColor(IconColors.Icon.Proteins);
                            float proteinsMinValue = 0f;
                            float proteinsMaxValue = LocalPlayerConditionModule.GetMaxNutritionProtein();
                            float proteinsValue = LocalPlayerConditionModule.GetNutritionProtein();
                            //if(LocalItemsManager.m_ItemIconsSprites.TryGetValue("Watch_protein_icon", out Sprite localIcon))
                            //{
                            //    GUILayout.Box(localIcon.texture, GUI.skin.label);
                            //}
                            GUILayout.Label("proteins");
                            GUILayout.HorizontalSlider(proteinsValue, proteinsMinValue, proteinsMaxValue, GUILayout.Width(175f));
                        }
                    }
                }
                else
                {
                    using (new GUILayout.VerticalScope(GUI.skin.label))
                    {
                        GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), LocalStylingManager.ColoredCommentLabel(Color.yellow));
                    }
                }
                GUI.color = LocalStylingManager.DefaultColor;
                GUI.backgroundColor = LocalStylingManager.DefaultBackGroundColor;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(MacronutrientsBox));
            }
        }

        protected virtual void CompassBox()
        {
            try
            {
                if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                {
                    using (new GUILayout.VerticalScope(GUI.skin.label))
                    {
                        GUI.backgroundColor = LocalStylingManager.DefaultClearColor;
                        using (new GUILayout.HorizontalScope(GUI.skin.box))
                        {
                            Vector3 forward = LocalPlayer.gameObject.transform.forward;
                            float angle = Vector3.Angle(Vector3.forward, forward);
                            if (forward.x < 0f)
                            {
                                angle = 360f - angle;
                            }
                            Quaternion rotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
                            string direction = string.Empty;
                            string zDir = string.Empty;
                            string wDir = string.Empty;

                            float z = (float)Math.Round(rotation.z, 2, MidpointRounding.ToEven);
                            float w = (float)Math.Round(rotation.w, 2, MidpointRounding.ToEven);

                            if (z == 0.0f && Math.Abs(w) == 1.0f)
                            {
                                GUI.color = IconColors.GetColor(IconColors.Icon.Proteins);
                                direction = "N";
                                zDir = "^ ^";
                                wDir = "- -";
                            }

                            if (z > 0.0f && z < 0.80f && w >= -1.0f && w < -0.70f)
                            {
                                GUI.color = LocalStylingManager.DefaultColor;
                                direction = "NW";
                                zDir = "^ ^";
                                wDir = "< <";
                            }

                            if (z >= 0.70f && w <= -0.70f)
                            {
                                GUI.color = IconColors.GetColor(IconColors.Icon.Hydration);
                                direction = "W";
                                zDir = $"- -";
                                wDir = $"< <";
                            }

                            if (z >= 0.80f && z<= 1.0f  && w >= -0.60f && w < 0.0f)
                            {
                                GUI.color = LocalStylingManager.DefaultColor;
                                direction = "SW";
                                zDir = "v v";
                                wDir = "< <";
                            }

                            if (Math.Abs(z) == 1.0f && w == 0.0f)
                            {
                                direction = "S";
                                zDir = "v v";
                                wDir = "- -";
                            }

                            if (z > 0.70f && z <= 1.0f && w > 0.0f && w < 0.70f)
                            {
                                GUI.color = LocalStylingManager.DefaultColor;
                                direction = "SE";
                                zDir = "v v";
                                wDir = "> >";
                            }

                            if (z >= 0.70f && w >= 0.70f)
                            {
                                GUI.color = LocalStylingManager.DefaultColor;
                                direction = "E";
                                zDir = $"- -";
                                wDir = $"> >";
                            }

                            if (z > 0.0f && z < 0.70f  && w >= 0.80f && w < 1.0f)
                            {
                                GUI.color = LocalStylingManager.DefaultColor;
                                direction = "NE";
                                zDir = "^ ^";
                                wDir = "> >";
                            }

                            GUILayout.Label($"{zDir}", LocalStylingManager.CompassLabel, GUILayout.Width(50f));
                            GUILayout.Label($"{direction}", LocalStylingManager.CompassLabel, GUILayout.Width(200f));
                            GUILayout.Label($"{wDir}", LocalStylingManager.CompassLabel, GUILayout.Width(50f));
                        }

                        GUI.backgroundColor = LocalStylingManager.DefaultBackGroundColor;
                        using (new GUILayout.VerticalScope(GUI.skin.label))
                        {
                            LocalPlayer.GetGPSCoordinates(out int gps_lat, out int gps_long);
                            string GPSCoordinatesW = gps_lat.ToString();
                            string GPSCoordinatesS = gps_long.ToString();
                            using (new GUILayout.HorizontalScope(GUI.skin.label))
                            {
                                GUI.color = LocalStylingManager.DefaultColor;
                                GUILayout.Label($"{GPSCoordinatesW}", LocalStylingManager.PositionLabel, GUILayout.Width(75f));
                                GUI.color = IconColors.GetColor(IconColors.Icon.Hydration);
                                GUILayout.Label($"\'W", LocalStylingManager.DirectionLabel, GUILayout.Width(75f));
                            }
                            using (new GUILayout.HorizontalScope(GUI.skin.label))
                            {
                                GUI.color = LocalStylingManager.DefaultColor;
                                GUILayout.Label($"{GPSCoordinatesS}", LocalStylingManager.PositionLabel, GUILayout.Width(75f));
                                GUI.color = IconColors.GetColor(IconColors.Icon.Proteins);
                                GUILayout.Label($"\'S", LocalStylingManager.DirectionLabel, GUILayout.Width(75f));
                            }
                        }
                   
                    }
                }
                else
                {
                    using (new GUILayout.VerticalScope(GUI.skin.label))
                    {
                        GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), LocalStylingManager.ColoredCommentLabel(Color.yellow));
                    }
                }
                GUI.color = LocalStylingManager.DefaultColor;
                GUI.backgroundColor = LocalStylingManager.DefaultBackGroundColor;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(CompassBox));
            }
        }
    
    }
}
