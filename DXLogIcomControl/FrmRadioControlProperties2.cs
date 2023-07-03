using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using ConfigFile;

namespace DXLog.net
{
    public partial class FrmRadioControlProperties2 : Form
    {
        public RadioSettings Settings; // Why is this public not visible from DXLogIcomControl?

        public FrmRadioControlProperties2()
        {
            InitializeComponent();
        }

        public FrmRadioControlProperties2(RadioSettings sett)
        {
            InitializeComponent();

            DialogResult = DialogResult.Cancel;

            Settings = sett;

            for (int i = 1; i <= 4; i++)
            {
                cbEdgeSelection.Items.Add(i.ToString());
            }

            for (char letter = 'A'; letter < 'A' + RadioSettings.Configs; letter++)
            {
                cbConfiguration.Items.Add("Configuration " + letter);
            }

            cbEdgeSelection.Enabled = Settings.HasEdgeControl;
            chkUseScrollMode.Enabled = Settings.HasScroll;

            lbRadioName.Text = Settings.RadioModelName;

            switch (Settings.RadioModel)
            {
                case RadioType.IC905:
                    break;
                case RadioType.IC9700:
                    tbcwl3.Enabled = false; // 4m
                    tbcwu3.Enabled = false;
                    tbphl3.Enabled = false;
                    tbphu3.Enabled = false;
                    tbdgl3.Enabled = false;
                    tbdgu3.Enabled = false;

                    tbcwl4.Enabled = false; // 2m
                    tbcwu4.Enabled = false;
                    tbphl4.Enabled = false;
                    tbphu4.Enabled = false;
                    tbdgl4.Enabled = false;
                    tbdgu4.Enabled = false;

                    tbcwl5.Enabled = false; // 70cm
                    tbcwu5.Enabled = false;
                    tbphl5.Enabled = false;
                    tbphu5.Enabled = false;
                    tbdgl5.Enabled = false;
                    tbdgu5.Enabled = false;
                    break;
            }

            refreshTable();
        }

        private void refreshTable()
        {
            cbEdgeSelection.SelectedIndex = Settings.EdgeSet[Settings.Configuration] - 1;
            chkUseScrollMode.Checked = Settings.UseScrolling[Settings.Configuration];

            for (int i = 0; i < RadioSettings.Bands; i++)
            {
                TextBox tbcwl = (TextBox)Controls.Find(string.Format("tbcwl{0}", i), true)[0];
                TextBox tbcwu = (TextBox)Controls.Find(string.Format("tbcwu{0}", i), true)[0];
                TextBox tbphl = (TextBox)Controls.Find(string.Format("tbphl{0}", i), true)[0];
                TextBox tbphu = (TextBox)Controls.Find(string.Format("tbphu{0}", i), true)[0];
                TextBox tbdgl = (TextBox)Controls.Find(string.Format("tbdgl{0}", i), true)[0];
                TextBox tbdgu = (TextBox)Controls.Find(string.Format("tbdgu{0}", i), true)[0];

                tbcwl.Text = Settings.LowerEdgeCW[Settings.Configuration][i].ToString();
                tbcwu.Text = Settings.UpperEdgeCW[Settings.Configuration][i].ToString();
                tbphl.Text = Settings.LowerEdgePhone[Settings.Configuration][i].ToString();
                tbphu.Text = Settings.UpperEdgePhone[Settings.Configuration][i].ToString();
                tbdgl.Text = Settings.LowerEdgeDigital[Settings.Configuration][i].ToString();
                tbdgu.Text = Settings.UpperEdgeDigital[Settings.Configuration][i].ToString();
            }

            // Do this last since it triggers a callback
            cbConfiguration.SelectedIndex = Settings.Configuration;
        }

        private bool parseEntries()
        {
            try
            {
                for (int i = 0; i < RadioSettings.Bands; i++)
                {
                    TextBox tbcwl = (TextBox)Controls.Find(string.Format("tbcwl{0}", i), true)[0];
                    TextBox tbcwu = (TextBox)Controls.Find(string.Format("tbcwu{0}", i), true)[0];
                    Settings.LowerEdgeCW[Settings.Configuration][i] = int.Parse(tbcwl.Text);
                    Settings.UpperEdgeCW[Settings.Configuration][i] = int.Parse(tbcwu.Text);

                    TextBox tbphl = (TextBox)Controls.Find(string.Format("tbphl{0}", i), true)[0];
                    TextBox tbphu = (TextBox)Controls.Find(string.Format("tbphu{0}", i), true)[0];
                    Settings.LowerEdgePhone[Settings.Configuration][i] = int.Parse(tbphl.Text);
                    Settings.UpperEdgePhone[Settings.Configuration][i] = int.Parse(tbphu.Text);

                    TextBox tbdgl = (TextBox)Controls.Find(string.Format("tbdgl{0}", i), true)[0];
                    TextBox tbdgu = (TextBox)Controls.Find(string.Format("tbdgu{0}", i), true)[0];
                    Settings.LowerEdgeDigital[Settings.Configuration][i] = int.Parse(tbdgl.Text);
                    Settings.UpperEdgeDigital[Settings.Configuration][i] = int.Parse(tbdgu.Text);

                    Settings.EdgeSet[Settings.Configuration] = cbEdgeSelection.SelectedIndex + 1;
                    Settings.UseScrolling[Settings.Configuration] = chkUseScrollMode.Checked;
                }
            }
            catch
            {
                MessageBox.Show("Invalid entry", "ICOM control properties", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (parseEntries())
            {
                Config.Save("RCWaterfallConfiguration", ((char)(cbConfiguration.SelectedIndex + 'A')).ToString());

                for (int cn = 0; cn < RadioSettings.Configs; cn++)
                {
                    char cl = (char)('A' + (char)cn);
                    Config.Save("RCWaterfallEdgeSet" + cl, Settings.EdgeSet[cn]);
                    Config.Save("RCWaterfallScrolling" + cl, Settings.UseScrolling[cn]);

                    Config.Save("RCWaterfallLowerEdgeCW" + cl, string.Join(";", Settings.LowerEdgeCW[cn].Select(j => j.ToString()).ToArray()));
                    Config.Save("RCWaterfallUpperEdgeCW" + cl, string.Join(";", Settings.UpperEdgeCW[cn].Select(j => j.ToString()).ToArray()));
                    Config.Save("RCWaterfallLowerEdgePhone" + cl, string.Join(";", Settings.LowerEdgePhone[cn].Select(j => j.ToString()).ToArray()));
                    Config.Save("RCWaterfallUpperEdgePhone" + cl, string.Join(";", Settings.UpperEdgePhone[cn].Select(j => j.ToString()).ToArray()));
                    Config.Save("RCWaterfallLowerEdgeDigital" + cl, string.Join(";", Settings.LowerEdgeDigital[cn].Select(j => j.ToString()).ToArray()));
                    Config.Save("RCWaterfallUpperEdgeDigital" + cl, string.Join(";", Settings.UpperEdgeDigital[cn].Select(j => j.ToString()).ToArray()));
                }

                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnDefaults_Click(object sender, EventArgs e)
        {
            DefaultRadioSettings def = new DefaultRadioSettings();

            Settings.LowerEdgeCW[cbConfiguration.SelectedIndex] = def.LowerEdgeCW.Split(';').Select(s => int.Parse(s)).ToArray();
            Settings.UpperEdgeCW[cbConfiguration.SelectedIndex] = def.UpperEdgeCW.Split(';').Select(s => int.Parse(s)).ToArray();
            Settings.RefLevelCW[cbConfiguration.SelectedIndex] = def.RefLevelCW.Split(';').Select(s => int.Parse(s)).ToArray();
            Settings.PwrLevelCW = def.PwrLevelCW.Split(';').Select(s => int.Parse(s)).ToArray();

            Settings.LowerEdgePhone[cbConfiguration.SelectedIndex] = def.LowerEdgePhone.Split(';').Select(s => int.Parse(s)).ToArray();
            Settings.UpperEdgePhone[cbConfiguration.SelectedIndex] = def.UpperEdgePhone.Split(';').Select(s => int.Parse(s)).ToArray();
            Settings.RefLevelPhone[cbConfiguration.SelectedIndex] = def.RefLevelPhone.Split(';').Select(s => int.Parse(s)).ToArray();
            Settings.PwrLevelPhone = def.PwrLevelPhone.Split(';').Select(s => int.Parse(s)).ToArray();

            Settings.LowerEdgeDigital[cbConfiguration.SelectedIndex] = def.LowerEdgeDigital.Split(';').Select(s => int.Parse(s)).ToArray();
            Settings.UpperEdgeDigital[cbConfiguration.SelectedIndex] = def.UpperEdgeDigital.Split(';').Select(s => int.Parse(s)).ToArray();
            Settings.RefLevelDigital[cbConfiguration.SelectedIndex] = def.RefLevelDigital.Split(';').Select(s => int.Parse(s)).ToArray();
            Settings.PwrLevelDigital = def.PwrLevelDigital.Split(';').Select(s => int.Parse(s)).ToArray();

            refreshTable();
        }

        private void cbConfiguration_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (parseEntries())
            {
                Settings.Configuration = cbConfiguration.SelectedIndex;
                refreshTable();
            }
        }
    }
}
