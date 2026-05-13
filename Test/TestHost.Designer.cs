using System.Drawing;
using System.Windows.Forms;
using Ephemera.IconicSelector;


namespace Ephemera.IconicSelector.Test
{
    public partial class TestHost
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnGo1 = new Button();
            btnGo2 = new Button();
            icsel = new Selector();
            tvInfo = new Ephemera.NBagOfUis.TextViewer();
            tvState = new Ephemera.NBagOfUis.TextViewer();
            SuspendLayout();
            // 
            // btnGo1
            // 
            btnGo1.Location = new Point(12, 12);
            btnGo1.Name = "btnGo1";
            btnGo1.Size = new Size(48, 42);
            btnGo1.TabIndex = 2;
            btnGo1.Text = "Go1";
            btnGo1.UseVisualStyleBackColor = true;
            btnGo1.Click += BtnGo1_Click;
            // 
            // btnGo2
            // 
            btnGo2.Location = new Point(12, 60);
            btnGo2.Name = "btnGo2";
            btnGo2.Size = new Size(50, 42);
            btnGo2.TabIndex = 3;
            btnGo2.Text = "Go2";
            btnGo2.UseVisualStyleBackColor = true;
            btnGo2.Click += BtnGo2_Click;
            // 
            // icsel
            // 
            icsel.AllowDrop = true;
            icsel.AllowExternalDrop = false;
            icsel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            icsel.AutoScroll = true;
            icsel.DrawFont = new Font("Calibri", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            icsel.ImageSize = new Size(32, 32);
            icsel.IndicatorColor = Color.Purple;
            icsel.LeftMouseClick = MouseFunction.Click;
            icsel.Location = new Point(12, 124);
            icsel.Name = "icsel";
            icsel.Pad = 4;
            icsel.Size = new Size(373, 443);
            icsel.Spacing = 10;
            icsel.TabIndex = 4;
            // 
            // tvInfo
            // 
            tvInfo.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            tvInfo.BorderStyle = BorderStyle.FixedSingle;
            tvInfo.Location = new Point(391, 12);
            tvInfo.MatchUseBackground = true;
            tvInfo.MaxText = 50000;
            tvInfo.Name = "tvInfo";
            tvInfo.Prompt = "";
            tvInfo.Size = new Size(685, 555);
            tvInfo.TabIndex = 8;
            tvInfo.WordWrap = true;
            // 
            // tvState
            // 
            tvState.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tvState.BorderStyle = BorderStyle.FixedSingle;
            tvState.Location = new Point(75, 12);
            tvState.MatchUseBackground = true;
            tvState.MaxText = 50000;
            tvState.Name = "tvState";
            tvState.Prompt = "";
            tvState.Size = new Size(301, 90);
            tvState.TabIndex = 9;
            tvState.WordWrap = true;
            // 
            // TestHost
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1088, 584);
            Controls.Add(tvState);
            Controls.Add(tvInfo);
            Controls.Add(icsel);
            Controls.Add(btnGo2);
            Controls.Add(btnGo1);
            Name = "TestHost";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private Button btnGo1;
        private Button btnGo2;
        private Selector icsel;
        private Ephemera.NBagOfUis.TextViewer tvInfo;
        private NBagOfUis.TextViewer tvState;
    }
}