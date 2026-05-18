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
            tvInfo = new Ephemera.NBagOfUis.TextViewer();
            tbState = new TextBox();
            SuspendLayout();
            // 
            // btnGo1
            // 
            btnGo1.Location = new Point(468, 22);
            btnGo1.Name = "btnGo1";
            btnGo1.Size = new Size(48, 42);
            btnGo1.TabIndex = 2;
            btnGo1.Text = "Go1";
            btnGo1.UseVisualStyleBackColor = true;
            btnGo1.Click += BtnGo1_Click;
            // 
            // btnGo2
            // 
            btnGo2.Location = new Point(468, 70);
            btnGo2.Name = "btnGo2";
            btnGo2.Size = new Size(50, 42);
            btnGo2.TabIndex = 3;
            btnGo2.Text = "Go2";
            btnGo2.UseVisualStyleBackColor = true;
            btnGo2.Click += BtnGo2_Click;
            // 
            // tvInfo
            // 
            tvInfo.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tvInfo.BorderStyle = BorderStyle.FixedSingle;
            tvInfo.Location = new Point(468, 124);
            tvInfo.MatchUseBackground = true;
            tvInfo.MaxText = 50000;
            tvInfo.Name = "tvInfo";
            tvInfo.Prompt = "";
            tvInfo.Size = new Size(652, 351);
            tvInfo.TabIndex = 8;
            tvInfo.WordWrap = true;
            // 
            // tbState
            // 
            tbState.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbState.BorderStyle = BorderStyle.FixedSingle;
            tbState.Location = new Point(599, 22);
            tbState.Multiline = true;
            tbState.Name = "tbState";
            tbState.ReadOnly = true;
            tbState.Size = new Size(521, 90);
            tbState.TabIndex = 9;
            // 
            // TestHost
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1132, 492);
            Controls.Add(tbState);
            Controls.Add(tvInfo);
            Controls.Add(btnGo2);
            Controls.Add(btnGo1);
            Name = "TestHost";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnGo1;
        private Button btnGo2;
        private TextBox tbState;
        private Ephemera.NBagOfUis.TextViewer tvInfo;
    }
}