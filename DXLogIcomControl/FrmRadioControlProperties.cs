using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using ConfigFile;

namespace DXLog.net
{
    public partial class FrmRadioControlProperties : Form
    {
        public RadioSettings ConfigSettings; // Why is this public not visible from DXLogIcomControl?

        public FrmRadioControlProperties()
        {
            InitializeComponent();
        }

        public FrmRadioControlProperties(RadioSettings sett)
        {
            InitializeComponent();

            DialogResult = DialogResult.Cancel;

            ConfigSettings = sett;

            if (ConfigSettings.HasEdgeControl)
            {
                cbEdgeSelection.Enabled = true;
                for (int i = 1; i <= RadioSettings.Edges; i++)
                {
                    cbEdgeSelection.Items.Add(i.ToString());
                }
            }
            else
            {
                cbEdgeSelection.Enabled = false;
                for (int i = 1; i <= RadioSettings.Edges; i++)
                {
                    cbEdgeSelection.Items.Add("-");
                }
            }

            for (char letter = 'A'; letter < 'A' + RadioSettings.Configs; letter++)
            {
                cbConfiguration.Items.Add("Configuration " + letter);
            }

            chkUseScrollMode.Enabled = ConfigSettings.HasScroll;

            lbRadioName.Text = ConfigSettings.RadioModelName;

            RefreshTable();
        }

        private void RefreshTable()
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

                tbcwl.Text = ConfigSettings.LowerEdgeCW[ConfigSettings.Configuration][i].ToString();
                tbcwu.Text = ConfigSettings.UpperEdgeCW[ConfigSettings.Configuration][i].ToString();
                tbphl.Text = ConfigSettings.LowerEdgePhone[ConfigSettings.Configuration][i].ToString();
                tbphu.Text = ConfigSettings.UpperEdgePhone[ConfigSettings.Configuration][i].ToString();
                tbdgl.Text = ConfigSettings.LowerEdgeDigital[ConfigSettings.Configuration][i].ToString();
                tbdgu.Text = ConfigSettings.UpperEdgeDigital[ConfigSettings.Configuration][i].ToString();
            }

            // Do this last since it triggers a callback
            cbConfiguration.SelectedIndex = ConfigSettings.Configuration;
        }

        private bool ParseEntries()
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

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (ParseEntries())
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

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void BtnDefaults_Click(object sender, EventArgs e)
        {
            //DefaultRadioSettings def = new DefaultRadioSettings();

            ConfigSettings.LowerEdgeCW[cbConfiguration.SelectedIndex] = DefaultSettings.LowerEdgeCW.Split(';').Select(s => int.Parse(s)).ToArray();
            ConfigSettings.UpperEdgeCW[cbConfiguration.SelectedIndex] = DefaultSettings.UpperEdgeCW.Split(';').Select(s => int.Parse(s)).ToArray();
            ConfigSettings.RefLevelCW[cbConfiguration.SelectedIndex] = DefaultSettings.RefLevelCW.Split(';').Select(s => int.Parse(s)).ToArray();
            ConfigSettings.PwrLevelCW = DefaultSettings.PwrLevelCW.Split(';').Select(s => int.Parse(s)).ToArray();

            ConfigSettings.LowerEdgePhone[cbConfiguration.SelectedIndex] = DefaultSettings.LowerEdgePhone.Split(';').Select(s => int.Parse(s)).ToArray();
            ConfigSettings.UpperEdgePhone[cbConfiguration.SelectedIndex] = DefaultSettings.UpperEdgePhone.Split(';').Select(s => int.Parse(s)).ToArray();
            ConfigSettings.RefLevelPhone[cbConfiguration.SelectedIndex] = DefaultSettings.RefLevelPhone.Split(';').Select(s => int.Parse(s)).ToArray();
            ConfigSettings.PwrLevelPhone = DefaultSettings.PwrLevelPhone.Split(';').Select(s => int.Parse(s)).ToArray();

            ConfigSettings.LowerEdgeDigital[cbConfiguration.SelectedIndex] = DefaultSettings.LowerEdgeDigital.Split(';').Select(s => int.Parse(s)).ToArray();
            ConfigSettings.UpperEdgeDigital[cbConfiguration.SelectedIndex] = DefaultSettings.UpperEdgeDigital.Split(';').Select(s => int.Parse(s)).ToArray();
            ConfigSettings.RefLevelDigital[cbConfiguration.SelectedIndex] = DefaultSettings.RefLevelDigital.Split(';').Select(s => int.Parse(s)).ToArray();
            ConfigSettings.PwrLevelDigital = DefaultSettings.PwrLevelDigital.Split(';').Select(s => int.Parse(s)).ToArray();

            RefreshTable();
        }

        private void CbConfiguration_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ParseEntries())
            {
                ConfigSettings.Configuration = cbConfiguration.SelectedIndex;
                RefreshTable();
            }
        }
    }
}
