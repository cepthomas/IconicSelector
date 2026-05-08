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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TestHost));
            button1 = new Button();
            btnDnD = new Button();
            icsel = new Selector();
            tvInfo = new Ephemera.NBagOfUis.TextViewer();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(12, 12);
            button1.Name = "button1";
            button1.Size = new Size(48, 42);
            button1.TabIndex = 2;
            button1.Text = "!!!";
            button1.UseVisualStyleBackColor = true;
            button1.Click += Button1_Click;
            // 
            // btnDnD
            // 
            btnDnD.Location = new Point(66, 12);
            btnDnD.Name = "btnDnD";
            btnDnD.Size = new Size(50, 42);
            btnDnD.TabIndex = 3;
            btnDnD.Text = "DnD";
            btnDnD.UseVisualStyleBackColor = true;
            btnDnD.Click += BtnDnD_Click;
            // 
            // icsel
            // 
            icsel.AllowDrop = true;
            icsel.AllowExternalDrop = false;
            icsel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            icsel.AutoScroll = true;
            icsel.BorderStyle = BorderStyle.FixedSingle;
            icsel.LeftMouseClick = MouseFunction.Click;
            icsel.Location = new Point(12, 73);
            icsel.Name = "icsel";
            icsel.Size = new Size(358, 382);
            icsel.TabIndex = 4;
            // 
            // tvInfo
            // 
            tvInfo.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            tvInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            tvInfo.Location = new System.Drawing.Point(390, 73);
            tvInfo.MatchUseBackground = true;
            tvInfo.MaxText = 50000;
            tvInfo.Name = "tvInfo";
            tvInfo.Prompt = "";
            tvInfo.Size = new System.Drawing.Size(454, 382);
            tvInfo.TabIndex = 8;
            tvInfo.WordWrap = true;
            // 
            // TestHost
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(856, 472);
            Controls.Add(tvInfo);
            Controls.Add(icsel);
            Controls.Add(btnDnD);
            Controls.Add(button1);
            Name = "TestHost";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private Button button1;
        private Button btnDnD;
        private Selector icsel;
        private Ephemera.NBagOfUis.TextViewer tvInfo;
    }
}