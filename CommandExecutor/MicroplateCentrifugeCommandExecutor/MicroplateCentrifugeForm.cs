using Serilog;
using System;
using System.Windows.Forms;
using AxCentrifugeLib;
using AxCentrifugeLoaderLib;

namespace MicroplateCentrifugeCommandExecutor
{
    public partial class MicroplateCentrifugeForm : Form
    {
        public MicroplateCentrifugeForm()
        {
            InitializeComponent();

            Log.Debug("InitComponent");
            // Operate in Blocking mode with error dialogs disabled.
            axCentrifugeLoader.Blocking = false;
            axCentrifugeLoader.Blocking = true;
        }

        public AxCentrifugeLoader GetLoader()
        {
            return axCentrifugeLoader;
        }

        private void FinishToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void NoticeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var form = new NoticeForm())
            {
                form.ShowDialog();
            }
        }

        private void loderSettingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            axCentrifugeLoader.ShowDiagsDialog(true, 0);
        }

        private void axCentrifugeLoader_Error(object sender, _DCentrifugeLoaderEvents_ErrorEvent e)
        {
            // Disable error dialogs.
            Log.Debug($"ActiveX error event description: {e.description}");
            e.cancelDisplay = true;
            Log.Debug("cancelDisplay set true.");
        }
    }
}
