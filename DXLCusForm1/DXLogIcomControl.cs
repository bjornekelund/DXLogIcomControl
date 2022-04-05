﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DXLog.net;
using ConfigFile;
using IOComm;

namespace DXLog.net
{
    public partial class DXLogIcomControl : KForm
    {
        public static string CusWinName
        {
            get { return "ICOM Controller"; }
        }

        public static int CusFormID
        {
            get { return 1022; }
        }
        
        private ContestData _cdata = null;
        //private Font _windowFont = new Font("Courier New", 10, FontStyle.Regular);

        private FrmMain mainForm = null;

        private delegate void newQsoSaved(DXQSO qso);

        private readonly bool NoRadio = false; // For debugging with no radio attached
        //private string programTitle = "ICOM Automagic";
        //private readonly AssemblyName _assemblyName = Assembly.GetExecutingAssembly().GetName();
        //private static Brush SpecialGreen = (Brush)new Brush().ConvertFrom("#ff58f049");
        //private static Color SpecialGreen = Color.Green;
        //private readonly Color ActiveColor = SpecialGreen; // Color for active button
        //private readonly Color PassiveColor = Color.LightGray; // Color for passive button
        //private readonly Color BarefootColor = Color.DarkGreen; // Color for power label when barefoot
        //private readonly Color ExciterColor = Color.Black; // Color for power label when using PA
        //private readonly Color BandModeColor = Color.Blue; // Color for valid band and mode display

        // Pre-baked CI-V commands
        private byte[] CIVSetFixedMode = { 0xfe, 0xfe, 0xff, 0xe0, 0x27, 0x14, 0x00, 0x01, 0xfd };
        private byte[] CIVSetEdgeSet = { 0xfe, 0xfe, 0xff, 0xe0, 0x27, 0x16, 0x0, 0xff, 0xfd };
        private byte[] CIVSetRefLevel = { 0xfe, 0xfe, 0xff, 0xe0, 0x27, 0x19, 0x00, 0x00, 0x00, 0x00, 0xfd };
        private byte[] CIVSetPwrLevel = { 0xfe, 0xfe, 0xff, 0xe0, 0x14, 0x0a, 0x00, 0x00, 0xfd };
        private const int HamBands = 14;
        private const int MaxMHz = 470;
        private const int TableSize = 74;

        // Maps MHz to band name.
        private string[] bandName = new string[MaxMHz];
        private readonly string[] REFbandName = new string[TableSize]
            { "??m", "160m", "??m", "80m", "??m", "60m", "40m", "40m", "??m", "30m",
            "30m", "??m", "??m", "20m", "20m", "??m", "??m", "17m", "17m", "??m",
            "15m", "15m", "??m", "??m", "12m", "12m", "??m", "11m", "10m", "10m",
            "??m", "??m", "??m", "??m", "??m", "??m", "??m", "??m", "??m", "??m",
            "??m", "??m", "??m", "??m", "??m", "??m", "??m", "??m", "??m", "6m",
            "6m", "6m", "6m", "6m", "??m", "??m", "??m", "??m", "??m", "??m",
            "??m", "??m", "??m", "??m", "??m", "??m", "??m", "??m", "??m", "4m",
            "4m", "4m", "4m", "4m" };

        // Maps MHz to internal band index.
        // Bands are 160=0, 80=1, etc. up to 11=4m
        private int[] bandIndex = new int[MaxMHz];
        private readonly int[] REFbandIndex = new int[TableSize]
            { 0, 0, 0, 1, 1, 2, 3, 3, 3, 4,
            4, 4, 4, 5, 5, 5, 5, 6, 6, 6,
            7, 7, 7, 7, 8, 8, 8, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 10, 10, 10, 10, 10, 10, 10, 10, 10,
            10, 10, 10, 10, 10, 10, 10, 10, 10, 10,
            10, 11, 11, 11, 11, 11, 11, 11, 11, 11,
            11, 11, 11, 11 };

        // Maps actual MHz to radio's scope edge set on ICOM 7xxx. 54 elements.
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

        // Per mode/band waterfall edges and ref levels.
        private int[] lowerEdgeCW = new int[HamBands];
        private int[] upperEdgeCW = new int[HamBands];
        private int[] refLevelCW = new int[HamBands];
        private int[] lowerEdgePhone = new int[HamBands];
        private int[] upperEdgePhone = new int[HamBands];
        private int[] refLevelPhone = new int[HamBands];
        private int[] lowerEdgeDigital = new int[HamBands];
        private int[] upperEdgeDigital = new int[HamBands];
        private int[] refLevelDigital = new int[HamBands];
        private int[] pwrLevelCW = new int[HamBands];
        private int[] pwrLevelPhone = new int[HamBands];
        private int[] pwrLevelDigital = new int[HamBands];

        // Global variables
        private int currentLowerEdge, currentUpperEdge, currentRefLevel, currentPwrLevel;
        private int currentFrequency = 0, newMHz, currentMHz = 0;
        private string currentMode = string.Empty, newMode = string.Empty;
        private bool Barefoot;

        private byte CIVaddress = 0x94;
        private bool UseScrollMode = true;

        public DXLogIcomControl()
        {
            InitializeComponent();

        }

        public DXLogIcomControl(ContestData cdata)
        {
            InitializeComponent();
            _cdata = cdata;
            //FormLayoutChangeEvent += new FormLayoutChange(handle_FormLayoutChangeEvent);

            string message;
            string[] commandLineArguments = Environment.GetCommandLineArgs();

            while (contextMenuStrip1.Items.Count > 0)
                contextMenuStrip2.Items.Add(contextMenuStrip1.Items[0]);
            contextMenuStrip2.Items.RemoveByKey("fixWindowSizeToolStripMenuItem");
            contextMenuStrip2.Items.RemoveByKey("fontSizeToolStripMenuItem");
            contextMenuStrip2.Items.RemoveByKey("colorsToolStripMenuItem");

            // Set the decoding arrays to default
            for (int MHz = 0; MHz < MaxMHz; MHz++)
            {
                bandName[MHz] = "??m";
                bandIndex[MHz] = 1;
                RadioEdgeSet[MHz] = 1;
            }

            // Initialize using tables
            for (int MHz = 0; MHz < TableSize; MHz++)
            {
                bandName[MHz] = REFbandName[MHz];
                bandIndex[MHz] = REFbandIndex[MHz];
                RadioEdgeSet[MHz] = REFRadioEdgeSet[MHz];
            }

            // Add 2m
            for (int MHz = 137; MHz < 200; MHz++)
            {
                bandName[MHz] = "2m";
                bandIndex[MHz] = 12;
                RadioEdgeSet[MHz] = 16;
            }

            // Add 70cm
            for (int MHz = 400; MHz < 470; MHz++)
            {
                bandName[MHz] = "70cm";
                bandIndex[MHz] = 13;
                RadioEdgeSet[MHz] = 17;
            }

            // Fetch lower and upper edges and ref levels from last time, ugly solution due to limitations in WPF settings management
            //lowerEdgeCW = Properties.Settings.Default.LowerEdgesCW.Split(';').Select(s => int.Parse(s)).ToArray();
            //upperEdgeCW = Properties.Settings.Default.UpperEdgesCW.Split(';').Select(s => int.Parse(s)).ToArray();
            //refLevelCW = Properties.Settings.Default.RefLevelsCW.Split(';').Select(s => int.Parse(s)).ToArray();
            //pwrLevelCW = Properties.Settings.Default.PwrLevelsCW.Split(';').Select(s => int.Parse(s)).ToArray();

            //lowerEdgePhone = Properties.Settings.Default.LowerEdgesPhone.Split(';').Select(s => int.Parse(s)).ToArray();
            //upperEdgePhone = Properties.Settings.Default.UpperEdgesPhone.Split(';').Select(s => int.Parse(s)).ToArray();
            //refLevelPhone = Properties.Settings.Default.RefLevelsPhone.Split(';').Select(s => int.Parse(s)).ToArray();
            //pwrLevelPhone = Properties.Settings.Default.PwrLevelsPhone.Split(';').Select(s => int.Parse(s)).ToArray();

            //lowerEdgeDigital = Properties.Settings.Default.LowerEdgesDigital.Split(';').Select(s => int.Parse(s)).ToArray();
            //upperEdgeDigital = Properties.Settings.Default.UpperEdgesDigital.Split(';').Select(s => int.Parse(s)).ToArray();
            //refLevelDigital = Properties.Settings.Default.RefLevelsDigital.Split(';').Select(s => int.Parse(s)).ToArray();
            //pwrLevelDigital = Properties.Settings.Default.PwrLevelsDigital.Split(';').Select(s => int.Parse(s)).ToArray();

            //if (lowerEdgeCW.Length != HamBands)
            //{
            //    Properties.Settings.Default.Reset();
            //}


            if (mainForm == null)
            {
                mainForm = (FrmMain)(ParentForm ?? Owner);
                if (mainForm != null)
                {
                    mainForm.scheduler.Second += UpdateRadio;
                    //_cdata.ActiveVFOChanged += new ContestData.ActiveVFOChange(UpdateRadio);
                    //_cdata.ActiveRadioBandChanged += new ContestData.ActiveRadioBandChange(UpdateRadio);
                    //_cdata.FocusedRadioChanged += new ContestData.FocusedRadioChange(UpdateRadio);
                }
            }


        }

        // Save all settings when closing program
        private void OnClosing(object sender, EventArgs e)
        {
            // Remember window location 
            //Properties.Settings.Default.Top = Top;
            //Properties.Settings.Default.Left = Left;

            //// Ugly but because WPF Settings can not store arrays. 
            //// Each array is turned into a formatted string that can be read back using Parse()
            //Properties.Settings.Default.LowerEdgesCW = string.Join(";", lowerEdgeCW.Select(i => i.ToString()).ToArray());
            //Properties.Settings.Default.UpperEdgesCW = string.Join(";", upperEdgeCW.Select(i => i.ToString()).ToArray());
            //Properties.Settings.Default.RefLevelsCW = string.Join(";", refLevelCW.Select(i => i.ToString()).ToArray());
            //Properties.Settings.Default.PwrLevelsCW = string.Join(";", pwrLevelCW.Select(i => i.ToString()).ToArray());

            //Properties.Settings.Default.LowerEdgesPhone = string.Join(";", lowerEdgePhone.Select(i => i.ToString()).ToArray());
            //Properties.Settings.Default.UpperEdgesPhone = string.Join(";", upperEdgePhone.Select(i => i.ToString()).ToArray());
            //Properties.Settings.Default.RefLevelsPhone = string.Join(";", refLevelPhone.Select(i => i.ToString()).ToArray());
            //Properties.Settings.Default.PwrLevelsPhone = string.Join(";", pwrLevelPhone.Select(i => i.ToString()).ToArray());

            //Properties.Settings.Default.LowerEdgesDigital = string.Join(";", lowerEdgeDigital.Select(i => i.ToString()).ToArray());
            //Properties.Settings.Default.UpperEdgesDigital = string.Join(";", upperEdgeDigital.Select(i => i.ToString()).ToArray());
            //Properties.Settings.Default.RefLevelsDigital = string.Join(";", refLevelDigital.Select(i => i.ToString()).ToArray());
            //Properties.Settings.Default.PwrLevelsDigital = string.Join(";", pwrLevelDigital.Select(i => i.ToString()).ToArray());

            ////Properties.Settings.Default.COMport = ComPort;
            //Properties.Settings.Default.Barefoot = Barefoot;

            //Properties.Settings.Default.Save();

            mainForm.scheduler.Second -= UpdateRadio;
        }

        //private void DXLogIcomControl_FormClosing(object sender, FormClosingEventArgs e)
        //{
        //}

        //private void handle_FormLayoutChangeEvent()
        //{
        //    InitializeLayout();
        //}

        public override void InitializeLayout()
        {
            //InitializeLayout(_windowFont);
            //if (FormLayout.FontName.Contains("Courier"))
            //    _windowFont = new Font(FormLayout.FontName, FormLayout.FontSize, FontStyle.Regular);
            //else
            //    _windowFont = Helper.GetSpecialFont(FontStyle.Regular, FormLayout.FontSize);

        }

        private void UpdateRadio()
        {
            UpdateRadio(1);
        }

        private void UpdateRadio(int radionumber)
        {
            currentMHz = newMHz;
            currentMode = newMode;

            switch (currentMode)
            {
                case "CW":
                    currentLowerEdge = lowerEdgeCW[bandIndex[currentMHz]];
                    currentUpperEdge = upperEdgeCW[bandIndex[currentMHz]];
                    currentRefLevel = refLevelCW[bandIndex[currentMHz]];
                    currentPwrLevel = pwrLevelCW[bandIndex[currentMHz]];
                    break;
                case "Phone":
                    currentLowerEdge = lowerEdgePhone[bandIndex[currentMHz]];
                    currentUpperEdge = upperEdgePhone[bandIndex[currentMHz]];
                    currentRefLevel = refLevelPhone[bandIndex[currentMHz]];
                    currentPwrLevel = pwrLevelPhone[bandIndex[currentMHz]];
                    break;
                default:
                    currentLowerEdge = lowerEdgeDigital[bandIndex[currentMHz]];
                    currentUpperEdge = upperEdgeDigital[bandIndex[currentMHz]];
                    currentRefLevel = refLevelDigital[bandIndex[currentMHz]];
                    currentPwrLevel = pwrLevelDigital[bandIndex[currentMHz]];
                    break;
            }

            // Execute changes to the UI on main thread 
            //Application.Current.Dispatcher.Invoke(new Action(() =>
            //{
                // Highlight band-mode button and exit Zoomed mode if active

                // Allow entry in edge text boxes 
                //LowerEdgeTextbox.Enabled= true;
                //UpperEdgeTextbox.Enabled = true;

                // Update UI and waterfall edges and ref level in radio 
                UpdateRadioEdges(currentLowerEdge, currentUpperEdge, RadioEdgeSet[currentMHz]);
                UpdateRadioReflevel(currentRefLevel);
                UpdateRadioPwrlevel(currentPwrLevel);

                // Update band/mode display in UI
                //BandLabel.Text = bandName[newMHz];
                //BandLabel.ForeColor= BandModeColor;
                //ModeLabel.Text = newMode;
                //ModeLabel.ForeColor = BandModeColor;

                // Enable UI components
                //ZoomButton.Enabled = true;
                //BandModeButton.Enabled= true;
                //LowerEdgeTextbox.Enabled= true;
                //UpperEdgeTextbox.Enabled = true;
                //RefLevelSlider.Enabled = true;
                //PwrLevelSlider.Enabled = true;
                //PwrLevelLabel.Enabled = true;
            //}));
        }

        // On hitting a key in upper and lower edge text boxes
        private void OnEdgeTextboxKeydown(object sender, KeyEventArgs e)
        {
            int lower, upper;

            if (e.KeyData == Keys.Return) // Only parse input when ENTER is hit 
            {

                try // Parse and ignore input if there are parsing errors
                {
                    //lower = int.Parse(LowerEdgeTextbox.Text);
                    //upper = int.Parse(UpperEdgeTextbox.Text);
                }
                catch
                {
                    return; // Ignore input if parsing failed
                }

                // We have a successful parse, assign values
                //currentLowerEdge = lower;
                //currentUpperEdge = upper;

                switch (currentMode)
                {
                    case "CW":
                        lowerEdgeCW[bandIndex[currentMHz]] = currentLowerEdge;
                        upperEdgeCW[bandIndex[currentMHz]] = currentUpperEdge;
                        currentRefLevel = refLevelCW[bandIndex[currentMHz]];
                        break;
                    case "Phone":
                        lowerEdgePhone[bandIndex[currentMHz]] = currentLowerEdge;
                        upperEdgePhone[bandIndex[currentMHz]] = currentUpperEdge;
                        currentRefLevel = refLevelPhone[bandIndex[currentMHz]];
                        break;
                    default:
                        lowerEdgeDigital[bandIndex[currentMHz]] = currentLowerEdge;
                        upperEdgeDigital[bandIndex[currentMHz]] = currentUpperEdge;
                        currentRefLevel = refLevelDigital[bandIndex[currentMHz]];
                        break;
                }

                UpdateRadioEdges(currentLowerEdge, currentUpperEdge, RadioEdgeSet[currentMHz]);
                UpdateRadioReflevel(currentRefLevel);

                // Toggle focus betwen the two entry text boxes
                //if (sender == LowerEdgeTextbox)
                //{
                //    UpperEdgeTextbox.Focus();
                //}
                //else
                //{
                //    LowerEdgeTextbox.Focus();
                //}
            }
        }
        //private void BandModeButton_Click(object sender, EventArgs e)
        //{

        //}

        // On band-mode button clicked
        private void OnBandModeButton(object sender, EventArgs e)
        {
            switch (currentMode)
            {
                case "CW":
                    currentLowerEdge = lowerEdgeCW[bandIndex[currentMHz]];
                    currentUpperEdge = upperEdgeCW[bandIndex[currentMHz]];
                    currentRefLevel = refLevelCW[bandIndex[currentMHz]];
                    break;
                case "Phone":
                    currentLowerEdge = lowerEdgePhone[bandIndex[currentMHz]];
                    currentUpperEdge = upperEdgePhone[bandIndex[currentMHz]];
                    currentRefLevel = refLevelPhone[bandIndex[currentMHz]];
                    break;
                default: // All other modes = Digital 
                    currentLowerEdge = lowerEdgeDigital[bandIndex[currentMHz]];
                    currentUpperEdge = upperEdgeDigital[bandIndex[currentMHz]];
                    currentRefLevel = refLevelDigital[bandIndex[currentMHz]];
                    break;
            }

            UpdateRadioEdges(currentLowerEdge, currentUpperEdge, RadioEdgeSet[currentMHz]);

            UpdateRadioReflevel(currentRefLevel);

            //LowerEdgeTextbox.Enabled = true;
            //UpperEdgeTextbox.Enabled = true;

            //ZoomButton.BackColor = PassiveColor;
            //ZoomButton.ForeColor = PassiveColor;
            //BandModeButton.BackColor= ActiveColor;
            //BandModeButton.ForeColor = ActiveColor;
        }

        // On arrow key modification of slider
        private void OnRefSliderKey(object sender, KeyEventArgs e)
        {
            UpdateRefSlider();
        }

        private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form prop = new IcomProperties();

            if (prop.ShowDialog() == DialogResult.OK)
            {
                // Save stuff
            }
        }

        private void ToggleBarefoot(object sender, EventArgs e)
        {
            Barefoot = !Barefoot;

            UpdateRadioPwrlevel(currentPwrLevel);
        }

        // On mouse modification of slider
        private void OnRefSliderMouseClick(object sender, MouseEventArgs e)
        {
            UpdateRefSlider();
        }

        //private void OnRefSliderMouseClick(object sender, EventArgs e)
        //{
        //}

        // Update ref level on slider action
        private void UpdateRefSlider()
        {
            currentRefLevel = (int)(RefLevelSlider.Value + 0.0f);

            UpdateRadioReflevel(currentRefLevel);

            switch (currentMode)
            {
                case "CW":
                    refLevelCW[bandIndex[currentMHz]] = currentRefLevel;
                    break;
                case "Phone":
                    refLevelPhone[bandIndex[currentMHz]] = currentRefLevel;
                    break;
                default:
                    refLevelDigital[bandIndex[currentMHz]] = currentRefLevel;
                    break;
            }
        }

        // on mouse movement of power slider
        private void OnPwrSliderMouseClick(object sender, MouseEventArgs e)
        {
            UpdatePwrSlider();
        }

        // Update pwr level on slider action
        private void UpdatePwrSlider()
        {
            currentPwrLevel = (int)(PwrLevelSlider.Value + 0.0f);
            UpdateRadioPwrlevel(currentPwrLevel);

            if (currentMHz != 0)
            {
                switch (currentMode)
                {
                    case "CW":
                        pwrLevelCW[bandIndex[currentMHz]] = currentPwrLevel;
                        break;
                    case "Phone":
                        pwrLevelPhone[bandIndex[currentMHz]] = currentPwrLevel;
                        break;
                    default:
                        pwrLevelDigital[bandIndex[currentMHz]] = currentPwrLevel;
                        break;
                }
            }
        }

        // Update radio with new waterfall edges
        private void UpdateRadioEdges(int lower_edge, int upper_edge, int ICOMedgeSegment)
        {
            // Compose CI-V command to set waterfall edges
            byte[] CIVSetEdges = new byte[19]
            {
                0xfe, 0xfe, CIVaddress, 0xe0,
                0x27, 0x1e,
                (byte)((ICOMedgeSegment / 10) * 16 + (ICOMedgeSegment % 10)),
                (byte)Config.Read("ICOMedgeSet", 4),
                0x00, // Lower 10Hz & 1Hz
                (byte)((lower_edge % 10) * 16 + 0), // 1kHz & 100Hz
                (byte)(((lower_edge / 100) % 10) * 16 + ((lower_edge / 10) % 10)), // 100kHz & 10kHz
                (byte)(((lower_edge / 10000) % 10) * 16 + (lower_edge / 1000) % 10), // 10MHz & 1MHz
                (byte)(((lower_edge / 1000000) % 10) * 16 + (lower_edge / 100000) % 10), // 1GHz & 100MHz
                0x00, // // Upper 10Hz & 1Hz 
                (byte)((upper_edge % 10) * 16 + 0), // 1kHz & 100Hz
                (byte)(((upper_edge / 100) % 10) * 16 + (upper_edge / 10) % 10), // 100kHz & 10kHz
                (byte)(((upper_edge / 10000) % 10) * 16 + (upper_edge / 1000) % 10), // 10MHz & 1MHz
                (byte)(((upper_edge / 1000000) % 10) * 16 + (upper_edge / 100000) % 10), // 1GHz & 100MHz
                0xfd
            };

            // Update UI if present (this function may be called before main window is created)
            //if (LowerEdgeTextbox != null)
            //{
            //    LowerEdgeTextbox.Text = lower_edge.ToString();
            //    UpperEdgeTextbox.Text = upper_edge.ToString();
            //}

            // Update radio if we are not in debug mode
            if (!NoRadio)
            {
                CIVSetFixedMode[2] = CIVaddress;
                CIVSetFixedMode[7] = (byte)(UseScrollMode ? 0x03 : 0x01);
                CIVSetEdgeSet[2] = CIVaddress;
                CIVSetEdgeSet[7] = (byte)Config.Read("ICOMedgeSet", 4);

                CATCommon radio1 = mainForm.COMMainProvider.RadioObject(1);

                if (radio1 != null)
                {
                    radio1.SendCustomCommand(CIVSetFixedMode);
                    radio1.SendCustomCommand(CIVSetEdgeSet);
                    radio1.SendCustomCommand(CIVSetEdges);
                }
            }
        }

        // Update radio with new REF level
        private void UpdateRadioReflevel(int ref_level)
        {
            int absRefLevel = (ref_level >= 0) ? ref_level : -ref_level;

            CIVSetRefLevel[2] = CIVaddress;
            CIVSetRefLevel[7] = (byte)((absRefLevel / 10) * 16 + absRefLevel % 10);
            CIVSetRefLevel[9] = (ref_level >= 0) ? (byte)0 : (byte)1;

            // Update UI if present (this function may be called before main window is created)
            if (RefLevelLabel != null)
            {
                RefLevelSlider.Value = ref_level;
                RefLevelLabel.Text = string.Format("Ref: {0:+#;-#;0}dB", ref_level);
            }

            // Update radio if we are not debugging
            //if (!NoRadio && Port.IsOpen)
            //{
            //    Port.Write(CIVSetRefLevel, 0, CIVSetRefLevel.Length); // set edge set EdgeSet
            //}
        }

        // Update radio with new PWR level
        private void UpdateRadioPwrlevel(int pwr_level)
        {
            int usedPower;

            // Update UI if present (this function may be called before main window is created)
            if (PwrLevelLabel != null)
            {
                if (Barefoot)
                {
                    PwrLevelSlider.Enabled = false;
                    //PwrLevelLabel.ForeColor = BarefootColor;
                    //PwrLevelLabel.FontWeight = FontWeights.Bold;
                    usedPower = 255;
                    PwrLevelSlider.Value = 100;
                    PwrLevelLabel.Text = "Pwr:100%";
                }
                else
                {
                    PwrLevelSlider.Enabled = true;
                    //PwrLevelLabel.ForeColor = ExciterColor;
                    //PwrLevelLabel.FontWeight = FontWeights.Normal;
                    usedPower = (int)(255.0f * pwr_level / 100.0f + 0.99f); // Weird ICOM mapping of percent to binary
                    PwrLevelSlider.Value = pwr_level;
                    PwrLevelLabel.Text = string.Format("Pwr:{0,3}%", pwr_level);
                }

                CIVSetPwrLevel[2] = CIVaddress;
                CIVSetPwrLevel[6] = (byte)((usedPower / 100) % 10);
                CIVSetPwrLevel[7] = (byte)((((usedPower / 10) % 10) << 4) + (usedPower % 10));

                // Update radio if present
                //if (!NoRadio && Port.IsOpen)
                //{
                //    Port.Write(CIVSetPwrLevel, 0, CIVSetPwrLevel.Length); // set power level 
                //}
            }
        }
    }
    public class DefaultValues
    {
        public string LowerEdgeCW = "1810;3500;5352;7000;10100;14000;18068;21000;24890;28000;50000;70000;144000;432000";
        public string UpperEdgeCW = "1840;3570;5366;7040;10130;14070;18109;21070;24920;28070;50150;71000;144100;432100";
        public string RefLevelCW = "0;0;0;0;0;0;0;0;0;0;0;0;0;0";
        public string PwrLevelCW = "18;18;18;18;18;18;18;18;18;18;18;18;18;18";

        public string LowerEdgePhone = "1840;3600;5353;7040;10100;14100;18111;21150;24931;28300;50100;70000;144200;432200";
        public string UpperEdgePhone = "2000;3800;5366;7200;10150;14350;18168;21450;24990;28600;50500;71000;144400;432300";
        public string RefLevelPhone = "0;0;0;0;0;0;0;0;0;0;0;0;0;0";
        public string PwrLevelPhone = "18;18;18;18;18;18;18;18;18;18;18;18;18;18";

        public string LowerEdgeDigital = "1840;3570;5352;7040;10130;14070;18089;21070;24910;28070;50300;70000;144000;432000";
        public string UpperEdgeDigital = "1860;3600;5366;7080;10150;14100;18109;21150;24932;28110;50350;71000;144400;432400";
        public string RefLevelDigital = "0;0;0;0;0;0;0;0;0;0;0;0;0;0";
        public string PwrLevelDigital = "18;18;18;18;18;18;18;18;18;18;18;18;18;18";

        public int EdgeSet = 4;
        public bool UseScrolling = true;
    }
}