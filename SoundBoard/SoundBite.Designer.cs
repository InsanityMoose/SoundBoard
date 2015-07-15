namespace SoundBoard
{
    partial class SoundBite
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelFile = new System.Windows.Forms.Label();
            this.buttonPlay = new System.Windows.Forms.Button();
            this.buttonHotKey = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // labelFile
            // 
            this.labelFile.AllowDrop = true;
            this.labelFile.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.labelFile.Dock = System.Windows.Forms.DockStyle.Left;
            this.labelFile.Location = new System.Drawing.Point(0, 0);
            this.labelFile.Name = "labelFile";
            this.labelFile.Size = new System.Drawing.Size(167, 50);
            this.labelFile.TabIndex = 0;
            this.labelFile.Text = "Drop file here...";
            this.labelFile.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelFile.DragDrop += new System.Windows.Forms.DragEventHandler(this.labelFile_DragDrop);
            this.labelFile.DragEnter += new System.Windows.Forms.DragEventHandler(this.labelFile_DragEnter);
            // 
            // buttonPlay
            // 
            this.buttonPlay.Dock = System.Windows.Forms.DockStyle.Right;
            this.buttonPlay.Location = new System.Drawing.Point(223, 0);
            this.buttonPlay.Name = "buttonPlay";
            this.buttonPlay.Size = new System.Drawing.Size(61, 50);
            this.buttonPlay.TabIndex = 1;
            this.buttonPlay.Text = "Play";
            this.buttonPlay.UseVisualStyleBackColor = true;
            this.buttonPlay.Click += new System.EventHandler(this.buttonPlay_Click);
            // 
            // buttonHotKey
            // 
            this.buttonHotKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonHotKey.Location = new System.Drawing.Point(167, 0);
            this.buttonHotKey.Name = "buttonHotKey";
            this.buttonHotKey.Size = new System.Drawing.Size(56, 50);
            this.buttonHotKey.TabIndex = 2;
            this.buttonHotKey.Text = "Hotkey";
            this.buttonHotKey.UseVisualStyleBackColor = true;
            this.buttonHotKey.Click += new System.EventHandler(this.buttonHotKey_Click);
            // 
            // SoundBite
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.buttonHotKey);
            this.Controls.Add(this.buttonPlay);
            this.Controls.Add(this.labelFile);
            this.Name = "SoundBite";
            this.Size = new System.Drawing.Size(284, 50);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label labelFile;
        private System.Windows.Forms.Button buttonPlay;
        private System.Windows.Forms.Button buttonHotKey;
    }
}
