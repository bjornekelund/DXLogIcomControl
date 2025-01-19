using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using ConfigFile;
using IOComm;

namespace DXLog.net
{
    public enum RadioType
    {
        None, IC7610, IC7760, IC7851, IC7850, IC7300, IC705, IC905, IC9700, K4, K3P3, FTDX101D, FTDX101MP, FTDX10, TS890, TS990
    }

    public partial class FrmRadioControl1 : KForm
    {
        public static string CusWinName
        {
            get { return "Radio 1 control"; }
        }

        public static int CusFormID
        {
            get { return 1022; }
        }

        public class RadioMapType
        {
            public string Brand;
            public string Model;
            public RadioType Radio;
        };

        public RadioMapType[] RadioMap = new RadioMapType [] {
            new RadioMapType { Brand = "Kenwood", Model = "890", Radio = RadioType.TS890 },
            new RadioMapType { Brand = "Kenwood", Model = "990", Radio = RadioType.TS990 },
            new RadioMapType { Brand = "Elecraft", Model = "K3", Radio = RadioType.K3P3 },
            new RadioMapType { Brand = "Elecraft", Model = "K4", Radio = RadioType.K4 },
            new RadioMapType { Brand = "ICOM", Model = "7610", Radio = RadioType.IC7610 },
            new RadioMapType { Brand = "ICOM", Model = "7760", Radio = RadioType.IC7760 },
            new RadioMapType { Brand = "ICOM", Model = "7850", Radio = RadioType.IC7850 },
            new RadioMapType { Brand = "ICOM", Model = "7851", Radio = RadioType.IC7851 },
            new RadioMapType { Brand = "ICOM", Model = "7300", Radio = RadioType.IC7300 },
            new RadioMapType { Brand = "ICOM", Model = "705", Radio = RadioType.IC705 },
            new RadioMapType { Brand = "ICOM", Model = "9700", Radio = RadioType.IC9700 },
            new RadioMapType { Brand = "ICOM", Model = "905", Radio = RadioType.IC905 },
            new RadioMapType { Brand = "Yaesu", Model = "101D", Radio = RadioType.FTDX101D },
            new RadioMapType { Brand = "Yaesu", Model = "101MP", Radio = RadioType.FTDX101MP },
            new RadioMapType { Brand = "Yaesu", Model = "DX10", Radio = RadioType.FTDX10 },
        }; 

        private readonly ContestData _cdata = null;

        private FrmMain _mainform = null;

        private int CurrentLowerEdge, CurrentUpperEdge, CurrentRefLevel, CurrentPwrLevel;
        private double CurrentFrequency = 0.0;
        private string CurrentMode = string.Empty;
        private int _radioNumber = 1;

        CATCommon Radio = null;

        readonly RadioSettings Settings = new RadioSettings();

        class WaterFallProperties
        {
            public int MinRef;
            public int MaxRef;
            public bool HasEdges;
            public int Edges;
            public bool HasScroll;
        }

        readonly Dictionary<RadioType, WaterFallProperties> Waterfall = new Dictionary<RadioType, WaterFallProperties> {
            { RadioType.None,      new WaterFallProperties(){ MinRef = -20, MaxRef = 20, HasEdges = false, Edges = 1, HasScroll = false }},
            { RadioType.IC705,     new WaterFallProperties(){ MinRef = -20, MaxRef = 20, HasEdges = true, Edges = 4, HasScroll = true }},
            { RadioType.IC7300,    new WaterFallProperties(){ MinRef = -20, MaxRef = 20, HasEdges = true, Edges = 4, HasScroll = true }},
            { RadioType.IC7610,    new WaterFallProperties(){ MinRef = -30, MaxRef = 10, HasEdges = true, Edges = 4, HasScroll = true }},
            { RadioType.IC7760,    new WaterFallProperties(){ MinRef = -30, MaxRef = 10, HasEdges = true, Edges = 4, HasScroll = true }},
            { RadioType.IC7851,    new WaterFallProperties(){ MinRef = -20, MaxRef = 20, HasEdges = true, Edges = 4, HasScroll = true }},
            { RadioType.IC7850,    new WaterFallProperties(){ MinRef = -20, MaxRef = 20, HasEdges = true, Edges = 4, HasScroll = true }},
            { RadioType.IC9700,    new WaterFallProperties(){ MinRef = -20, MaxRef = 20, HasEdges = true, Edges = 4, HasScroll = true }},
            { RadioType.IC905,     new WaterFallProperties(){ MinRef = -20, MaxRef = 20, HasEdges = true, Edges = 4, HasScroll = true }},
            { RadioType.K4,        new WaterFallProperties(){ MinRef = -140, MaxRef = 10, HasEdges = false, Edges = 1, HasScroll = true }},
            { RadioType.K3P3,      new WaterFallProperties(){ MinRef = -140, MaxRef = 10, HasEdges = false, Edges = 1, HasScroll = true }},
            { RadioType.FTDX10,    new WaterFallProperties(){ MinRef = -30, MaxRef = 30, HasEdges = false, Edges = 4, HasScroll = true }},
            { RadioType.FTDX101D,  new WaterFallProperties(){ MinRef = -30, MaxRef = 30, HasEdges = false, Edges = 4, HasScroll = true }},
            { RadioType.FTDX101MP, new WaterFallProperties(){ MinRef = -30, MaxRef = 30, HasEdges = false, Edges = 4, HasScroll = true }},
            { RadioType.TS890,     new WaterFallProperties(){ MinRef = -20, MaxRef = 10, HasEdges = true, Edges = 4, HasScroll = true }},
            { RadioType.TS990,     new WaterFallProperties(){ MinRef = -20, MaxRef = 10, HasEdges = true, Edges = 4, HasScroll = true }}
        };

        public int RadioNumber
        {
            get { return _radioNumber; }
            set
            {
                _radioNumber = value;
                Text = string.Format("Radio {0} control", _radioNumber);
            }
        }

        public FrmRadioControl1()
        {
            InitializeComponent();
        }

        public FrmRadioControl1(ContestData contestdata)
        {
            InitializeComponent();
            RadioNumber = 1;

            _cdata = contestdata;

            while (contextMenuStrip1.Items.Count > 0)
            {
                contextMenuStrip2.Items.Add(contextMenuStrip1.Items[0]);
            }

            contextMenuStrip2.Items.RemoveByKey("fixWindowSizeToolStripMenuItem");
            contextMenuStrip2.Items.RemoveByKey("fontSizeToolStripMenuItem");
            contextMenuStrip2.Items.RemoveByKey("colorsToolStripMenuItem");

            GetConfig(true);
        }

        void GetConfig(bool all)
        {
            try
            {
                Settings.Configuration = Config.Read("RCWaterfallConfiguration", 0);

                if (Settings.Configuration < 0 || Settings.Configuration > RadioSettings.Configs)
                {
                    Settings.Configuration = 0;
                    Config.Save("RCWaterfallConfiguration", 0);
                }

                Settings.PowerControl = Config.Read("RCPowerControl", true);
                Settings.RefLevelControl = Config.Read("RCRefControl", true);

                for (int i = 0; i < RadioSettings.Configs; i++)
                {
                    char letter = (char)('A' + i);

                    Settings.LowerEdgeCW[i] = Config.Read("RCWaterfallLowerEdgeCW" + letter, DefaultSettings.LowerEdgeCW).Split(';').Select(s => int.Parse(s)).ToArray();
                    Settings.UpperEdgeCW[i] = Config.Read("RCWaterfallUpperEdgeCW" + letter, DefaultSettings.UpperEdgeCW).Split(';').Select(s => int.Parse(s)).ToArray();

                    Settings.LowerEdgePhone[i] = Config.Read("RCWaterfallLowerEdgePhone" + letter, DefaultSettings.LowerEdgePhone).Split(';').Select(s => int.Parse(s)).ToArray();
                    Settings.UpperEdgePhone[i] = Config.Read("RCWaterfallUpperEdgePhone" + letter, DefaultSettings.UpperEdgePhone).Split(';').Select(s => int.Parse(s)).ToArray();

                    Settings.LowerEdgeDigital[i] = Config.Read("RCWaterfallLowerEdgeDigital" + letter, DefaultSettings.LowerEdgeDigital).Split(';').Select(s => int.Parse(s)).ToArray();
                    Settings.UpperEdgeDigital[i] = Config.Read("RCWaterfallUpperEdgeDigital" + letter, DefaultSettings.UpperEdgeDigital).Split(';').Select(s => int.Parse(s)).ToArray();

                    Settings.EdgeSet[i] = Config.Read("RCWaterfallEdgeSet" + letter, DefaultSettings.EdgeSet);
                    Settings.UseScrolling[i] = Config.Read("RCWaterfallScrolling" + letter, DefaultSettings.UseScrolling);

                    if (all)
                    {
                        Settings.RefLevelCW[i] = Config.Read("RCWaterfallRefCW" + letter, DefaultSettings.RefLevelCW).Split(';').Select(s => int.Parse(s)).ToArray();
                        Settings.RefLevelPhone[i] = Config.Read("RCWaterfallRefPhone" + letter, DefaultSettings.RefLevelPhone).Split(';').Select(s => int.Parse(s)).ToArray();
                        Settings.RefLevelDigital[i] = Config.Read("RCWaterfallRefDigital" + letter, DefaultSettings.RefLevelDigital).Split(';').Select(s => int.Parse(s)).ToArray();
                    }

                    if (Settings.LowerEdgeCW[i].Length != RadioSettings.Bands || Settings.UpperEdgeCW[i].Length != RadioSettings.Bands ||
                        Settings.LowerEdgePhone[i].Length != RadioSettings.Bands || Settings.UpperEdgePhone[i].Length != RadioSettings.Bands ||
                        Settings.LowerEdgeDigital[i].Length != RadioSettings.Bands || Settings.UpperEdgeDigital[i].Length != RadioSettings.Bands ||
                        Settings.EdgeSet.Length != RadioSettings.Edges || Settings.UseScrolling.Length != RadioSettings.Configs ||
                        Settings.RefLevelCW[i].Length != RadioSettings.Bands || Settings.RefLevelPhone[i].Length != RadioSettings.Bands || Settings.RefLevelDigital[i].Length != RadioSettings.Bands ||
                        Settings.EdgeSet[i] > RadioSettings.Edges)
                    {
                        throw new Exception();
                    }
                }
                if (all)
                {
                    Settings.PwrLevelCW = Config.Read("RCTransmitPowerCW", DefaultSettings.PwrLevelCW).Split(';').Select(s => int.Parse(s)).ToArray();
                    Settings.PwrLevelPhone = Config.Read("RCTransmitPowerPhone", DefaultSettings.PwrLevelPhone).Split(';').Select(s => int.Parse(s)).ToArray();
                    Settings.PwrLevelDigital = Config.Read("RCTransmitPowerDigital", DefaultSettings.PwrLevelDigital).Split(';').Select(s => int.Parse(s)).ToArray();

                    if (Settings.PwrLevelCW.Length != RadioSettings.Bands || Settings.PwrLevelPhone.Length != RadioSettings.Bands || Settings.PwrLevelDigital.Length != RadioSettings.Bands)
                    {
                        throw new Exception();
                    }
                }
            }
            catch
            {
                for (int i = 0; i < RadioSettings.Configs; i++)
                {
                    // Settings are somehow corrupted. Reset everything to default.
                    Settings.LowerEdgeCW[i] = DefaultSettings.LowerEdgeCW.Split(';').Select(s => int.Parse(s)).ToArray();
                    Settings.UpperEdgeCW[i] = DefaultSettings.UpperEdgeCW.Split(';').Select(s => int.Parse(s)).ToArray();

                    Settings.LowerEdgePhone[i] = DefaultSettings.LowerEdgePhone.Split(';').Select(s => int.Parse(s)).ToArray();
                    Settings.UpperEdgePhone[i] = DefaultSettings.UpperEdgePhone.Split(';').Select(s => int.Parse(s)).ToArray();

                    Settings.LowerEdgeDigital[i] = DefaultSettings.LowerEdgeDigital.Split(';').Select(s => int.Parse(s)).ToArray();
                    Settings.UpperEdgeDigital[i] = DefaultSettings.UpperEdgeDigital.Split(';').Select(s => int.Parse(s)).ToArray();

                    Settings.EdgeSet[i] = DefaultSettings.EdgeSet;
                    Settings.UseScrolling[i] = DefaultSettings.UseScrolling;

                    Settings.RefLevelCW[i] = DefaultSettings.RefLevelCW.Split(';').Select(s => int.Parse(s)).ToArray();
                    Settings.RefLevelPhone[i] = DefaultSettings.RefLevelPhone.Split(';').Select(s => int.Parse(s)).ToArray();
                    Settings.RefLevelDigital[i] = DefaultSettings.RefLevelDigital.Split(';').Select(s => int.Parse(s)).ToArray();
                }

                Settings.PwrLevelPhone = DefaultSettings.PwrLevelPhone.Split(';').Select(s => int.Parse(s)).ToArray();
                Settings.PwrLevelCW = DefaultSettings.PwrLevelCW.Split(';').Select(s => int.Parse(s)).ToArray();
                Settings.PwrLevelDigital = DefaultSettings.PwrLevelDigital.Split(';').Select(s => int.Parse(s)).ToArray();
            }
        }

        private void OnClosing(object sender, FormClosingEventArgs e)
        {
            for (int i = 0; i < RadioSettings.Configs; i++)
            {
                char letter = (char)('A' + i);
                Config.Save("RCWaterfallRefCW" + letter, string.Join(";", Settings.RefLevelCW[i].Select(j => j.ToString()).ToArray()));
                Config.Save("RCWaterfallRefPhone" + letter, string.Join(";", Settings.RefLevelPhone[i].Select(j => j.ToString()).ToArray()));
                Config.Save("RCWaterfallRefDigital" + letter, string.Join(";", Settings.RefLevelDigital[i].Select(j => j.ToString()).ToArray()));
            }

            Config.Save("RCTransmitPowerCW", string.Join(";", Settings.PwrLevelCW.Select(j => j.ToString()).ToArray()));
            Config.Save("RCTransmitPowerPhone", string.Join(";", Settings.PwrLevelPhone.Select(j => j.ToString()).ToArray()));
            Config.Save("RCTransmitPowerDigital", string.Join(";", Settings.PwrLevelDigital.Select(j => j.ToString()).ToArray()));

            Config.Save("RCPowerControl", Settings.PowerControl);
            Config.Save("RCRefControl", Settings.RefLevelControl);

            _cdata.ActiveRadioBandChanged -= UpdateRadio;
            _cdata.ActiveVFOChanged -= OnVFOChange;
        }

        public override void InitializeLayout()
        {
            if (_mainform == null)
            {
                _mainform = (FrmMain)(ParentForm ?? Owner);

                if (_mainform != null)
                {
                    _cdata.ActiveRadioBandChanged += new ContestData.ActiveRadioBandChange(OnBandChange);
                    _cdata.ActiveVFOChanged += new ContestData.ActiveVFOChange(OnVFOChange);
                }
            }

            UpdateRadio(_radioNumber);
        }

        private void OnVFOChange(int radionumber)
        {
            if (_cdata.OPTechnique != ContestData.Technique.SO2V)
            {
                UpdateRadio(radionumber);
            }
        }

        private void OnBandChange(int radionumber)
        {
            UpdateRadio(radionumber);
        }

        private CATCommon PhysicalRadio()
        {
            if (_cdata.OPTechnique == ContestData.Technique.SO2V)
            {
                return _mainform.COMMainProvider.RadioObject(1);
            }
            else
            {
                return _mainform.COMMainProvider.RadioObject(_radioNumber);
            }
        }

        delegate void UpdateRadioDelegate(int radionumber);

        private void UpdateRadio(int radionumber)
        {
            if (InvokeRequired)
            {
                UpdateRadioDelegate d = new UpdateRadioDelegate(UpdateRadio);
                Invoke(d, radionumber);
                return;
            }

            int _physicalRadio;
            int _selradio = radionumber < 1 ? _cdata.ActiveRadio : radionumber;

            if (_cdata.OPTechnique == ContestData.Technique.SO2V)
            {
                _physicalRadio = 1;
                CurrentFrequency = _selradio == 1 ? _cdata.Radio1_FreqA : _cdata.Radio1_FreqB;
                CurrentMode = _selradio == 1 ? _cdata.Radio1_ModeA : _cdata.Radio1_ModeB;
            }
            else
            {
                _physicalRadio = _selradio;
                if (_radioNumber != _selradio) return;
                CurrentFrequency = _selradio == 1 ? _cdata.Radio1_ActiveFreq : _cdata.Radio2_ActiveFreq;
                CurrentMode = _selradio == 1 ? _cdata.Radio1_ActiveMode : _cdata.Radio2_ActiveMode;
            }
            //label1.Text = string.Format("rn={0} sr={1} MHz={2} Md={3}", radionumber, _selradio, CurrentMHz, CurrentMode);

            Radio = PhysicalRadio();

            if (Radio == null)
            {
                Settings.RadioModel = RadioType.None;
                Settings.RadioModelName = "No radio";
            }
            else
            {
                Settings.RadioModel = RadioType.None;
                Settings.RadioModelName = Radio.GetType().GetField("RadioID").GetValue(null).ToString();

                foreach (RadioMapType r in RadioMap.OrderBy(o => o.Model))
                {
                    if (Settings.RadioModelName.Contains(r.Brand) && Settings.RadioModelName.Contains(r.Model))
                    {
                        Settings.RadioModel = r.Radio;
                        break;
                    }
                }
            }

            Settings.HasEdgeControl = Waterfall[Settings.RadioModel].HasEdges ;
            Settings.HasScroll = Waterfall[Settings.RadioModel].HasScroll;

            switch (CurrentMode)
            {
                case "CW":
                    CurrentLowerEdge = Settings.LowerEdgeCW[Settings.Configuration][BandIndex(CurrentFrequency)];
                    CurrentUpperEdge = Settings.UpperEdgeCW[Settings.Configuration][BandIndex(CurrentFrequency)];
                    CurrentRefLevel = Settings.RefLevelCW[Settings.Configuration][BandIndex(CurrentFrequency)];
                    CurrentPwrLevel = Settings.PwrLevelCW[BandIndex(CurrentFrequency)];
                    break;
                case "LSB":
                case "SSB":
                case "USB":
                case "AM":
                case "FM":
                    CurrentLowerEdge = Settings.LowerEdgePhone[Settings.Configuration][BandIndex(CurrentFrequency)];
                    CurrentUpperEdge = Settings.UpperEdgePhone[Settings.Configuration][BandIndex(CurrentFrequency)];
                    CurrentRefLevel = Settings.RefLevelPhone[Settings.Configuration][BandIndex(CurrentFrequency)];
                    CurrentPwrLevel = Settings.PwrLevelPhone[BandIndex(CurrentFrequency)];
                    break;
                default:
                    CurrentLowerEdge = Settings.LowerEdgeDigital[Settings.Configuration][BandIndex(CurrentFrequency)];
                    CurrentUpperEdge = Settings.UpperEdgeDigital[Settings.Configuration][BandIndex(CurrentFrequency)];
                    CurrentRefLevel = Settings.RefLevelDigital[Settings.Configuration][BandIndex(CurrentFrequency)];
                    CurrentPwrLevel = Settings.PwrLevelDigital[BandIndex(CurrentFrequency)];
                    break;
            }

            // Update reference level slider ends if radio has changed. Handle out of limits to prevent exception when switching radio.
            if (CurrentRefLevel < Waterfall[Settings.RadioModel].MinRef || CurrentRefLevel > Waterfall[Settings.RadioModel].MaxRef)
            {
                CurrentRefLevel = 0;
            }

            RefLevelSlider.Minimum = Waterfall[Settings.RadioModel].MinRef;
            RefLevelSlider.Maximum = Waterfall[Settings.RadioModel].MaxRef;

            // Update UI and waterfall edges and ref level in radio 
            UpdateRadioEdges(CurrentLowerEdge, CurrentUpperEdge);
            rangeLabel.Text = string.Format("WF: {0:N0} - {1:N0}", CurrentLowerEdge, CurrentUpperEdge);

            UpdateRadioReflevel(CurrentRefLevel);
            UpdateRadioPwrlevel(CurrentPwrLevel);
        }

        private void PropertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form prop = new FrmRadioControlProperties(Settings);

            prop.ShowDialog();
            GetConfig(false); // Retrieving via config, ugly
            UpdateRadio(_radioNumber);
        }

        private void OnRefSliderMouseClick(object sender, EventArgs e)
        {
            OnRefSlider();
        }

        // On mouse modification of slider
        private void OnRefSliderMouseClick(object sender, MouseEventArgs e)
        {
            //OnRefSlider();
        }

        private void OnRefSlider()
        {
            CurrentRefLevel = RefLevelSlider.Value;

            switch (CurrentMode)
            {
                case "CW":
                    Settings.RefLevelCW[Settings.Configuration][BandIndex(CurrentFrequency)] = CurrentRefLevel;
                    break;
                case "LSB":
                case "SSB":
                case "USB":
                case "AM":
                case "FM":
                    Settings.RefLevelPhone[Settings.Configuration][BandIndex(CurrentFrequency)] = CurrentRefLevel;
                    break;
                default:
                    Settings.RefLevelDigital[Settings.Configuration][BandIndex(CurrentFrequency)] = CurrentRefLevel;
                    break;
            }

            UpdateRadioReflevel(CurrentRefLevel);
        }

        // on mouse movement of power slider
        private void OnPwrSliderMouseClick(object sender, MouseEventArgs e)
        {
            //OnPwrSlider();
        }

        private void OnPwrSliderMouseClick(object sender, EventArgs e)
        {
            OnPwrSlider();
        }

        private void OnPwrSlider()
        {
            CurrentPwrLevel = PwrLevelSlider.Value;
            if (CurrentFrequency > 0.0)
            {
                switch (CurrentMode)
                {
                    case "CW":
                        Settings.PwrLevelCW[BandIndex(CurrentFrequency)] = CurrentPwrLevel;
                        break;
                    case "LSB":
                    case "SSB":
                    case "USB":
                    case "AM":
                    case "FM":
                        Settings.PwrLevelPhone[BandIndex(CurrentFrequency)] = CurrentPwrLevel;
                        break;
                    default:
                        Settings.PwrLevelDigital[BandIndex(CurrentFrequency)] = CurrentPwrLevel;
                        break;
                }
                UpdateRadioPwrlevel(CurrentPwrLevel);
            }
        }

        private void RefLevelLabel_Click(object sender, EventArgs e)
        {
            Settings.RefLevelControl = !Settings.RefLevelControl;
            Config.Save("RCRefControl", Settings.RefLevelControl);
            UpdateRadioReflevel(CurrentRefLevel);
        }

        private void PwrLevelLabel_Click(object sender, EventArgs e)
        {
            Settings.PowerControl = !Settings.PowerControl;
            Config.Save("RCPowerControl", Settings.PowerControl);
            UpdateRadioPwrlevel(CurrentPwrLevel);
        }

        // Update radio with new waterfall edges
        private void UpdateRadioEdges(int lower_edge, int upper_edge)
        {
            switch (Settings.RadioModel)
            {
                case RadioType.IC7610:
                case RadioType.IC7760:
                case RadioType.IC7300:
                case RadioType.IC705:
                case RadioType.IC7851:
                case RadioType.IC905:
                case RadioType.IC9700:
                    byte[] CIVSetFixedModeMain = { 0x27, 0x14, 0x00, 0x01 };
                    byte[] CIVSetEdgeSetMain = { 0x27, 0x16, 0x00, 0xff };

                    // Compose CI-V command to set waterfall edges
                    byte[] CIVSetEdges = new byte[] {
                    0x27, 0x1e,
                    (byte)((IcomBandIndex(CurrentFrequency) / 10) * 16 + (IcomBandIndex(CurrentFrequency) % 10)),
                    (byte)Settings.EdgeSet[Settings.Configuration],
                    0x00, // Lower 10Hz & 1Hz
                    (byte)((lower_edge % 10) * 16 + 0), // 1kHz & 100Hz
                    (byte)(((lower_edge / 100) % 10) * 16 + ((lower_edge / 10) % 10)), // 100kHz & 10kHz
                    (byte)(((lower_edge / 10000) % 10) * 16 + (lower_edge / 1000) % 10), // 10MHz & 1MHz
                    (byte)(((lower_edge / 1000000) % 10) * 16 + (lower_edge / 100000) % 10), // 1GHz & 100MHz
                    0x00, // // Upper 10Hz & 1Hz 
                    (byte)((upper_edge % 10) * 16 + 0), // 1kHz & 100Hz
                    (byte)(((upper_edge / 100) % 10) * 16 + (upper_edge / 10) % 10), // 100kHz & 10kHz
                    (byte)(((upper_edge / 10000) % 10) * 16 + (upper_edge / 1000) % 10), // 10MHz & 1MHz
                    (byte)(((upper_edge / 1000000) % 10) * 16 + (upper_edge / 100000) % 10) };// 1GHz & 100MHz

                    CIVSetFixedModeMain[3] = (byte)(Settings.UseScrolling[Settings.Configuration] ? 0x03 : 0x01);
                    CIVSetEdgeSetMain[3] = (byte)Settings.EdgeSet[Settings.Configuration];

                    Radio.SendCustomCommand(CIVSetFixedModeMain);
                    Radio.SendCustomCommand(CIVSetEdgeSetMain);
                    Radio.SendCustomCommand(CIVSetEdges);
                    break;
                case RadioType.K4:
                    // TODO: Implement support for K4
                    break;
                case RadioType.K3P3:
                    // TODO: Implement support for P3
                    break;
                case RadioType.FTDX101D:
                case RadioType.FTDX101MP:
                case RadioType.FTDX10:
                    // Set "fixed" or "cursor" type display
                    string scrollcmd = "SS06" + (Settings.UseScrolling[Settings.Configuration] ? "70000;" : "A0000;");
                    Radio.SendCustomCommand(scrollcmd);
                    // TODO: Implement support for Yaesu edges
                    break;
                case RadioType.TS890:
                case RadioType.TS990:
                    // Select main receiver
                    Radio.SendCustomCommand("BS220;");
                    // Set fixed type display. Does not have scrolling.
                    Radio.SendCustomCommand("BS31;");
                    // Set edges (documentation says this is a request only command)
                    string edgcmd = "BS5" + lower_edge.ToString("00000") + "000" + upper_edge.ToString("00000") + "000;";
                    Radio.SendCustomCommand(edgcmd);
                    break;
                default:
                    break;
            }
        }

        // Update radio with new REF level
        private void UpdateRadioReflevel(int ref_level)
        {
            if (RefLevelLabel != null)
            {
                if (Settings.RefLevelControl)
                {
                    RefLevelSlider.Value = ref_level;
                    RefLevelSlider.Enabled = true;

                    RefLevelLabel.Text = string.Format("REF: {0,3:+#;-#;0}dB", ref_level);
                    int absRefLevel = (ref_level >= 0) ? ref_level : -ref_level;

                    switch (Settings.RadioModel)
                    {
                        case RadioType.IC7610:
                        case RadioType.IC7760:
                        case RadioType.IC7300:
                        case RadioType.IC705:
                        case RadioType.IC7851:
                        case RadioType.IC905:
                        case RadioType.IC9700:
                            byte[] CIVSetRefLevelMain = { 0x27, 0x19, 0x00, 0x00, 0x00, 0x00 };

                            CIVSetRefLevelMain[3] = (byte)((absRefLevel / 10) * 16 + absRefLevel % 10);
                            CIVSetRefLevelMain[5] = (ref_level >= 0) ? (byte)0 : (byte)1;

                            if (Settings.RefLevelControl)
                            {
                                Radio.SendCustomCommand(CIVSetRefLevelMain);
                            }
                            break;
                        case RadioType.K4:
                        case RadioType.K3P3:
                            string cmd = "#REF$" + ref_level.ToString() + ";";
                            if (Settings.RefLevelControl)
                            {
                                Radio.SendCustomCommand(cmd);
                            }
                            break;
                        case RadioType.FTDX101MP:
                        case RadioType.FTDX101D:
                        case RadioType.FTDX10:
                            cmd = "SS04" + (ref_level < 0 ? "-" : "+") + absRefLevel.ToString("00") + ".0";
                            if (Settings.RefLevelControl)
                            {
                                Radio.SendCustomCommand(cmd);
                            }
                            break;
                        case RadioType.TS890:
                        case RadioType.TS990:
                            int kval = 2 * ref_level + 40;
                            cmd = "BSC" + kval.ToString("000") + ";";
                            if (Settings.RefLevelControl)
                            {
                                Radio.SendCustomCommand(cmd);
                            }
                            break;
                        default: // No radio
                            break;
                    }
                }
                else
                {
                    RefLevelSlider.Value = 0;
                    RefLevelSlider.Enabled = false;
                    RefLevelLabel.Text = "REF: -";
                }
            }
        }

        // Update radio with new PWR level
        private void UpdateRadioPwrlevel(int pwr_level)
        {
            if (PwrLevelSlider != null)
            {
                if (Settings.PowerControl)
                {
                    PwrLevelSlider.Value = pwr_level;
                    PwrLevelLabel.Text = string.Format("PWR:{0,3}%", pwr_level);
                    PwrLevelSlider.Enabled = true;

                    switch (Settings.RadioModel)
                    {
                        case RadioType.IC7610:
                        case RadioType.IC7300:
                        case RadioType.IC705:
                        case RadioType.IC7851:
                        case RadioType.IC905:
                        case RadioType.IC9700:
                            byte[] CIVSetPwrLevel = { 0x14, 0x0a, 0x00, 0x00 };

                            int icomPower = (int)(255.0f * pwr_level / 100.0f + 0.99f); // Weird ICOM mapping of percent to binary

                            CIVSetPwrLevel[2] = (byte)((icomPower / 100) % 10);
                            CIVSetPwrLevel[3] = (byte)((((icomPower / 10) % 10) << 4) + (icomPower % 10));

                            if (Settings.PowerControl)
                            {
                                Radio.SendCustomCommand(CIVSetPwrLevel);
                            }
                            break;
                        case RadioType.K4:
                            int watt = 110 * pwr_level;
                            string cmd = "PC" + watt.ToString("000") + "H;";
                            if (Settings.PowerControl)
                            {
                                Radio.SendCustomCommand(cmd);
                            }
                            break;
                        case RadioType.K3P3:
                            watt = 110 * pwr_level;
                            cmd = "PC" + watt.ToString("000") + ";";
                            if (Settings.PowerControl)
                            {
                                Radio.SendCustomCommand(cmd);
                            }
                            break;
                        case RadioType.FTDX101D:
                        case RadioType.FTDX10:
                            watt = pwr_level < 5 ? 5 : pwr_level;
                            cmd = "PC" + watt.ToString("000") + ";";
                            if (Settings.PowerControl)
                            {
                                Radio.SendCustomCommand(cmd);
                            }
                            break;
                        case RadioType.TS890:
                            watt = pwr_level < 5 ? 5 : pwr_level;
                            cmd = "PC" + watt.ToString("000") + ";";
                            if (Settings.PowerControl)
                            {
                                Radio.SendCustomCommand(cmd);
                            }
                            break;
                        case RadioType.TS990:
                        case RadioType.FTDX101MP:
                            watt = pwr_level < 3 ? 5 : pwr_level * 2;
                            cmd = "PC" + watt.ToString("000") + ";";
                            if (Settings.PowerControl)
                            {
                                Radio.SendCustomCommand(cmd);
                            }
                            break;
                        default: // Unknown or no radio
                            break;
                    }
                }
                else
                {
                    PwrLevelSlider.Value = 0;
                    PwrLevelSlider.Enabled = false;
                    PwrLevelLabel.Text = "PWR: -";
                }
            }
        }

        private int BandIndex(double freq)
        {
            // Maps frequency to internal band index. Upper limit for each index. 
            // Bands are 160=0, 80=1, etc. 11=6m, 12=2m, up to 17=3cm
            int[] _bandlimit = new int[] { 2000, 5000, 6000, 8000, 11000, 15000, 20000, 22000, 26000, 31000, 60000, 75000, 200000, 470000, 1500000, 3000000, 7000000, 11000000 };
            int _index = 0;
            for (int i = 0; i < _bandlimit.Length; i++)
            {
                if (freq < _bandlimit[i])
                {
                    _index = i;
                    break;
                }
            }
            return _index;
        }

        private int IcomBandIndex(double freq)
        {
            int[] _icomBandlimit;

            switch (Settings.RadioModel)
            {
                case RadioType.IC905:
                case RadioType.IC9700:
                    _icomBandlimit = new int[] { 0, 148000, 450000, 1300000, 2450000, 5925000, 10600000 };
                    break;
                default:
                    _icomBandlimit = new int[] { 0, 1600, 2000, 6000, 8000, 11000, 15000, 20000, 22000, 26000, 30000, 45000, 60000, 74800, 108000, 137000, 400000, 480000 };
                    break;
            }

            int _index = 0;
            for (int i = 0; i < _icomBandlimit.Length; i++)
            {
                if (freq < _icomBandlimit[i])
                {
                    _index = i;
                    break;
                }
            }
            return _index;
        }
    }


    public class DefaultSettings
    {
        public const string LowerEdgeCW = "1810;3500;5352;7000;10100;14000;18068;21000;24890;28000;50000;70000;144000;432000;1296000;2320000;5650000;10000000";
        public const string UpperEdgeCW = "1840;3570;5366;7040;10130;14070;18109;21070;24920;28070;50150;71000;144100;432100;1296100;2320100;5650100;10000100";
        public const string RefLevelCW = "0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0";
        public const string PwrLevelCW = "20;20;20;20;20;20;20;20;20;20;20;20;20;20;20;20;20;20";

        public const string LowerEdgePhone = "1860;3600;5352;7060;10100;14100;18111;21150;24931;28300;50100;70000;144200;432100;1296200;2320100;5650200;10000200";
        public const string UpperEdgePhone = "2000;3800;5366;7200;10150;14350;18168;21450;24990;28600;50500;71000;144400;432300;1296400;2300300;5650400;10000400";
        public const string RefLevelPhone = "0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0";
        public const string PwrLevelPhone = "20;20;20;20;20;20;20;20;20;20;20;20;20;20;20;20;20;20";

        public const string LowerEdgeDigital = "1840;3570;5352;7040;10130;14070;18089;21070;24910;28070;50300;70000;144000;432000;1296000;2320000;5650150;10368150";
        public const string UpperEdgeDigital = "1860;3600;5366;7080;10150;14100;18109;21150;24932;28110;50350;71000;144400;432100;1296100;2320100;5650200;10368250";
        public const string RefLevelDigital = "0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0";
        public const string PwrLevelDigital = "20;20;20;20;20;20;20;20;20;20;20;20;20;20;20;20;20;20";

        public const int EdgeSet = 4;
        public const bool UseScrolling = false;
        public const bool NoRefLevel = false;
        public const bool NoPowerLevel = false;
    }

    public class RadioSettings
    {
        public int Configuration = 0;
        public string RadioModelName;
        public RadioType RadioModel;
        public const int Configs = 4;
        public const int Bands = 18;
        public const int Edges = 4;

        public int[][] LowerEdgeCW = new int[Configs][];
        public int[][] UpperEdgeCW = new int[Configs][];
        public int[][] RefLevelCW = new int[Configs][];
        public int[][] LowerEdgePhone = new int[Configs][];
        public int[][] UpperEdgePhone = new int[Configs][];
        public int[][] RefLevelPhone = new int[Configs][];
        public int[][] LowerEdgeDigital = new int[Configs][];
        public int[][] UpperEdgeDigital = new int[Configs][];
        public int[][] RefLevelDigital = new int[Configs][];
        public int[] PwrLevelCW;
        public int[] PwrLevelPhone;
        public int[] PwrLevelDigital;
        public int[] EdgeSet = new int[Configs];
        public bool[] UseScrolling = new bool[Configs];
        public bool[] SupportedBand;
        public bool RefLevelControl;
        public bool PowerControl;
        public bool HasEdgeControl;
        public bool HasScroll;
    }
}
