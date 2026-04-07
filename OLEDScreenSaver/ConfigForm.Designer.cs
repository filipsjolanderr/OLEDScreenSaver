namespace OLEDScreenSaver
{
    partial class ConfigForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.timeoutTrackBar = new System.Windows.Forms.TrackBar();
            this.timeoutNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.secondStageTrackBar = new System.Windows.Forms.TrackBar();
            this.secondStageNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.dimPercentageTrackBar = new System.Windows.Forms.TrackBar();
            this.dimPercentageNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.dimPercentageLabel = new System.Windows.Forms.Label();
            this.animationDurationTrackBar = new System.Windows.Forms.TrackBar();
            this.animationDurationNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.animationDurationLabel = new System.Windows.Forms.Label();
            this.dimCheckbox = new System.Windows.Forms.CheckBox();
            this.groupBoxGeneral = new System.Windows.Forms.GroupBox();
            this.groupBoxDimming = new System.Windows.Forms.GroupBox();
            this.groupBoxScreens = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.timeoutTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.secondStageTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.timeoutNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.secondStageNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dimPercentageTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dimPercentageNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.animationDurationTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.animationDurationNumericUpDown)).BeginInit();
            this.groupBoxGeneral.SuspendLayout();
            this.groupBoxDimming.SuspendLayout();
            this.groupBoxScreens.SuspendLayout();
            this.cancelButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.screenNameTextbox = new System.Windows.Forms.TextBox();
            this.startupCheckbox = new System.Windows.Forms.CheckBox();
            this.timeoutLabel = new System.Windows.Forms.Label();
            this.secondStageTimeoutLabel = new System.Windows.Forms.Label();
            this.screenLabel = new System.Windows.Forms.Label();
            this.screenCheckedListBox = new System.Windows.Forms.CheckedListBox();
            this.SuspendLayout();
            // 
            // groupBoxGeneral
            // 
            this.groupBoxGeneral.Controls.Add(this.dimCheckbox);
            this.groupBoxGeneral.Controls.Add(this.startupCheckbox);
            this.groupBoxGeneral.Location = new System.Drawing.Point(12, 12);
            this.groupBoxGeneral.Name = "groupBoxGeneral";
            this.groupBoxGeneral.Size = new System.Drawing.Size(700, 90);
            this.groupBoxGeneral.TabIndex = 0;
            this.groupBoxGeneral.TabStop = false;
            this.groupBoxGeneral.Text = "General Settings";
            // 
            // startupCheckbox
            // 
            this.startupCheckbox.AutoSize = true;
            this.startupCheckbox.Location = new System.Drawing.Point(16, 26);
            this.startupCheckbox.Name = "startupCheckbox";
            this.startupCheckbox.Size = new System.Drawing.Size(188, 24);
            this.startupCheckbox.TabIndex = 7;
            this.startupCheckbox.Text = "Launch with Windows";
            this.startupCheckbox.UseVisualStyleBackColor = true;
            // 
            // dimCheckbox
            // 
            this.dimCheckbox.AutoSize = true;
            this.dimCheckbox.Location = new System.Drawing.Point(16, 56);
            this.dimCheckbox.Name = "dimCheckbox";
            this.dimCheckbox.Size = new System.Drawing.Size(200, 24);
            this.dimCheckbox.TabIndex = 20;
            this.dimCheckbox.Text = "Enable Dimming Stage";
            this.dimCheckbox.UseVisualStyleBackColor = true;
            this.dimCheckbox.CheckedChanged += new System.EventHandler(this.DimCheckbox_CheckedChanged);
            // 
            // groupBoxDimming
            // 
            this.groupBoxDimming.Controls.Add(this.timeoutLabel);
            this.groupBoxDimming.Controls.Add(this.timeoutTrackBar);
            this.groupBoxDimming.Controls.Add(this.timeoutNumericUpDown);
            this.groupBoxDimming.Controls.Add(this.secondStageTimeoutLabel);
            this.groupBoxDimming.Controls.Add(this.secondStageTrackBar);
            this.groupBoxDimming.Controls.Add(this.secondStageNumericUpDown);
            this.groupBoxDimming.Controls.Add(this.dimPercentageLabel);
            this.groupBoxDimming.Controls.Add(this.dimPercentageTrackBar);
            this.groupBoxDimming.Controls.Add(this.dimPercentageNumericUpDown);
            this.groupBoxDimming.Controls.Add(this.animationDurationLabel);
            this.groupBoxDimming.Controls.Add(this.animationDurationTrackBar);
            this.groupBoxDimming.Controls.Add(this.animationDurationNumericUpDown);
            this.groupBoxDimming.Location = new System.Drawing.Point(12, 108);
            this.groupBoxDimming.Name = "groupBoxDimming";
            this.groupBoxDimming.Size = new System.Drawing.Size(700, 320);
            this.groupBoxDimming.TabIndex = 1;
            this.groupBoxDimming.TabStop = false;
            this.groupBoxDimming.Text = "Dimming Behavior";
            // 
            // timeoutLabel
            // 
            this.timeoutLabel.AutoSize = true;
            this.timeoutLabel.Location = new System.Drawing.Point(16, 34);
            this.timeoutLabel.Name = "timeoutLabel";
            this.timeoutLabel.Size = new System.Drawing.Size(110, 20);
            this.timeoutLabel.TabIndex = 5;
            this.timeoutLabel.Text = "Dim after (min)";
            // 
            // timeoutTrackBar
            // 
            this.timeoutTrackBar.Location = new System.Drawing.Point(260, 30);
            this.timeoutTrackBar.Name = "timeoutTrackBar";
            this.timeoutTrackBar.Size = new System.Drawing.Size(300, 45);
            this.timeoutTrackBar.TabIndex = 0;
            this.timeoutTrackBar.Minimum = 1;
            this.timeoutTrackBar.Maximum = 600;
            this.timeoutTrackBar.TickFrequency = 10;
            this.timeoutTrackBar.SmallChange = 2;
            this.timeoutTrackBar.LargeChange = 10;
            this.timeoutTrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.timeoutTrackBar.Scroll += new System.EventHandler(this.TimeoutTrackBar_Scroll);
            // 
            // timeoutNumericUpDown
            // 
            this.timeoutNumericUpDown.Location = new System.Drawing.Point(580, 30);
            this.timeoutNumericUpDown.Name = "timeoutNumericUpDown";
            this.timeoutNumericUpDown.Size = new System.Drawing.Size(90, 26);
            this.timeoutNumericUpDown.TabIndex = 12;
            this.timeoutNumericUpDown.DecimalPlaces = 1;
            this.timeoutNumericUpDown.Increment = 0.2m;
            this.timeoutNumericUpDown.Maximum = 60.0m;
            this.timeoutNumericUpDown.Minimum = 0.1m;
            this.timeoutNumericUpDown.Value = 5.0m;
            this.timeoutNumericUpDown.ValueChanged += new System.EventHandler(this.TimeoutNumericUpDown_ValueChanged);
            // 
            // secondStageTimeoutLabel
            // 
            this.secondStageTimeoutLabel.AutoSize = true;
            this.secondStageTimeoutLabel.Location = new System.Drawing.Point(16, 104);
            this.secondStageTimeoutLabel.Name = "secondStageTimeoutLabel";
            this.secondStageTimeoutLabel.Size = new System.Drawing.Size(141, 20);
            this.secondStageTimeoutLabel.TabIndex = 11;
            this.secondStageTimeoutLabel.Text = "Full blackout delay";
            // 
            // secondStageTrackBar
            // 
            this.secondStageTrackBar.Location = new System.Drawing.Point(260, 100);
            this.secondStageTrackBar.Name = "secondStageTrackBar";
            this.secondStageTrackBar.Size = new System.Drawing.Size(300, 45);
            this.secondStageTrackBar.TabIndex = 1;
            this.secondStageTrackBar.Minimum = 0;
            this.secondStageTrackBar.Maximum = 600;
            this.secondStageTrackBar.TickFrequency = 10;
            this.secondStageTrackBar.SmallChange = 2;
            this.secondStageTrackBar.LargeChange = 10;
            this.secondStageTrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.secondStageTrackBar.Scroll += new System.EventHandler(this.SecondStageTrackBar_Scroll);
            // 
            // secondStageNumericUpDown
            // 
            this.secondStageNumericUpDown.Location = new System.Drawing.Point(580, 100);
            this.secondStageNumericUpDown.Name = "secondStageNumericUpDown";
            this.secondStageNumericUpDown.Size = new System.Drawing.Size(90, 26);
            this.secondStageNumericUpDown.TabIndex = 13;
            this.secondStageNumericUpDown.DecimalPlaces = 1;
            this.secondStageNumericUpDown.Increment = 0.2m;
            this.secondStageNumericUpDown.Maximum = 60.0m;
            this.secondStageNumericUpDown.Minimum = 0.0m;
            this.secondStageNumericUpDown.Value = 1.0m;
            this.secondStageNumericUpDown.ValueChanged += new System.EventHandler(this.SecondStageNumericUpDown_ValueChanged);
            // 
            // dimPercentageLabel
            // 
            this.dimPercentageLabel.AutoSize = true;
            this.dimPercentageLabel.Location = new System.Drawing.Point(16, 174);
            this.dimPercentageLabel.Name = "dimPercentageLabel";
            this.dimPercentageLabel.Size = new System.Drawing.Size(125, 20);
            this.dimPercentageLabel.TabIndex = 22;
            this.dimPercentageLabel.Text = "Dim Level (%)";
            // 
            // dimPercentageTrackBar
            // 
            this.dimPercentageTrackBar.Location = new System.Drawing.Point(260, 170);
            this.dimPercentageTrackBar.Name = "dimPercentageTrackBar";
            this.dimPercentageTrackBar.Size = new System.Drawing.Size(300, 45);
            this.dimPercentageTrackBar.TabIndex = 2;
            this.dimPercentageTrackBar.Minimum = 10;
            this.dimPercentageTrackBar.Maximum = 100;
            this.dimPercentageTrackBar.TickFrequency = 2;
            this.dimPercentageTrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.dimPercentageTrackBar.Scroll += new System.EventHandler(this.DimPercentageTrackBar_Scroll);
            // 
            // dimPercentageNumericUpDown
            // 
            this.dimPercentageNumericUpDown.Location = new System.Drawing.Point(580, 170);
            this.dimPercentageNumericUpDown.Name = "dimPercentageNumericUpDown";
            this.dimPercentageNumericUpDown.Size = new System.Drawing.Size(90, 26);
            this.dimPercentageNumericUpDown.TabIndex = 14;
            this.dimPercentageNumericUpDown.Maximum = 100m;
            this.dimPercentageNumericUpDown.Minimum = 10m;
            this.dimPercentageNumericUpDown.Value = 50m;
            this.dimPercentageNumericUpDown.ValueChanged += new System.EventHandler(this.DimPercentageNumericUpDown_ValueChanged);
            // 
            // animationDurationLabel
            // 
            this.animationDurationLabel.AutoSize = true;
            this.animationDurationLabel.Location = new System.Drawing.Point(16, 244);
            this.animationDurationLabel.Name = "animationDurationLabel";
            this.animationDurationLabel.Size = new System.Drawing.Size(185, 20);
            this.animationDurationLabel.TabIndex = 23;
            this.animationDurationLabel.Text = "Animation Duration (ms)";
            // 
            // animationDurationTrackBar
            // 
            this.animationDurationTrackBar.Location = new System.Drawing.Point(260, 240);
            this.animationDurationTrackBar.Name = "animationDurationTrackBar";
            this.animationDurationTrackBar.Size = new System.Drawing.Size(300, 45);
            this.animationDurationTrackBar.TabIndex = 3;
            this.animationDurationTrackBar.Minimum = 0;
            this.animationDurationTrackBar.Maximum = 5000;
            this.animationDurationTrackBar.TickFrequency = 100;
            this.animationDurationTrackBar.SmallChange = 100;
            this.animationDurationTrackBar.LargeChange = 500;
            this.animationDurationTrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.animationDurationTrackBar.Scroll += new System.EventHandler(this.AnimationDurationTrackBar_Scroll);
            // 
            // animationDurationNumericUpDown
            // 
            this.animationDurationNumericUpDown.Location = new System.Drawing.Point(580, 240);
            this.animationDurationNumericUpDown.Name = "animationDurationNumericUpDown";
            this.animationDurationNumericUpDown.Size = new System.Drawing.Size(90, 26);
            this.animationDurationNumericUpDown.TabIndex = 15;
            this.animationDurationNumericUpDown.Maximum = 5000m;
            this.animationDurationNumericUpDown.Minimum = 0m;
            this.animationDurationNumericUpDown.Increment = 100m;
            this.animationDurationNumericUpDown.Value = 1000m;
            this.animationDurationNumericUpDown.ValueChanged += new System.EventHandler(this.AnimationDurationNumericUpDown_ValueChanged);
            // 
            // groupBoxScreens
            // 
            this.groupBoxScreens.Controls.Add(this.screenCheckedListBox);
            this.groupBoxScreens.Location = new System.Drawing.Point(12, 440);
            this.groupBoxScreens.Name = "groupBoxScreens";
            this.groupBoxScreens.Size = new System.Drawing.Size(700, 140);
            this.groupBoxScreens.TabIndex = 2;
            this.groupBoxScreens.TabStop = false;
            this.groupBoxScreens.Text = "OLED screens to dim";
            // 
            // screenCheckedListBox
            // 
            this.screenCheckedListBox.FormattingEnabled = true;
            this.screenCheckedListBox.Location = new System.Drawing.Point(16, 26);
            this.screenCheckedListBox.Name = "screenCheckedListBox";
            this.screenCheckedListBox.Size = new System.Drawing.Size(650, 100);
            this.screenCheckedListBox.TabIndex = 4;
            // 
            // screenNameTextbox
            // 
            this.screenNameTextbox.Enabled = false;
            this.screenNameTextbox.Location = new System.Drawing.Point(12, 530);
            this.screenNameTextbox.Name = "screenNameTextbox";
            this.screenNameTextbox.Size = new System.Drawing.Size(10, 26);
            this.screenNameTextbox.TabIndex = 3;
            this.screenNameTextbox.Visible = false;
            // 
            // screenLabel
            // 
            this.screenLabel.AutoSize = true;
            this.screenLabel.Location = new System.Drawing.Point(28, 530);
            this.screenLabel.Name = "screenLabel";
            this.screenLabel.Size = new System.Drawing.Size(104, 20);
            this.screenLabel.TabIndex = 6;
            this.screenLabel.Text = "Screen name";
            this.screenLabel.Visible = false;
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(400, 600);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(130, 50);
            this.cancelButton.TabIndex = 5;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // saveButton
            // 
            this.saveButton.Location = new System.Drawing.Point(540, 600);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(130, 50);
            this.saveButton.TabIndex = 6;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.SaveButton_Click);
            // 
            // ConfigForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(730, 670);
            this.MinimumSize = new System.Drawing.Size(740, 710);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Controls.Add(this.groupBoxGeneral);
            this.Controls.Add(this.groupBoxDimming);
            this.Controls.Add(this.groupBoxScreens);
            this.Controls.Add(this.screenLabel);
            this.Controls.Add(this.screenNameTextbox);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.cancelButton);
            this.Name = "ConfigForm";
            this.Text = "Config";
            ((System.ComponentModel.ISupportInitialize)(this.timeoutTrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.secondStageTrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.timeoutNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.secondStageNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dimPercentageTrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dimPercentageNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.animationDurationTrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.animationDurationNumericUpDown)).EndInit();
            this.groupBoxGeneral.ResumeLayout(false);
            this.groupBoxGeneral.PerformLayout();
            this.groupBoxDimming.ResumeLayout(false);
            this.groupBoxDimming.PerformLayout();
            this.groupBoxScreens.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TrackBar timeoutTrackBar;
        private System.Windows.Forms.NumericUpDown timeoutNumericUpDown;
        private System.Windows.Forms.TrackBar secondStageTrackBar;
        private System.Windows.Forms.NumericUpDown secondStageNumericUpDown;
        private System.Windows.Forms.TrackBar dimPercentageTrackBar;
        private System.Windows.Forms.NumericUpDown dimPercentageNumericUpDown;
        private System.Windows.Forms.Label dimPercentageLabel;
        private System.Windows.Forms.TrackBar animationDurationTrackBar;
        private System.Windows.Forms.NumericUpDown animationDurationNumericUpDown;
        private System.Windows.Forms.Label animationDurationLabel;
        private System.Windows.Forms.CheckBox dimCheckbox;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.TextBox screenNameTextbox;
        private System.Windows.Forms.CheckBox startupCheckbox;
        private System.Windows.Forms.Label timeoutLabel;
        private System.Windows.Forms.Label secondStageTimeoutLabel;
        private System.Windows.Forms.Label screenLabel;
        private System.Windows.Forms.CheckedListBox screenCheckedListBox;
        private System.Windows.Forms.GroupBox groupBoxGeneral;
        private System.Windows.Forms.GroupBox groupBoxDimming;
        private System.Windows.Forms.GroupBox groupBoxScreens;
    }
}
