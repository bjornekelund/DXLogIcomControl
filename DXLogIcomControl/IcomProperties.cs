using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using ConfigFile;

namespace DXLog.net
{
    public partial class IcomProperties : Form
    {
        public RadioSettings Settings; // Why is this public not visible from DXLogIcomControl?

        public IcomProperties()
        {
            InitializeComponent();
        }

        public IcomProperties(RadioSettings sett)
        {
            InitializeComponent();

            DialogResult = DialogResult.Cancel;

            Settings = sett;

            for (int i = 1; i <= 4; i++)
            {
                cbEdgeSelection.Items.Add(i.ToString());
            }

            for (char letter = 'A'; letter < 'A' + Settings.Configs; letter++)
            {
                cbConfiguration.Items.Add("Configuration " + letter);
            }

            refreshTable();
        }

        private void refreshTable()
        {
            cbEdgeSelection.SelectedIndex = Settings.EdgeSet[Settings.Configuration] - 1;
            chkUseScrollMode.Checked = Settings.Scrolling[Settings.Configuration];
            cbConfiguration.SelectedIndex = Settings.Configuration;

            for (int i = 0; i < Settings.Bands; i++)
            {
                TextBox tbcwl = (TextBox)Controls.Find(string.Format("tbcwl{0}", i), true)[0];
                TextBox tbcwu = (TextBox)Controls.Find(string.Format("tbcwu{0}", i), true)[0];
                TextBox tbphl = (TextBox)Controls.Find(string.Format("tbphl{0}", i), true)[0];
                TextBox tbphu = (TextBox)Controls.Find(string.Format("tbphu{0}", i), true)[0];
                TextBox tbdgl = (TextBox)Controls.Find(string.Format("tbdgl{0}", i), true)[0];
                TextBox tbdgu = (TextBox)Controls.Find(string.Format("tbdgu{0}", i), true)[0];

                tbcwl.Text = Settings.LowerEdgeCW[cbConfiguration.SelectedIndex][i].ToString();
                tbcwu.Text = Settings.UpperEdgeCW[cbConfiguration.SelectedIndex][i].ToString();
                tbphl.Text = Settings.LowerEdgePhone[cbConfiguration.SelectedIndex][i].ToString();
                tbphu.Text = Settings.UpperEdgePhone[cbConfiguration.SelectedIndex][i].ToString();
                tbdgl.Text = Settings.LowerEdgeDigital[cbConfiguration.SelectedIndex][i].ToString();
                tbdgu.Text = Settings.UpperEdgeDigital[cbConfiguration.SelectedIndex][i].ToString();
            }
        }

        private bool parseEntries()
        {
            try
            {
                for (int i = 0; i < Settings.Bands; i++)
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
                    Settings.Scrolling[Settings.Configuration] = chkUseScrollMode.Checked;
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
                Config.Save("WaterfallConfiguration", Settings.Configuration + 1);

                for (int cn = 0; cn < Settings.Configs; cn++)
                {
                    char cl = (char)('A' + (char)cn);
                    Config.Save("WaterfallEdgeSet" + cl, Settings.EdgeSet[cn]);
                    Config.Save("WaterfallScrolling" + cl, Settings.Scrolling[cn]);

                    Config.Save("WaterfallLowerEdgeCW" + cl, string.Join(";", Settings.LowerEdgeCW[cn].Select(j => j.ToString()).ToArray()));
                    Config.Save("WaterfallUpperEdgeCW" + cl, string.Join(";", Settings.UpperEdgeCW[cn].Select(j => j.ToString()).ToArray()));
                    Config.Save("WaterfallLowerEdgePhone" + cl, string.Join(";", Settings.LowerEdgePhone[cn].Select(j => j.ToString()).ToArray()));
                    Config.Save("WaterfallUpperEdgePhone" + cl, string.Join(";", Settings.UpperEdgePhone[cn].Select(j => j.ToString()).ToArray()));
                    Config.Save("WaterfallLowerEdgeDigital" + cl, string.Join(";", Settings.LowerEdgeDigital[cn].Select(j => j.ToString()).ToArray()));
                    Config.Save("WaterfallUpperEdgeDigital" + cl, string.Join(";", Settings.UpperEdgeDigital[cn].Select(j => j.ToString()).ToArray()));
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
            if (tbcwl0.Text != "" && parseEntries())
            {
                Settings.Configuration = cbConfiguration.SelectedIndex;
                refreshTable();
            }
        }
    }
}
