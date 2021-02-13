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
        private static readonly float ModScreenTotalHeight = 150f;
        private static readonly float ModScreenMinWidth = Screen.width;
        private static readonly float ModScreenMaxWidth = Screen.width;
        private static readonly float ModScreenMinHeight = 50f;
        private static readonly float ModScreenMaxHeight = 200f;
        private static float ModScreenStartPositionX { get; set; } = (Screen.width - ModScreenMaxWidth);
        private static float ModScreenStartPositionY { get; set; } = (Screen.height - ModScreenMaxHeight);
        private static bool IsMinimized { get; set; } = false;
        private bool ShowUI = false;
        public static Rect ModHUDScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);

        private static Player LocalPlayer;
        private static HUDManager LocalHUDManager;
        private static Watch LocalWatch;

        // Based on Watch
        public GameObject LocalHUDCanvas = new GameObject();
        private Dictionary<int, WatchData> LocalHUDCanvasDatas = new Dictionary<int, WatchData>();
        private static float MAX_BEATS_PER_SEC = 240f;
        private static float MIN_BEATS_PER_SEC = 60f;
        private int m_FakeDayOffset;

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
            InitData();
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
                    UpdateData();
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

            InitLocalHUDCanvas();
            InitMacronutrientsData();
            InitSanityData();
            InitTimeData();
            InitCompassData();
            SetAllActive();
        }

        private void InitLocalHUDCanvas()
        {
            var LocalHUDCanvasComponent = new Canvas();
            LocalHUDCanvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
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
                CompassBox();
                MacronutrientsBox();
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void MacronutrientsBox()
        {
            try
            {
                if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                {
                    using (var macroNutrientsScope = new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        WatchMacronutrientsData nutrientsData = (WatchMacronutrientsData)LocalHUDCanvasDatas[2];
                        GUILayout.Label(nutrientsData.m_Fat.mainTexture, GUI.skin.label);
                        GUILayout.Label(nutrientsData.m_Carbo.mainTexture, GUI.skin.label);
                        GUILayout.Label(nutrientsData.m_Hydration.mainTexture, GUI.skin.label);
                        GUILayout.Label(nutrientsData.m_Proteins.mainTexture, GUI.skin.label);
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
                        WatchCompassData compassData = (WatchCompassData)LocalHUDCanvasDatas[3];
                        GUILayout.Label($"South: { compassData.m_GPSCoordinates.text}", GUI.skin.label);
                        GUILayout.Label($"West: { compassData.m_GPSCoordinatesW.text}", GUI.skin.label);
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

        private void SetAllActive()
        {
            foreach (int key in LocalHUDCanvasDatas.Keys)
            {
                LocalHUDCanvasDatas[key].GetParent().SetActive(true);
            }
        }

        private void InitMacronutrientsData()
        {
            WatchMacronutrientsData watchMacronutrientsData = new WatchMacronutrientsData
            {
                m_Parent = LocalHUDCanvas,
                m_Fat = LocalWatch.transform.Find("Fat").GetComponent<Image>(),
                m_Carbo = LocalWatch.transform.Find("Carbo").GetComponent<Image>(),
                m_Hydration = LocalWatch.transform.Find("Hydration").GetComponent<Image>(),
                m_Proteins = LocalWatch.transform.Find("Proteins").GetComponent<Image>(),
                m_FatBG = LocalWatch.transform.Find("FatBG").GetComponent<Image>(),
                m_CarboBG = LocalWatch.transform.Find("CarboBG").GetComponent<Image>(),
                m_HydrationBG = LocalWatch.transform.Find("HydrationBG").GetComponent<Image>(),
                m_ProteinsBG = LocalWatch.transform.Find("ProteinsBG").GetComponent<Image>()
            };
            watchMacronutrientsData.m_Fat.color = IconColors.GetColor(IconColors.Icon.Fat);
            watchMacronutrientsData.m_Carbo.color = IconColors.GetColor(IconColors.Icon.Carbo);
            watchMacronutrientsData.m_Proteins.color = IconColors.GetColor(IconColors.Icon.Proteins);
            watchMacronutrientsData.m_Hydration.color = IconColors.GetColor(IconColors.Icon.Hydration);
            watchMacronutrientsData.m_Parent.SetActive(value: false);
            LocalHUDCanvasDatas.Add(2, watchMacronutrientsData);
        }

        private void InitSanityData()
        {
            WatchSanityData watchSanityData = new WatchSanityData
            {
                m_Parent = LocalHUDCanvas,
                m_Sanity = LocalWatch.transform.Find("SanityHRM").GetComponent<SWP_HeartRateMonitor>(),
                m_SanityText = LocalWatch.GetComponent<Text>()
            };
            watchSanityData.m_Parent.SetActive(value: false);
            LocalHUDCanvasDatas.Add(1, watchSanityData);
        }

        private void InitTimeData()
        {
            WatchTimeData watchTimeData = new WatchTimeData
            {
                m_Parent = LocalHUDCanvas,
                m_TimeHourDec = LocalWatch.transform.Find("HourDec").GetComponent<Text>(),
                m_TimeHourUnit = LocalWatch.transform.Find("HourUnit").GetComponent<Text>(),
                m_TimeMinuteDec = LocalWatch.transform.Find("MinuteDec").GetComponent<Text>(),
                m_TimeMinuteUnit = LocalWatch.transform.Find("MinuteUnit").GetComponent<Text>(),
                m_DayDec = LocalWatch.transform.Find("DayDec").GetComponent<Text>(),
                m_DayUnit = LocalWatch.transform.Find("DayUnit").GetComponent<Text>(),
                m_DayName = LocalWatch.transform.Find("DayName").GetComponent<Text>(),
                m_MonthName = LocalWatch.transform.Find("MonthName").GetComponent<Text>()
            };
            watchTimeData.m_Parent.SetActive(value: false);
            LocalHUDCanvasDatas.Add(0, watchTimeData);
        }

        private void InitCompassData()
        {
            WatchCompassData watchCompassData = new WatchCompassData
            {
                m_Parent = LocalHUDCanvas,
                m_Compass = LocalWatch.transform.Find("CompassIcon").gameObject,
                m_GPSCoordinates = LocalWatch.transform.Find("S_Coordinates").GetComponent<Text>(),
                m_GPSCoordinatesW = LocalWatch.transform.Find("W_Coordinates").GetComponent<Text>()
            };
            watchCompassData.m_Parent.SetActive(value: false);
            LocalHUDCanvasDatas.Add(3, watchCompassData);
        }

        private void UpdateData()
        {
            UpdateSanity();
            UpdateTime();
            UpdateMacronutrients();
            UpdateCompass();
        }

        private void UpdateSanity()
        {
            WatchSanityData obj2 = (WatchSanityData)LocalHUDCanvasDatas[1];
            obj2.m_Sanity.BeatsPerMinute = (int)CJTools.Math.GetProportionalClamp(MIN_BEATS_PER_SEC, MAX_BEATS_PER_SEC, PlayerSanityModule.Get().m_Sanity, 1f, 0f);
            obj2.m_SanityText.text = PlayerSanityModule.Get().m_Sanity.ToString();
        }

        public void ClearFakeDate()
        {
            m_FakeDayOffset = 0;
        }

        public void SetFakeDate(int day, int month)
        {
            DateTime dateTime = MainLevel.Instance.m_TODSky.Cycle.DateTime;
            dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day);
            int year = ((month > dateTime.Month && day > dateTime.Day) ? (dateTime.Year - 1) : dateTime.Year);
            DateTime d = new DateTime(year, month, day);
            m_FakeDayOffset = (d - dateTime).Days;
        }

        private void UpdateTime()
        {
            WatchTimeData watchTimeData = (WatchTimeData)LocalHUDCanvasDatas[0];
            DateTime dateTime = MainLevel.Instance.m_TODSky.Cycle.DateTime.AddDays(m_FakeDayOffset);
            int hour = dateTime.Hour;
            int num2 = hour % 10;
            int num3 = (hour - num2) / 10;
            int minute = dateTime.Minute;
            int num4 = minute % 10;
            int num5 = (minute - num4) / 10;
            watchTimeData.m_TimeHourDec.text = num3.ToString();
            watchTimeData.m_TimeHourUnit.text = num2.ToString();
            watchTimeData.m_TimeMinuteDec.text = num5.ToString();
            watchTimeData.m_TimeMinuteUnit.text = num4.ToString();
            Localization localization = GreenHellGame.Instance.GetLocalization();
            string key = "Watch_" + EnumUtils<DayOfWeek>.GetName(dateTime.DayOfWeek);
            watchTimeData.m_DayName.text = localization.Get(key);
            switch (dateTime.Month)
            {
                case 1:
                    key = "Watch_January";
                    break;
                case 2:
                    key = "Watch_February";
                    break;
                case 3:
                    key = "Watch_March";
                    break;
                case 4:
                    key = "Watch_April";
                    break;
                case 5:
                    key = "Watch_May";
                    break;
                case 6:
                    key = "Watch_June";
                    break;
                case 7:
                    key = "Watch_July";
                    break;
                case 8:
                    key = "Watch_August";
                    break;
                case 9:
                    key = "Watch_September";
                    break;
                case 10:
                    key = "Watch_October";
                    break;
                case 11:
                    key = "Watch_November";
                    break;
                case 12:
                    key = "Watch_December";
                    break;
            }
            watchTimeData.m_MonthName.text = localization.Get(key);
            int day = dateTime.Day;
            int num6 = day % 10;
            int num7 = (day - num6) / 10;
            watchTimeData.m_DayDec.text = num7.ToString();
            watchTimeData.m_DayUnit.text = num6.ToString();
        }

        private void UpdateMacronutrients()
        {
            WatchMacronutrientsData obj3 = (WatchMacronutrientsData)LocalHUDCanvasDatas[2];
            float fillAmount =LocalPlayer.GetNutritionFat() /LocalPlayer.GetMaxNutritionFat();
            obj3.m_Fat.fillAmount = fillAmount;
            float fillAmount2 =LocalPlayer.GetNutritionCarbo() /LocalPlayer.GetMaxNutritionCarbo();
            obj3.m_Carbo.fillAmount = fillAmount2;
            float fillAmount3 =LocalPlayer.GetNutritionProtein() /LocalPlayer.GetMaxNutritionProtein();
            obj3.m_Proteins.fillAmount = fillAmount3;
            float fillAmount4 =LocalPlayer.GetHydration() /LocalPlayer.GetMaxHydration();
            obj3.m_Hydration.fillAmount = fillAmount4;
        }

        private void UpdateCompass()
        {
            WatchCompassData obj = (WatchCompassData)LocalHUDCanvasDatas[3];
            Vector3 forward = LocalPlayer.gameObject.transform.forward;
            float num = Vector3.Angle(Vector3.forward, forward);
            if (forward.x < 0f)
            {
                num = 360f - num;
            }
            Quaternion rotation = Quaternion.Euler(new Vector3(0f, 0f, num));
            obj.m_Compass.transform.rotation = rotation;
            LocalPlayer.GetGPSCoordinates(out int gps_lat, out int gps_long);
            obj.m_GPSCoordinatesW.text = gps_lat.ToString();
            obj.m_GPSCoordinates.text = gps_long.ToString();
        }
    }
}
