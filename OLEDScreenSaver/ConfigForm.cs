using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OLEDScreenSaver
{
    public partial class ConfigForm : Form
    {
        private readonly IConfigurationRepository _configRepository;
        private readonly IScreenSaverManager _screenSaverManager;
        private readonly OledFormManager _oledFormManager;
        private readonly ILogger _logger;
        private readonly IScreenService _screenService;

        public ConfigForm(
            IConfigurationRepository configRepository, 
            IScreenSaverManager screenSaverManager,
            OledFormManager oledFormManager,
            ILogger logger,
            IScreenService screenService)
        {
            _configRepository = configRepository ?? throw new ArgumentNullException(nameof(configRepository));
            _screenSaverManager = screenSaverManager ?? throw new ArgumentNullException(nameof(screenSaverManager));
            _oledFormManager = oledFormManager ?? throw new ArgumentNullException(nameof(oledFormManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _screenService = screenService ?? throw new ArgumentNullException(nameof(screenService));

            try
            {
                InitializeComponent();
                this.Icon = Properties.Resources.Alecive_Flatwoken_Apps_Computer_Screensaver;
                startupCheckbox.Checked = _configRepository.LoadStartup();
                screenNameTextbox.Text = _configRepository.LoadScreenName();
                dimCheckbox.Checked = _configRepository.LoadDimEnabled();

                var loadedTimeout = _configRepository.LoadTimeout();
                timeoutNumericUpDown.Value = (decimal)loadedTimeout;
                timeoutTrackBar.Value = Math.Max(timeoutTrackBar.Minimum, Math.Min(timeoutTrackBar.Maximum, (int)(loadedTimeout * 10)));
                
                var loadedSecondStage = _configRepository.LoadSecondStageTimeout();
                secondStageNumericUpDown.Value = (decimal)loadedSecondStage;
                secondStageTrackBar.Value = Math.Max(secondStageTrackBar.Minimum, Math.Min(secondStageTrackBar.Maximum, (int)(loadedSecondStage * 10)));

                var loadedDimPercentage = _configRepository.LoadDimPercentage();
                dimPercentageNumericUpDown.Value = loadedDimPercentage;
                dimPercentageTrackBar.Value = Math.Max(dimPercentageTrackBar.Minimum, Math.Min(dimPercentageTrackBar.Maximum, loadedDimPercentage));

                var loadedAnimationDuration = _configRepository.LoadAnimationDuration();
                animationDurationNumericUpDown.Value = loadedAnimationDuration;
                animationDurationTrackBar.Value = Math.Max(animationDurationTrackBar.Minimum, Math.Min(animationDurationTrackBar.Maximum, loadedAnimationDuration));

                UpdateDimState();
                PopulateScreenList();
            }
            catch (Exception ex)
            {
                _logger.Error("Error initializing ConfigForm", ex);
                MessageBox.Show($"Error initializing configuration form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void PopulateScreenList()
        {
            try
            {
                screenCheckedListBox.Items.Clear();
                var friendlyNames = _screenService.GetAllMonitorsFriendlyNames().ToList();
                var allScreens = Screen.AllScreens;
                
                for (var i = 0; i < allScreens.Length; i++)
                {
                    var name = (i < friendlyNames.Count && !string.IsNullOrEmpty(friendlyNames[i])) 
                        ? friendlyNames[i] 
                        : $"Display {allScreens[i].DeviceName}";
                    screenCheckedListBox.Items.Add(name);
                }

                var selectedScreens = _configRepository.LoadScreenNames();
                for (var i = 0; i < screenCheckedListBox.Items.Count; i++)
                {
                    if (selectedScreens.Contains(screenCheckedListBox.Items[i].ToString()))
                    {
                        screenCheckedListBox.SetItemChecked(i, true);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error populating screen list", ex);
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            var selectedScreens = screenCheckedListBox.CheckedItems.Cast<string>().ToList();
            if (selectedScreens.Count == 0)
            {
                MessageBox.Show("Please select at least one OLED screen.", "No Screens Selected", MessageBoxButtons.OK);
                return;
            }

            _configRepository.SaveScreenNames(selectedScreens);
            _configRepository.SaveTimeout(timeoutNumericUpDown.Value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
            _configRepository.SaveSecondStageTimeout(secondStageNumericUpDown.Value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
            _configRepository.SaveDimEnabled(dimCheckbox.Checked);
            _configRepository.SaveDimPercentage(dimPercentageNumericUpDown.Value.ToString());
            _configRepository.SaveAnimationDuration(animationDurationNumericUpDown.Value.ToString());
            _configRepository.SetStartup(startupCheckbox.Checked);

            this.DialogResult = DialogResult.Yes;
            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.No;
            Close();
        }

        private void DimCheckbox_CheckedChanged(object sender, EventArgs e) => UpdateDimState();

        private void UpdateDimState()
        {
            bool en = dimCheckbox.Checked;
            timeoutTrackBar.Enabled = timeoutNumericUpDown.Enabled = timeoutLabel.Enabled = en;
            dimPercentageTrackBar.Enabled = dimPercentageNumericUpDown.Enabled = dimPercentageLabel.Enabled = en;
            animationDurationTrackBar.Enabled = animationDurationNumericUpDown.Enabled = animationDurationLabel.Enabled = en;
            secondStageTimeoutLabel.Text = en ? "Full black out delay (min)" : "Black out after (min)";
        }

        private void TimeoutTrackBar_Scroll(object sender, EventArgs e) => timeoutNumericUpDown.Value = (decimal)(timeoutTrackBar.Value / 10.0);
        private void SecondStageTrackBar_Scroll(object sender, EventArgs e) => secondStageNumericUpDown.Value = (decimal)(secondStageTrackBar.Value / 10.0);
        private void DimPercentageTrackBar_Scroll(object sender, EventArgs e) => dimPercentageNumericUpDown.Value = dimPercentageTrackBar.Value;
        private void AnimationDurationTrackBar_Scroll(object sender, EventArgs e) => animationDurationNumericUpDown.Value = animationDurationTrackBar.Value;

        private void TimeoutNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            timeoutTrackBar.Value = (int)(timeoutNumericUpDown.Value * 10);
            if (secondStageNumericUpDown.Value < timeoutNumericUpDown.Value) secondStageNumericUpDown.Value = timeoutNumericUpDown.Value;
        }

        private void SecondStageNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            secondStageTrackBar.Value = (int)(secondStageNumericUpDown.Value * 10);
            if (timeoutNumericUpDown.Value > secondStageNumericUpDown.Value) timeoutNumericUpDown.Value = secondStageNumericUpDown.Value;
        }

        private void DimPercentageNumericUpDown_ValueChanged(object sender, EventArgs e) => dimPercentageTrackBar.Value = (int)dimPercentageNumericUpDown.Value;
        private void AnimationDurationNumericUpDown_ValueChanged(object sender, EventArgs e) => animationDurationTrackBar.Value = (int)animationDurationNumericUpDown.Value;
    }
}
