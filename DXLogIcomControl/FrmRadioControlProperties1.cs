using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using ConfigFile;

namespace DXLog.net
{
    public partial class FrmRadioControlProperties1 : Form
    {
        public RadioSettings ConfigSettings; // Why is this public not visible from DXLogIcomControl?

        public FrmRadioControlProperties1()
        {
            InitializeComponent();
        }

        public FrmRadioControlProperties1(RadioSettings sett)
        {
            InitializeComponent();

            DialogResult = DialogResult.Cancel;

            ConfigSettings = sett;

            for (int i = 1; i <= 4; i++)
            {
                cbEdgeSelection.Items.Add(i.ToString());
            }

            for (char letter = 'A'; letter < 'A' + RadioSettings.Configs; letter++)
            {
                cbConfiguration.Items.Add("Configuration " + letter);
            }

            cbEdgeSelection.Enabled = ConfigSettings.HasEdgeControl;
            chkUseScrollMode.Enabled = ConfigSettings.HasScroll;

            lbRadioName.Text = ConfigSettings.RadioModelName;

            refreshTable();
        }

        private void refreshTable()
        {
            cbEdgeSelection.SelectedIndex = ConfigSettings.EdgeSet[ConfigSettings.Configuration] - 1;
            chkUseScrollMode.Checked = ConfigSettings.UseScrolling[ConfigSettings.Configuration];

            for (int i = 0; i < RadioSettings.Bands; i++)
            {
                TextBox tbcwl = (TextBox)Controls.Find(string.Format("tbcwl{0}", i), true)[0];
                TextBox tbcwu = (TextBox)Controls.Find(string.Format("tbcwu{0}", i), true)[0];
                TextBox tbphl = (TextBox)Controls.Find(string.Format("tbphl{0}", i), true)[0];
                TextBox tbphu = (TextBox)Controls.Find(string.Format("tbphu{0}", i), true)[0];
                TextBox tbdgl = (TextBox)Controls.Find(string.Format("tbdgl{0}", i), true)[0];
                TextBox tbdgu = (TextBox)Controls.Find(string.Format("tbdgu{0}", i), true)[0];

                bool _sup = ConfigSettings.SupportedBand[i];
                tbcwl.Text = ConfigSettings.LowerEdgeCW[ConfigSettings.Configuration][i].ToString();
                tbcwl.Enabled = _sup;
                tbcwu.Text = ConfigSettings.UpperEdgeCW[ConfigSettings.Configuration][i].ToString();
                tbcwu.Enabled = _sup;
                tbphl.Text = ConfigSettings.LowerEdgePhone[ConfigSettings.Configuration][i].ToString();
                tbphl.Enabled = _sup;
                tbphu.Text = ConfigSettings.UpperEdgePhone[ConfigSettings.Configuration][i].ToString();
                tbphu.Enabled = _sup;
                tbdgl.Text = ConfigSettings.LowerEdgeDigital[ConfigSettings.Configuration][i].ToString();
                tbdgl.Enabled = _sup;
                tbdgu.Text = ConfigSettings.UpperEdgeDigital[ConfigSettings.Configuration][i].ToString();
                tbdgu.Enabled = _sup;
            }

            // Do this last since it triggers a callback
            cbConfiguration.SelectedIndex = ConfigSettings.Configuration;
        }

        private bool parseEntries()
        {
            try
            {
                for (int i = 0; i < RadioSettings.Bands; i++)
                {
                    TextBox tbcwl = (TextBox)Controls.Find(string.Format("tbcwl{0}", i), true)[0];
                    TextBox tbcwu = (TextBox)Controls.Find(string.Format("tbcwu{0}", i), true)[0];
                    ConfigSettings.LowerEdgeCW[ConfigSettings.Configuration][i] = int.Parse(tbcwl.Text);
                    ConfigSettings.UpperEdgeCW[ConfigSettings.Configuration][i] = int.Parse(tbcwu.Text);

                    TextBox tbphl = (TextBox)Controls.Find(string.Format("tbphl{0}", i), true)[0];
                    TextBox tbphu = (TextBox)Controls.Find(string.Format("tbphu{0}", i), true)[0];
                    ConfigSettings.LowerEdgePhone[ConfigSettings.Configuration][i] = int.Parse(tbphl.Text);
                    ConfigSettings.UpperEdgePhone[ConfigSettings.Configuration][i] = int.Parse(tbphu.Text);

                    TextBox tbdgl = (TextBox)Controls.Find(string.Format("tbdgl{0}", i), true)[0];
                    TextBox tbdgu = (TextBox)Controls.Find(string.Format("tbdgu{0}", i), true)[0];
                    ConfigSettings.LowerEdgeDigital[ConfigSettings.Configuration][i] = int.Parse(tbdgl.Text);
                    ConfigSettings.UpperEdgeDigital[ConfigSettings.Configuration][i] = int.Parse(tbdgu.Text);

                    ConfigSettings.EdgeSet[ConfigSettings.Configuration] = cbEdgeSelection.SelectedIndex + 1;
                    ConfigSettings.UseScrolling[ConfigSettings.Configuration] = chkUseScrollMode.Checked;
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
                Config.Save("RCWaterfallConfiguration", cbConfiguration.SelectedIndex);

                for (int cn = 0; cn < RadioSettings.Configs; cn++)
                {
                    char cl = (char)('A' + (char)cn);
                    Config.Save("RCWaterfallEdgeSet" + cl, ConfigSettings.EdgeSet[cn]);
                    Config.Save("RCWaterfallScrolling" + cl, ConfigSettings.UseScrolling[cn]);

                    Config.Save("RCWaterfallLowerEdgeCW" + cl, string.Join(";", ConfigSettings.LowerEdgeCW[cn].Select(j => j.ToString()).ToArray()));
                    Config.Save("RCWaterfallUpperEdgeCW" + cl, string.Join(";", ConfigSettings.UpperEdgeCW[cn].Select(j => j.ToString()).ToArray()));
                    Config.Save("RCWaterfallLowerEdgePhone" + cl, string.Join(";", ConfigSettings.LowerEdgePhone[cn].Select(j => j.ToString()).ToArray()));
                    Config.Save("RCWaterfallUpperEdgePhone" + cl, string.Join(";", ConfigSettings.UpperEdgePhone[cn].Select(j => j.ToString()).ToArray()));
                    Config.Save("RCWaterfallLowerEdgeDigital" + cl, string.Join(";", ConfigSettings.LowerEdgeDigital[cn].Select(j => j.ToString()).ToArray()));
                    Config.Save("RCWaterfallUpperEdgeDigital" + cl, string.Join(";", ConfigSettings.UpperEdgeDigital[cn].Select(j => j.ToString()).ToArray()));
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

            ConfigSettings.LowerEdgeCW[cbConfiguration.SelectedIndex] = def.LowerEdgeCW.Split(';').Select(s => int.Parse(s)).ToArray();
            ConfigSettings.UpperEdgeCW[cbConfiguration.SelectedIndex] = def.UpperEdgeCW.Split(';').Select(s => int.Parse(s)).ToArray();
            ConfigSettings.RefLevelCW[cbConfiguration.SelectedIndex] = def.RefLevelCW.Split(';').Select(s => int.Parse(s)).ToArray();
            ConfigSettings.PwrLevelCW = def.PwrLevelCW.Split(';').Select(s => int.Parse(s)).ToArray();

            ConfigSettings.LowerEdgePhone[cbConfiguration.SelectedIndex] = def.LowerEdgePhone.Split(';').Select(s => int.Parse(s)).ToArray();
            ConfigSettings.UpperEdgePhone[cbConfiguration.SelectedIndex] = def.UpperEdgePhone.Split(';').Select(s => int.Parse(s)).ToArray();
            ConfigSettings.RefLevelPhone[cbConfiguration.SelectedIndex] = def.RefLevelPhone.Split(';').Select(s => int.Parse(s)).ToArray();
            ConfigSettings.PwrLevelPhone = def.PwrLevelPhone.Split(';').Select(s => int.Parse(s)).ToArray();

            ConfigSettings.LowerEdgeDigital[cbConfiguration.SelectedIndex] = def.LowerEdgeDigital.Split(';').Select(s => int.Parse(s)).ToArray();
            ConfigSettings.UpperEdgeDigital[cbConfiguration.SelectedIndex] = def.UpperEdgeDigital.Split(';').Select(s => int.Parse(s)).ToArray();
            ConfigSettings.RefLevelDigital[cbConfiguration.SelectedIndex] = def.RefLevelDigital.Split(';').Select(s => int.Parse(s)).ToArray();
            ConfigSettings.PwrLevelDigital = def.PwrLevelDigital.Split(';').Select(s => int.Parse(s)).ToArray();

            refreshTable();
        }

        private void cbConfiguration_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (parseEntries())
            {
                ConfigSettings.Configuration = cbConfiguration.SelectedIndex;
                refreshTable();
            }
        }
    }
}
