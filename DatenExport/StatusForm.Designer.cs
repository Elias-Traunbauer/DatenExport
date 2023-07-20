using System;

namespace DatenExport
{
    partial class StatusForm
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
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.lblDescription = new System.Windows.Forms.Label();
            this.buttonClipboard = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(12, 72);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(506, 23);
            this.progressBar.TabIndex = 0;
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.Location = new System.Drawing.Point(12, 101);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(506, 37);
            this.buttonCancel.TabIndex = 1;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.ButtonCancel_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(91, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Current operation:";
            // 
            // lblDescription
            // 
            this.lblDescription.AutoSize = true;
            this.lblDescription.Location = new System.Drawing.Point(12, 32);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(98, 13);
            this.lblDescription.TabIndex = 3;
            this.lblDescription.Text = "Waiting for status...";
            // 
            // buttonClipboard
            // 
            this.buttonClipboard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClipboard.Location = new System.Drawing.Point(364, 29);
            this.buttonClipboard.Name = "buttonClipboard";
            this.buttonClipboard.Size = new System.Drawing.Size(154, 37);
            this.buttonClipboard.TabIndex = 4;
            this.buttonClipboard.Text = "Save export";
            this.buttonClipboard.UseVisualStyleBackColor = true;
            this.buttonClipboard.Click += new System.EventHandler(this.ButtonClipboard_Click);
            // 
            // StatusForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(530, 150);
            this.Controls.Add(this.buttonClipboard);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.progressBar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "StatusForm";
            this.ShowIcon = false;
            this.Text = "Export Status";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.StatusForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.Button buttonClipboard;
    }
}