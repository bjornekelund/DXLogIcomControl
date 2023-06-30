﻿using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using ConfigFile;
using IOComm;

namespace DXLog.net
{
    public partial class DXLogIcomControl : KForm
    {
        public static string CusWinName
        {
            get { return "Radio controller"; }
        }

        public static int CusFormID
        {
            get { return 1022; }
        }
        
        private ContestData _cdata = null;

        private FrmMain _mainform = null;

        // Pre-baked CI-V commands
        private byte[] CIVSetFixedModeMain = { 0x27, 0x14, 0x00, 0x01 };
        //private byte[] CIVSetFixedModeSub = { 0x27, 0x14, 0x01, 0x01 };

        private byte[] CIVSetEdgeSetMain = { 0x27, 0x16, 0x00, 0xff };
        //private byte[] CIVSetEdgeSetSub = { 0x27, 0x16, 0x01, 0xff };

        private byte[] CIVSetRefLevelMain = { 0x27, 0x19, 0x00, 0x00, 0x00, 0x00 };
        //private byte[] CIVSetRefLevelSub = { 0x27, 0x19, 0x01, 0x00, 0x00, 0x00 };

        private byte[] CIVSetPwrLevel = { 0x14, 0x0a, 0x00, 0x00};

        private const int MaxMHz = 470;
        private const int TableSize = 74;

        // Maps MHz to internal band index.
        // Bands are 160=0, 80=1, etc. up to 13=70cm
        private int[] BandIndex = new int[MaxMHz];
        private readonly int[] REFbandIndex = new int[TableSize]
            { 0, 0, 0, 1, 1, 2, 3, 3, 3, 4,
            4, 4, 4, 5, 5, 5, 5, 6, 6, 6,
            7, 7, 7, 7, 8, 8, 8, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 10, 10, 10, 10, 10, 10, 10, 10, 10,
            10, 10, 10, 10, 10, 10, 10, 10, 10, 10,
            10, 11, 11, 11, 11, 11, 11, 11, 11, 11,
            11, 11, 11, 11 };

        // Maps actual MHz to radio's scope edge set.
        private int[] RadioEdgeSet = new int[MaxMHz];
        private readonly int[] REFRadioEdgeSet = new int[TableSize]
            { 1, 2, 3, 3, 3, 3, 4, 4, 5, 5,
            5, 6, 6, 6, 6, 7, 7, 7, 7, 7,
            8, 8, 9, 9, 9, 9, 10, 10, 10, 10,
            11, 11, 11, 11, 11, 11, 11, 11, 11, 11,
            11, 11, 11, 11, 11, 12, 12, 12, 12, 12,
            12, 12, 12, 12, 12, 12, 12, 12, 12, 12,
            13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
            13, 13, 13, 13 };

        private int CurrentLowerEdge, CurrentUpperEdge, CurrentRefLevel, CurrentPwrLevel;
        private int CurrentMHz = 0;
        private string CurrentMode = string.Empty;
        private int _radioNumber = 1;

        CATCommon Radio = null;

        RadioSettings Settings = new RadioSettings();
        DefaultRadioSettings Default = new DefaultRadioSettings();

        public int RadioNumber
        {
            get { return _radioNumber; }
            set
            {
                _radioNumber = value;
                Text = string.Format("Radio {0} controller", _radioNumber);
            }
        }

        public DXLogIcomControl()
        {
            InitializeComponent();
        }

        public DXLogIcomControl(ContestData contestdata)
        {
            InitializeComponent();
            RadioNumber = 1;
            _cdata = contestdata;

            while (contextMenuStrip1.Items.Count > 0)
                contextMenuStrip2.Items.Add(contextMenuStrip1.Items[0]);
            contextMenuStrip2.Items.RemoveByKey("fixWindowSizeToolStripMenuItem");
            contextMenuStrip2.Items.RemoveByKey("fontSizeToolStripMenuItem");
            contextMenuStrip2.Items.RemoveByKey("colorsToolStripMenuItem");

            Settings.LowerEdgeCW = new int[Settings.Configs][];
            Settings.UpperEdgeCW = new int[Settings.Configs][];
            Settings.RefLevelCW = new int[Settings.Configs][];
            Settings.LowerEdgePhone = new int[Settings.Configs][];
            Settings.UpperEdgePhone = new int[Settings.Configs][];
            Settings.RefLevelPhone = new int[Settings.Configs][];
            Settings.LowerEdgeDigital = new int[Settings.Configs][];
            Settings.UpperEdgeDigital = new int[Settings.Configs][];
            Settings.RefLevelDigital = new int[Settings.Configs][];
            Settings.PwrLevelCW = new int[Settings.Bands];
            Settings.PwrLevelPhone = new int[Settings.Bands];
            Settings.PwrLevelDigital = new int[Settings.Bands];
            Settings.EdgeSet = new int[Settings.Configs];
            Settings.Scrolling = new bool[Settings.Configs];
            for (int c = 0; c < Settings.Configs; c++)
            {
                Settings.LowerEdgeCW[c] = new int[Settings.Bands];
                Settings.UpperEdgeCW[c] = new int[Settings.Bands];
                Settings.RefLevelCW[c] = new int[Settings.Bands];
                Settings.LowerEdgePhone[c] = new int[Settings.Bands];
                Settings.UpperEdgePhone[c] = new int[Settings.Bands];
                Settings.RefLevelPhone[c] = new int[Settings.Bands];
                Settings.LowerEdgeDigital[c] = new int[Settings.Bands];
                Settings.UpperEdgeDigital[c] = new int[Settings.Bands];
                Settings.RefLevelDigital[c] = new int[Settings.Bands];
            }

            // Set the decoding arrays to default
            for (int MHz = 0; MHz < MaxMHz; MHz++)
            {
                BandIndex[MHz] = 1;
                RadioEdgeSet[MHz] = 1;
            }

            // Initialize using tables
            for (int MHz = 0; MHz < TableSize; MHz++)
            {
                BandIndex[MHz] = REFbandIndex[MHz];
                RadioEdgeSet[MHz] = REFRadioEdgeSet[MHz];
            }

            // Add 2m
            for (int MHz = 137; MHz < 200; MHz++)
            {
                BandIndex[MHz] = 12;
                RadioEdgeSet[MHz] = 16;
            }

            // Add 70cm
            for (int MHz = 400; MHz < 470; MHz++)
            {
                BandIndex[MHz] = 13;
                RadioEdgeSet[MHz] = 17;
            }

            GetConfig(true);
        }

        void GetConfig(bool all)
        {
            try
            {
                Settings.Configuration = Config.Read("WaterfallConfiguration", 1) - 1;

                if (Settings.Configuration < 0 || Settings.Configuration > Settings.Configs) 
                { 
                    Settings.Configuration = 0; 
                }

                for (int i = 0; i < Settings.Configs; i++)
                {
                    char letter = (char)('A' + i);

                    Settings.LowerEdgeCW[i] = Config.Read("WaterfallLowerEdgeCW" + letter, Default.LowerEdgeCW).Split(';').Select(s => int.Parse(s)).ToArray();
                    Settings.UpperEdgeCW[i] = Config.Read("WaterfallUpperEdgeCW" + letter, Default.UpperEdgeCW).Split(';').Select(s => int.Parse(s)).ToArray();

                    Settings.LowerEdgePhone[i] = Config.Read("WaterfallLowerEdgePhone" + letter, Default.LowerEdgePhone).Split(';').Select(s => int.Parse(s)).ToArray();
                    Settings.UpperEdgePhone[i] = Config.Read("WaterfallUpperEdgePhone" + letter, Default.UpperEdgePhone).Split(';').Select(s => int.Parse(s)).ToArray();

                    Settings.LowerEdgeDigital[i] = Config.Read("WaterfallLowerEdgeDigital" + letter, Default.LowerEdgeDigital).Split(';').Select(s => int.Parse(s)).ToArray();
                    Settings.UpperEdgeDigital[i] = Config.Read("WaterfallUpperEdgeDigital" + letter, Default.UpperEdgeDigital).Split(';').Select(s => int.Parse(s)).ToArray();

                    Settings.EdgeSet[i] = Config.Read("WaterfallEdgeSet" + letter, Default.EdgeSet);
                    Settings.Scrolling[i] = Config.Read("WaterfallScrolling" + letter, Default.UseScrolling);

                    if (all)
                    {
                        Settings.RefLevelCW[i] = Config.Read("WaterfallRefCW" + letter, Default.RefLevelCW).Split(';').Select(s => int.Parse(s)).ToArray();
                        Settings.RefLevelPhone[i] = Config.Read("WaterfallRefPhone" + letter, Default.RefLevelPhone).Split(';').Select(s => int.Parse(s)).ToArray();
                        Settings.RefLevelDigital[i] = Config.Read("WaterfallRefDigital" + letter, Default.RefLevelDigital).Split(';').Select(s => int.Parse(s)).ToArray();
                    }
                }
                if (all)
                {
                    Settings.PwrLevelCW = Config.Read("TransmitPowerCW", Default.PwrLevelCW).Split(';').Select(s => int.Parse(s)).ToArray();
                    Settings.PwrLevelPhone = Config.Read("TransmitPowerPhone", Default.PwrLevelPhone).Split(';').Select(s => int.Parse(s)).ToArray();
                    Settings.PwrLevelDigital = Config.Read("TransmitPowerDigital", Default.PwrLevelDigital).Split(';').Select(s => int.Parse(s)).ToArray();

                }
            }
            catch
            {
                for (int i = 0; i < Settings.Configs; i++)
                {
                    // Settings are somehow corrupted. Reset everything to default.
                    Settings.LowerEdgeCW[i] = Default.LowerEdgeCW.Split(';').Select(s => int.Parse(s)).ToArray();
                    Settings.UpperEdgeCW[i] = Default.UpperEdgeCW.Split(';').Select(s => int.Parse(s)).ToArray();

                    Settings.LowerEdgePhone[i] = Default.LowerEdgePhone.Split(';').Select(s => int.Parse(s)).ToArray();
                    Settings.UpperEdgePhone[i] = Default.UpperEdgePhone.Split(';').Select(s => int.Parse(s)).ToArray();

                    Settings.LowerEdgeDigital[i] = Default.LowerEdgeDigital.Split(';').Select(s => int.Parse(s)).ToArray();
                    Settings.UpperEdgeDigital[i] = Default.UpperEdgeDigital.Split(';').Select(s => int.Parse(s)).ToArray();

                    Settings.EdgeSet[i] = Default.EdgeSet;
                    Settings.Scrolling[i] = Default.UseScrolling;

                    Settings.RefLevelCW[i] = Default.RefLevelCW.Split(';').Select(s => int.Parse(s)).ToArray();
                    Settings.RefLevelPhone[i] = Default.RefLevelPhone.Split(';').Select(s => int.Parse(s)).ToArray();
                    Settings.RefLevelDigital[i] = Default.RefLevelDigital.Split(';').Select(s => int.Parse(s)).ToArray();
                }

                Settings.PwrLevelPhone = Default.PwrLevelPhone.Split(';').Select(s => int.Parse(s)).ToArray();
                Settings.PwrLevelCW = Default.PwrLevelCW.Split(';').Select(s => int.Parse(s)).ToArray();
                Settings.PwrLevelDigital = Default.PwrLevelDigital.Split(';').Select(s => int.Parse(s)).ToArray();
            }
        }

        private void OnClosing(object sender, FormClosingEventArgs e)
        {
            for (int i = 0; i < Settings.Configs; i++)
            {
                char letter = (char)('A' + i);
                Config.Save("WaterfallRefCW" + letter, string.Join(";", Settings.RefLevelCW[i].Select(j => j.ToString()).ToArray()));
                Config.Save("WaterfallRefPhone" + letter, string.Join(";", Settings.RefLevelPhone[i].Select(j => j.ToString()).ToArray()));
                Config.Save("WaterfallRefDigital" + letter, string.Join(";", Settings.RefLevelDigital[i].Select(j => j.ToString()).ToArray()));
            }

            Config.Save("TransmitPowerCW", string.Join(";", Settings.PwrLevelCW.Select(j => j.ToString()).ToArray()));
            Config.Save("TransmitPowerPhone", string.Join(";", Settings.PwrLevelPhone.Select(j => j.ToString()).ToArray()));
            Config.Save("TransmitPowerDigital", string.Join(";", Settings.PwrLevelDigital.Select(j => j.ToString()).ToArray()));

            _cdata.ActiveRadioBandChanged -= UpdateRadio;
            _cdata.ActiveVFOChanged -= UpdateRadioOnVFOChange;
        }

        public override void InitializeLayout()
        {
            if (_mainform == null)
            {
                _mainform = (FrmMain)(ParentForm ?? Owner);

                if (_mainform != null)
                {
                    _cdata.ActiveRadioBandChanged += new ContestData.ActiveRadioBandChange(UpdateRadio);
                    _cdata.ActiveVFOChanged += new ContestData.ActiveVFOChange(UpdateRadioOnVFOChange);
                }
            }

            UpdateRadio(_radioNumber);
        }

        private void UpdateRadioOnVFOChange(int radionumber)
        {
            if (_cdata.OPTechnique != ContestData.Technique.SO2V)
            {
                UpdateRadio(radionumber);
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

            int _physradio;
            int _selradio = radionumber < 1 ? _cdata.ActiveRadio : radionumber;

            if (_cdata.OPTechnique == ContestData.Technique.SO2V)
            {
                _physradio = 1;
                CurrentMHz = (int)(_selradio == 1 ? _cdata.Radio1_FreqA : _cdata.Radio1_FreqB) / 1000;
                CurrentMode = _selradio == 1 ? _cdata.Radio1_ModeA : _cdata.Radio1_ModeB;
            }
            else
            {
                _physradio = _selradio;
                if (_radioNumber != _selradio) return;
                CurrentMHz = (int)(_selradio == 1 ? _cdata.Radio1_ActiveFreq : _cdata.Radio2_ActiveFreq) / 1000;
                CurrentMode = _selradio == 1 ? _cdata.Radio1_ActiveMode : _cdata.Radio2_ActiveMode;
            }
            //label1.Text = string.Format("rn={0} sr={1} MHz={2} Md={3}", radionumber, _selradio, CurrentMHz, CurrentMode);

            switch (CurrentMode)
            {
                case "CW":
                    CurrentLowerEdge = Settings.LowerEdgeCW[Settings.Configuration][BandIndex[CurrentMHz]];
                    CurrentUpperEdge = Settings.UpperEdgeCW[Settings.Configuration][BandIndex[CurrentMHz]];
                    CurrentRefLevel = Settings.RefLevelCW[Settings.Configuration][BandIndex[CurrentMHz]];
                    CurrentPwrLevel = Settings.PwrLevelCW[BandIndex[CurrentMHz]];
                    break;
                case "LSB":
                case "SSB":
                case "USB":
                case "AM":
                case "FM":
                    CurrentLowerEdge = Settings.LowerEdgePhone[Settings.Configuration][BandIndex[CurrentMHz]];
                    CurrentUpperEdge = Settings.UpperEdgePhone[Settings.Configuration][BandIndex[CurrentMHz]];
                    CurrentRefLevel = Settings.RefLevelPhone[Settings.Configuration][BandIndex[CurrentMHz]];
                    CurrentPwrLevel = Settings.PwrLevelPhone[BandIndex[CurrentMHz]];
                    break;
                default:
                    CurrentLowerEdge = Settings.LowerEdgeDigital[Settings.Configuration][BandIndex[CurrentMHz]];
                    CurrentUpperEdge = Settings.UpperEdgeDigital[Settings.Configuration][BandIndex[CurrentMHz]];
                    CurrentRefLevel = Settings.RefLevelDigital[Settings.Configuration][BandIndex[CurrentMHz]];
                    CurrentPwrLevel = Settings.PwrLevelDigital[BandIndex[CurrentMHz]];
                    break;
            }

            Radio = _mainform.COMMainProvider.RadioObject(_physradio);

            // Update UI and waterfall edges and ref level in radio 
            UpdateRadioEdges(CurrentLowerEdge, CurrentUpperEdge, RadioEdgeSet[CurrentMHz]);
            rangeLabel.Text = string.Format("WF: {0:N0} - {1:N0}", CurrentLowerEdge, CurrentUpperEdge);

            UpdateRadioReflevel(CurrentRefLevel);
            UpdateRadioPwrlevel(CurrentPwrLevel);
        }

        private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form prop = new IcomProperties(Settings);
  
            if (prop.ShowDialog() == DialogResult.OK)
            {
                GetConfig(false); // Retrieving via config, ugly
            }

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

            UpdateRadioReflevel(CurrentRefLevel);

            switch (CurrentMode)
            {
                case "CW":
                    Settings.RefLevelCW[Settings.Configuration][BandIndex[CurrentMHz]] = CurrentRefLevel;
                    break;
                case "LSB":
                case "SSB":
                case "USB":
                case "AM":
                case "FM":
                    Settings.RefLevelPhone[Settings.Configuration][BandIndex[CurrentMHz]] = CurrentRefLevel;
                    break;
                default:
                    Settings.RefLevelDigital[Settings.Configuration][BandIndex[CurrentMHz]] = CurrentRefLevel;
                    break;
            }
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
            UpdateRadioPwrlevel(CurrentPwrLevel);

            if (CurrentMHz != 0)
            {
                switch (CurrentMode)
                {
                    case "CW":
                        Settings.PwrLevelCW[BandIndex[CurrentMHz]] = CurrentPwrLevel;
                        break;
                    case "LSB":
                    case "SSB":
                    case "USB":
                    case "AM":
                    case "FM":
                        Settings.PwrLevelPhone[BandIndex[CurrentMHz]] = CurrentPwrLevel;
                        break;
                    default:
                        Settings.PwrLevelDigital[BandIndex[CurrentMHz]] = CurrentPwrLevel;
                        break;
                }
            }
        }

        // Update radio with new waterfall edges
        private void UpdateRadioEdges(int lower_edge, int upper_edge, int ICOMedgeSegment)
        {
            // Compose CI-V command to set waterfall edges
            byte[] CIVSetEdges = new byte[]
            {
                0x27, 0x1e,
                (byte)((ICOMedgeSegment / 10) * 16 + (ICOMedgeSegment % 10)),
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
                (byte)(((upper_edge / 1000000) % 10) * 16 + (upper_edge / 100000) % 10) // 1GHz & 100MHz
            };

            CIVSetFixedModeMain[3] = (byte)(Settings.Scrolling[Settings.Configuration] ? 0x03 : 0x01);
            CIVSetEdgeSetMain[3] = (byte)Settings.EdgeSet[Settings.Configuration];

            //CIVSetFixedModeSub[3] = (byte)(Set.Scrolling ? 0x03 : 0x01);
            //CIVSetEdgeSetSub[3] = (byte)Set.EdgeSet;

            //debuglabel1.Text = BitConverter.ToString(CIVSetFixedModeMain).Replace("-", " ");
            //debuglabel2.Text = BitConverter.ToString(CIVSetEdgeSetMain).Replace("-", " ");
            //debuglabel3.Text = BitConverter.ToString(CIVSetEdges).Replace("-", " ");

            if (Radio != null && Radio.IsICOM())
            {
                Radio.SendCustomCommand(CIVSetFixedModeMain);
                //Radio.SendCustomCommand(CIVSetFixedModeSub);
                Radio.SendCustomCommand(CIVSetEdgeSetMain);
                //Radio.SendCustomCommand(CIVSetEdgeSetSub);
                Radio.SendCustomCommand(CIVSetEdges);
            }
        }

        // Update radio with new REF level
        private void UpdateRadioReflevel(int ref_level)
        {
            if (RefLevelLabel != null)
            {
                RefLevelSlider.Value = ref_level;
                RefLevelLabel.Text = string.Format("REF: {0,3:+#;-#;0}dB", ref_level);
            }

            int absRefLevel = (ref_level >= 0) ? ref_level : -ref_level;

            CIVSetRefLevelMain[3] = (byte)((absRefLevel / 10) * 16 + absRefLevel % 10);
            CIVSetRefLevelMain[5] = (ref_level >= 0) ? (byte)0 : (byte)1;

            //CIVSetRefLevelSub[3] = (byte)((absRefLevel / 10) * 16 + absRefLevel % 10);
            //CIVSetRefLevelSub[5] = (ref_level >= 0) ? (byte)0 : (byte)1;

            //debuglabel1.Text = BitConverter.ToString(CIVSetRefLevel).Replace("-", " ");
            //debuglabel2.Text = "";
            //debuglabel3.Text = "";

            if (Radio != null && Radio.IsICOM())
            {
                Radio.SendCustomCommand(CIVSetRefLevelMain);
                //Radio.SendCustomCommand(CIVSetRefLevelSub);
            }
        }

        // Update radio with new PWR level
        private void UpdateRadioPwrlevel(int pwr_level)
        {
            if (PwrLevelSlider != null)
            {
                PwrLevelSlider.Value = pwr_level;
                PwrLevelLabel.Text = string.Format("PWR:{0,3}%", pwr_level);
            }

            int icomPower = (int)(255.0f * pwr_level / 100.0f + 0.99f); // Weird ICOM mapping of percent to binary

            CIVSetPwrLevel[2] = (byte)((icomPower / 100) % 10);
            CIVSetPwrLevel[3] = (byte)((((icomPower / 10) % 10) << 4) + (icomPower % 10));

            //debuglabel1.Text = BitConverter.ToString(CIVSetPwrLevel).Replace("-", " ");
            //debuglabel2.Text = "";
            //debuglabel3.Text = "";
            if (Radio != null && Radio.IsICOM())
            {
                Radio.SendCustomCommand(CIVSetPwrLevel);
            }
        }
    }

    public class DefaultRadioSettings
    {
        public string LowerEdgeCW = "1810;3500;5352;7000;10100;14000;18068;21000;24890;28000;50000;70000;144000;432000";
        public string UpperEdgeCW = "1840;3570;5366;7040;10130;14070;18109;21070;24920;28070;50150;71000;144100;432100";
        public string RefLevelCW = "0;0;0;0;0;0;0;0;0;0;0;0;0;0";
        public string PwrLevelCW = "10;10;10;10;10;10;10;10;10;10;10;10;10;10";

        public string LowerEdgePhone = "1860;3600;5352;7040;10100;14100;18111;21150;24931;28300;50100;70000;144200;432200";
        public string UpperEdgePhone = "2000;3800;5366;7200;10150;14350;18168;21450;24990;28600;50500;71000;144400;432300";
        public string RefLevelPhone = "0;0;0;0;0;0;0;0;0;0;0;0;0;0";
        public string PwrLevelPhone = "10;10;10;10;10;10;10;10;10;10;10;10;10;10";

        public string LowerEdgeDigital = "1840;3570;5352;7040;10130;14070;18089;21070;24910;28070;50300;70000;144000;432000";
        public string UpperEdgeDigital = "1860;3600;5366;7080;10150;14100;18109;21150;24932;28110;50350;71000;144400;432400";
        public string RefLevelDigital = "0;0;0;0;0;0;0;0;0;0;0;0;0;0";
        public string PwrLevelDigital = "10;10;10;10;10;10;10;10;10;10;10;10;10;10";

        public int EdgeSet = 4;
        public bool UseScrolling = false;
    }

    public class RadioSettings
    {
        public int Configuration;
        public readonly int Configs = 4;
        public readonly int Bands = 14;

        public int[][] LowerEdgeCW;
        public int[][] UpperEdgeCW;
        public int[][] RefLevelCW;
        public int[][] LowerEdgePhone;
        public int[][] UpperEdgePhone;
        public int[][] RefLevelPhone;
        public int[][] LowerEdgeDigital;
        public int[][] UpperEdgeDigital;
        public int[][] RefLevelDigital;
        public int[] PwrLevelCW;
        public int[] PwrLevelPhone;
        public int[] PwrLevelDigital;
        public int [] EdgeSet;
        public bool [] Scrolling;
    }
}
