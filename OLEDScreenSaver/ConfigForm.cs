using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OLEDScreenSaver
{
    public partial class ConfigForm : Form
    {
        private ScreenSaver screenSaver;
        public ConfigForm(ScreenSaver pScreenSaver)
        {
            try
            {
                InitializeComponent();
                this.Icon = Properties.Resources.Alecive_Flatwoken_Apps_Computer_Screensaver;
                startupCheckbox.Checked = RegistryHelper.LoadStartup();
                screenNameTextbox.Text = RegistryHelper.LoadScreenName();
                dimCheckbox.Checked = RegistryHelper.LoadDimEnabled();

                // Save/Load for TrackBars
                var loadedTimeout = RegistryHelper.LoadTimeout();
                var timeoutTrackValue = (int)(loadedTimeout * 10);
                if (timeoutTrackValue < timeoutTrackBar.Minimum) timeoutTrackValue = timeoutTrackBar.Minimum;
                if (timeoutTrackValue > timeoutTrackBar.Maximum) timeoutTrackValue = timeoutTrackBar.Maximum;
                 timeoutTrackBar.Value = timeoutTrackValue;
                 timeoutNumericUpDown.Value = (decimal)loadedTimeout;
                 timeoutTrackBar.Scroll += TimeoutTrackBar_Scroll;

                // Adjust minimum for second stage to allow 0
                secondStageTrackBar.Minimum = 0;
                var loadedSecondStage = RegistryHelper.LoadSecondStageTimeout();
                 var secondStageTrackValue = (int)(loadedSecondStage * 10);
                if (secondStageTrackValue < secondStageTrackBar.Minimum) secondStageTrackValue = secondStageTrackBar.Minimum;
                if (secondStageTrackValue > secondStageTrackBar.Maximum) secondStageTrackValue = secondStageTrackBar.Maximum;
                 secondStageTrackBar.Value = secondStageTrackValue;
                 secondStageNumericUpDown.Value = (decimal)loadedSecondStage;
                 secondStageTrackBar.Scroll += SecondStageTrackBar_Scroll;
                 
                 // Save/Load for DimPercentage TrackBar
                 var loadedDimPercentage = RegistryHelper.LoadDimPercentage();
                 if (loadedDimPercentage < dimPercentageTrackBar.Minimum) loadedDimPercentage = dimPercentageTrackBar.Minimum;
                 if (loadedDimPercentage > dimPercentageTrackBar.Maximum) loadedDimPercentage = dimPercentageTrackBar.Maximum;
                 dimPercentageTrackBar.Value = loadedDimPercentage;
                 dimPercentageNumericUpDown.Value = loadedDimPercentage;
                 dimPercentageTrackBar.Scroll += DimPercentageTrackBar_Scroll;

                 // Save/Load for AnimationDuration TrackBar
                 var loadedAnimationDuration = RegistryHelper.LoadAnimationDuration();
                 if (loadedAnimationDuration < animationDurationTrackBar.Minimum) loadedAnimationDuration = animationDurationTrackBar.Minimum;
                 if (loadedAnimationDuration > animationDurationTrackBar.Maximum) loadedAnimationDuration = animationDurationTrackBar.Maximum;
                 animationDurationTrackBar.Value = loadedAnimationDuration;
                 animationDurationNumericUpDown.Value = loadedAnimationDuration;
                 animationDurationTrackBar.Scroll += AnimationDurationTrackBar_Scroll;
                 
                 UpdateDimState();
                this.screenSaver = pScreenSaver;

                // Populate screen list
                PopulateScreenList();
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error initializing ConfigForm: {ex.Message}");
                MessageBox.Show($"Error initializing configuration form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void PopulateScreenList()
        {
            try
            {
                screenCheckedListBox.Items.Clear();

                // Get all available screens with optimized query
                var screens = new List<string>();
                try
                {
                    var friendlyNames = ScreenInterrogatory.GetAllMonitorsFriendlyNames().ToList();
                    var allScreens = Screen.AllScreens;
                    
                    for (var i = 0; i < allScreens.Length; i++)
                    {
                        if (i < friendlyNames.Count && !string.IsNullOrEmpty(friendlyNames[i]))
                        {
                            screens.Add(friendlyNames[i]);
                        }
                        else
                        {
                            screens.Add($"Display {allScreens[i].DeviceName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"Error getting friendly names: {ex.Message}");
                    foreach (var screen in Screen.AllScreens)
                    {
                        screens.Add($"Display {screen.DeviceName}");
                    }
                }

                // Add screens to list
                foreach (var screenName in screens)
                {
                    screenCheckedListBox.Items.Add(screenName);
                }

                // Check previously selected screens
                var selectedScreens = RegistryHelper.LoadScreenNames();
                for (var i = 0; i < screenCheckedListBox.Items.Count; i++)
                {
                    var screenName = screenCheckedListBox.Items[i].ToString();
                    if (selectedScreens.Contains(screenName))
                    {
                        screenCheckedListBox.SetItemChecked(i, true);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error in PopulateScreenList: {ex.Message}");
                // Add a fallback entry so the form can still be used
                screenCheckedListBox.Items.Add("Error loading screens - check log");
            }
        }

        private void Label1_Click(object sender, EventArgs e)
        {

        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.No;
            Close();
        }

        private void DimCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            UpdateDimState();
        }

        private void UpdateDimState()
        {
            var dimEnabled = dimCheckbox.Checked;
            timeoutTrackBar.Enabled = dimEnabled;
            timeoutNumericUpDown.Enabled = dimEnabled;
            timeoutLabel.Enabled = dimEnabled;
            dimPercentageTrackBar.Enabled = dimEnabled;
            dimPercentageNumericUpDown.Enabled = dimEnabled;
            dimPercentageLabel.Enabled = dimEnabled;
            animationDurationTrackBar.Enabled = dimEnabled;
            animationDurationNumericUpDown.Enabled = dimEnabled;
            animationDurationLabel.Enabled = dimEnabled;
            
            if (dimEnabled)
            {
                secondStageTimeoutLabel.Text = "Full black out delay (min)";
            }
            else
            {
                secondStageTimeoutLabel.Text = "Black out after (min)";
            }
        }

        private void TimeoutTrackBar_Scroll(object sender, EventArgs e)
        {
            var minutes = (decimal)(timeoutTrackBar.Value / 10.0);
            if (minutes >= timeoutNumericUpDown.Minimum && minutes <= timeoutNumericUpDown.Maximum) 
                timeoutNumericUpDown.Value = minutes;
        }

        private void SecondStageTrackBar_Scroll(object sender, EventArgs e)
        {
            var minutes = (decimal)(secondStageTrackBar.Value / 10.0);
            if (minutes >= secondStageNumericUpDown.Minimum && minutes <= secondStageNumericUpDown.Maximum) 
                secondStageNumericUpDown.Value = minutes;
        }

        private void TimeoutNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            var val = (int)(timeoutNumericUpDown.Value * 10);
            if (val >= timeoutTrackBar.Minimum && val <= timeoutTrackBar.Maximum)
                timeoutTrackBar.Value = val;

            if (secondStageNumericUpDown.Value < timeoutNumericUpDown.Value)
            {
                secondStageNumericUpDown.Value = timeoutNumericUpDown.Value;
            }
        }

        private void SecondStageNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            var val = (int)(secondStageNumericUpDown.Value * 10);
            if (val >= secondStageTrackBar.Minimum && val <= secondStageTrackBar.Maximum)
                secondStageTrackBar.Value = val;

            if (timeoutNumericUpDown.Value > secondStageNumericUpDown.Value)
            {
                timeoutNumericUpDown.Value = secondStageNumericUpDown.Value;
            }
        }

        private void DimPercentageTrackBar_Scroll(object sender, EventArgs e)
        {
            var value = (decimal)(dimPercentageTrackBar.Value);
            if (value >= dimPercentageNumericUpDown.Minimum && value <= dimPercentageNumericUpDown.Maximum) 
                dimPercentageNumericUpDown.Value = value;
        }

        private void AnimationDurationTrackBar_Scroll(object sender, EventArgs e)
        {
            var value = (decimal)(animationDurationTrackBar.Value);
            if (value >= animationDurationNumericUpDown.Minimum && value <= animationDurationNumericUpDown.Maximum) 
                animationDurationNumericUpDown.Value = value;
        }

        private void DimPercentageNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            var val = (int)(dimPercentageNumericUpDown.Value);
            if (val >= dimPercentageTrackBar.Minimum && val <= dimPercentageTrackBar.Maximum)
                dimPercentageTrackBar.Value = val;
        }

        private void AnimationDurationNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            var val = (int)(animationDurationNumericUpDown.Value);
            if (val >= animationDurationTrackBar.Minimum && val <= animationDurationTrackBar.Maximum)
                animationDurationTrackBar.Value = val;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            var timeoutValue = (double)timeoutNumericUpDown.Value;
            var secondStageTimeoutValue = (double)secondStageNumericUpDown.Value;
            var dimPercentageValue = (int)dimPercentageNumericUpDown.Value;
            var animationDurationValue = (int)animationDurationNumericUpDown.Value;

            // Get selected screens
            var selectedScreens = new List<string>();
            foreach (string item in screenCheckedListBox.CheckedItems)
            {
                selectedScreens.Add(item);
            }

            if (selectedScreens.Count == 0)
            {
                var message = "Please select at least one OLED screen.";
                var caption = "No Screens Selected";
                var buttons = MessageBoxButtons.OK;
                MessageBox.Show(message, caption, buttons);
                return;
            }

            var timeoutStr = timeoutValue.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            var secondStageTimeoutStr = secondStageTimeoutValue.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

            if (!RegistryHelper.SaveScreenNames(selectedScreens) ||
                !RegistryHelper.SaveTimeout(timeoutStr) ||
                !RegistryHelper.SaveSecondStageTimeout(secondStageTimeoutStr) ||
                !RegistryHelper.SaveDimEnabled(dimCheckbox.Checked) ||
                !RegistryHelper.SaveDimPercentage(dimPercentageValue.ToString()) ||
                !RegistryHelper.SaveAnimationDuration(animationDurationValue.ToString()))
            {
                return;
            }
            RegistryHelper.SetStartup(startupCheckbox.Checked);

            this.DialogResult = DialogResult.Yes;
            Close();
        }
    }
}
