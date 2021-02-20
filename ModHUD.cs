using ModHUD.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        private static readonly float ModScreenTotalWidth = 300f;
        private static readonly float ModScreenTotalHeight = 300f;
        private static readonly float ModScreenMinWidth = 300f;
        private static readonly float ModScreenMaxWidth = 300f;
        private static readonly float ModScreenMinHeight = 300f;
        private static readonly float ModScreenMaxHeight = 300f;
        private static float ModScreenStartPositionX { get; set; } = 0f;
        private static float ModScreenStartPositionY { get; set; } = Screen.height - ModScreenTotalHeight- 75f;
        private static bool IsMinimized { get; set; } = false;
        private bool ShowUI = false;
        public static Rect ModHUDScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);

        private static Player LocalPlayer;
        private static ItemsManager LocalItemsManager;
        private static HUDManager LocalHUDManager;
        private static Watch LocalWatch;
        private static PlayerConditionModule LocalPlayerConditionModule;

        private static CompareArrayByDimension LocalDimensionComparer = new CompareArrayByDimension();
        private static SortedDictionary<int, List<string>> LocalSortedTextures = new SortedDictionary<int, List<string>>(LocalDimensionComparer);
        private static readonly string ReportPath = $"{Application.dataPath.Replace("GH_Data", "Logs")}" + "/ModHUD_textures_dump_" + DateTime.Now.ToLongTimeString().Replace(':', '_') + ".log";

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

            if (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(KeyCode.T))
            {
                DumpTextures();
                ShowHUDBigInfo(HUDBigInfoMessage($"Dumped all texture info to {ReportPath}", MessageType.Info));
            }

            if (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(KeyCode.Y))
            {
                DumpIconsInfo();
                ShowHUDBigInfo(HUDBigInfoMessage($"Dumped all item icon info to {Application.dataPath.Replace("GH_Data", "Logs")}/{nameof(ModHUD)}.log", MessageType.Info));
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
            LocalItemsManager = ItemsManager.Get();
            // InitLocalHUDCanvas();
            InitLocalCompass();
        }

        private void InitLocalCompass()
        {
            LocalCompass = LocalWatch?.m_Canvas.transform.Find("Compass").gameObject.transform.Find("CompassIcon").gameObject;
            if (LocalCompass == null)
            {
                LocalCompass = new GameObject(nameof(LocalCompass));
            }
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
                ModHUDScreen = GUILayout.Window(wid, ModHUDScreen, InitModHUDScreen, "",
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
                            if(LocalItemsManager.m_ItemIconsSprites.TryGetValue("Watch_fat_icon", out Sprite localIcon))
                            {
                                GUILayout.Box(localIcon.texture, GUI.skin.label);
                            }
                            GUILayout.Label("fats");
                            GUILayout.HorizontalSlider(fatValue, fatMinValue, fatMaxValue, GUILayout.Width(175f));
                        }

                        using (var carboScope = new GUILayout.HorizontalScope(GUI.skin.label))
                        {
                            GUI.backgroundColor = IconColors.GetColor(IconColors.Icon.Carbo);
                            GUI.color = IconColors.GetColor(IconColors.Icon.Carbo);
                            float carboMinValue = 0f;
                            float carboMaxValue = LocalPlayerConditionModule.GetMaxNutritionCarbo();
                            float carboValue = LocalPlayerConditionModule.GetNutritionCarbo();
                            if(LocalItemsManager.m_ItemIconsSprites.TryGetValue("Watch_carbo_icon", out Sprite localIcon))
                            {
                                GUILayout.Box(localIcon.texture, GUI.skin.label);
                            }
                            GUILayout.Label("carbs");
                            GUILayout.HorizontalSlider(carboValue, carboMinValue, carboMaxValue, GUILayout.Width(175f));
                        }

                        using (var hydrationScope = new GUILayout.HorizontalScope(GUI.skin.label))
                        {
                            GUI.backgroundColor = IconColors.GetColor(IconColors.Icon.Hydration);
                            GUI.color = IconColors.GetColor(IconColors.Icon.Hydration);
                            float hydrationMinValue = 0f;
                            float hydrationMaxValue = LocalPlayerConditionModule.GetMaxHydration();
                            float hydrationValue = LocalPlayerConditionModule.GetHydration();
                            if(LocalItemsManager.m_ItemIconsSprites.TryGetValue("Watch_water_icon", out Sprite localIcon))
                            {
                                GUILayout.Box(localIcon.texture, GUI.skin.label);
                            }
                            GUILayout.Label("hydration");
                            GUILayout.HorizontalSlider(hydrationValue, hydrationMinValue, hydrationMaxValue, GUILayout.Width(175f));
                        }

                        using (var proteinScope = new GUILayout.HorizontalScope(GUI.skin.label))
                        {
                            GUI.backgroundColor = IconColors.GetColor(IconColors.Icon.Proteins);
                            GUI.color = IconColors.GetColor(IconColors.Icon.Proteins);
                            float proteinsMinValue = 0f;
                            float proteinsMaxValue = LocalPlayerConditionModule.GetMaxNutritionProtein();
                            float proteinsValue = LocalPlayerConditionModule.GetNutritionProtein();
                            if(LocalItemsManager.m_ItemIconsSprites.TryGetValue("Watch_protein_icon", out Sprite localIcon))
                            {
                                GUILayout.Box(localIcon.texture, GUI.skin.label);
                            }
                            GUILayout.Label("proteins");
                            GUILayout.HorizontalSlider(proteinsValue, proteinsMinValue, proteinsMaxValue, GUILayout.Width(175f));
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
                GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fixedWidth = 50f,
                    fontSize = 20
                };

                if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                {
                    using (var compassScope = new GUILayout.VerticalScope(GUI.skin.label))
                    {
                        using (var directionScope = new GUILayout.HorizontalScope(GUI.skin.label))
                        {
                            Vector3 forward = LocalPlayer.gameObject.transform.forward;
                            float angle = Vector3.Angle(Vector3.forward, forward);
                            if (forward.x < 0f)
                            {
                                angle = 360f - angle;
                            }
                            Quaternion rotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
                            string direction = string.Empty;
                            double z = Math.Round(rotation.z, 2, MidpointRounding.ToEven);
                            double w = Math.Round(rotation.w, 2, MidpointRounding.ToEven);

                            if (z == 0.0f && Math.Abs(Math.Round(w)) == 1.0f)
                            {
                                GUI.color = defaultC;
                                direction = "N";
                            }
                            if (z >= 0.1f && z < 0.7f && w >= -1.0f && w < -0.7f)
                            {
                                GUI.color = defaultC;
                                direction = "NW";
                            }
                            if (z >= 0.7f && z < 0.8f && w >= -0.7f && w < -0.6f)
                            {
                                GUI.color = IconColors.GetColor(IconColors.Icon.Hydration);
                                direction = "W";
                            }
                            if (z >= 0.8f && z< 1.0f  && w >= -0.6f && w < 0.0f)
                            {
                                GUI.color = defaultC;
                                direction = "SW";
                            }
                            if (Math.Abs(Math.Round(z)) == 1.0f && w == 0.0f)
                            {
                                GUI.color = IconColors.GetColor(IconColors.Icon.Proteins);
                                direction = "S";
                            }
                            if (z <= 0.8f && z > 0.7f && w >= 0.1f && w < 0.7f)
                            {
                                GUI.color = defaultC;
                                direction = "SE";
                            }
                            if (z <= 0.7f && z > 0.6f && w >= 0.7f && w < 0.8f)
                            {
                                GUI.color = defaultC;
                                direction = "E";
                            }
                            if (z <= 0.6f && z > 0.0f && w >= 0.8f && w < 1.0f)
                            {
                                GUI.color = defaultC;
                                direction = "NE";
                            }
                            GUILayout.Label($"{z}", GUI.skin.label, GUILayout.Width(25f));
                            GUILayout.Label($"{direction}", GUI.skin.label);
                            GUILayout.Label($"{w}", GUI.skin.label,GUILayout.Width(25f));
                        }

                        using (var positionScope = new GUILayout.VerticalScope(GUI.skin.label))
                        {
                            LocalPlayer.GetGPSCoordinates(out int gps_lat, out int gps_long);
                            string GPSCoordinatesW = gps_lat.ToString();
                            string GPSCoordinatesS = gps_long.ToString();
                            using (var coordinatesWScope = new GUILayout.HorizontalScope(GUI.skin.label))
                            {
                                GUI.color = defaultC;
                                GUILayout.Label($"{ GPSCoordinatesW}", labelStyle);
                                GUI.color = IconColors.GetColor(IconColors.Icon.Hydration);
                                GUILayout.Label($"\'W", labelStyle);
                            }
                            using (var coordinatesSScope = new GUILayout.HorizontalScope(GUI.skin.label))
                            {
                                GUI.color = defaultC;
                                GUILayout.Label($"{ GPSCoordinatesS}", labelStyle);
                                GUI.color = IconColors.GetColor(IconColors.Icon.Proteins);
                                GUILayout.Label($"\'S", labelStyle);
                            }
                        }
                    }
                }
                else
                {
                    using (var infoScope = new GUILayout.VerticalScope(GUI.skin.label))
                    {
                        GUI.color = Color.yellow;
                        GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), GUI.skin.label);
                    }
                }
                GUI.color = defaultC;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(CompassBox));
            }
        }

        private void DumpTextures()
        {
            try
            {
                FileStream fileStream = File.Create(ReportPath);
                string text2 = string.Empty;
                LocalSortedTextures.Clear();
                Texture[] array = Resources.FindObjectsOfTypeAll<Texture>();
                int num = 0;
                foreach (Texture texture in array)
                {
                    int num2 = texture.width * texture.height;
                    if (!LocalSortedTextures.ContainsKey(num2))
                    {
                        LocalSortedTextures.Add(num2, new List<string>());
                    }
                    else
                    {
                        LocalSortedTextures[num2].Add(texture.name);
                    }
                    num += num2;
                }
                SortedDictionary<int, List<string>>.Enumerator enumerator = LocalSortedTextures.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    enumerator.Current.Value.Sort();
                }
                enumerator = LocalSortedTextures.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    enumerator.Current.Value.Sort();
                }
                enumerator = LocalSortedTextures.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    List<string>.Enumerator enumerator2 = enumerator.Current.Value.GetEnumerator();
                    while (enumerator2.MoveNext())
                    {
                        text2 = text2 + "Name: " + enumerator2.Current;
                        text2 = text2 + " Mem: " + enumerator.Current.Key;
                        text2 += Environment.NewLine;
                    }
                }
                text2 = text2 + "TotalMem: " + num;
                byte[] bytes = Encoding.ASCII.GetBytes(text2);
                fileStream.Write(bytes, 0, bytes.Length);
                fileStream.Close();
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(DumpTextures));
            }
        }

        private void DumpIconsInfo()
        {
            try
            {
                StringBuilder iconsInfo = new StringBuilder($"\nDumped Items Icon Info\n");
                foreach (var itemIconSprite in LocalItemsManager.m_ItemIconsSprites)
                {
                    iconsInfo.AppendLine($"Key\t{itemIconSprite.Key}");
                }
                ModAPI.Log.Write(iconsInfo.ToString());
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(DumpIconsInfo));
            }
        }
    }
}
