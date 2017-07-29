namespace IgorKL.ACAD3.Model.Drawing.Views
{
    partial class AnchorDeviationsCmdFormEx
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
            this.addOneItem_button = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.maxDiv_numericUpDown = new System.Windows.Forms.NumericUpDown();
            this.getYAxis_button = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.maxDiv_numericUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // addOneItem_button
            // 
            this.addOneItem_button.Location = new System.Drawing.Point(0, 74);
            this.addOneItem_button.Name = "addOneItem_button";
            this.addOneItem_button.Size = new System.Drawing.Size(156, 23);
            this.addOneItem_button.TabIndex = 0;
            this.addOneItem_button.Text = "Вставлять по одному";
            this.addOneItem_button.UseVisualStyleBackColor = true;
            this.addOneItem_button.Click += new System.EventHandler(this.addOneItem_button_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.groupBox2);
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(156, 68);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Допустимые отклонения";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.maxDiv_numericUpDown);
            this.groupBox2.Location = new System.Drawing.Point(6, 19);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(94, 44);
            this.groupBox2.TabIndex = 0;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "В плане, мм";
            // 
            // maxDiv_numericUpDown
            // 
            this.maxDiv_numericUpDown.Enabled = false;
            this.maxDiv_numericUpDown.Location = new System.Drawing.Point(6, 19);
            this.maxDiv_numericUpDown.Name = "maxDiv_numericUpDown";
            this.maxDiv_numericUpDown.Size = new System.Drawing.Size(68, 20);
            this.maxDiv_numericUpDown.TabIndex = 0;
            this.maxDiv_numericUpDown.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // getYAxis_button
            // 
            this.getYAxis_button.Location = new System.Drawing.Point(0, 99);
            this.getYAxis_button.Name = "getYAxis_button";
            this.getYAxis_button.Size = new System.Drawing.Size(156, 23);
            this.getYAxis_button.TabIndex = 2;
            this.getYAxis_button.Text = "Задать направление";
            this.getYAxis_button.UseVisualStyleBackColor = true;
            this.getYAxis_button.Click += new System.EventHandler(this.getYAxis_button_Click);
            // 
            // AnchorDeviationsCmdForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(156, 124);
            this.Controls.Add(this.getYAxis_button);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.addOneItem_button);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(172, 162);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(172, 162);
            this.Name = "AnchorDeviationsCmdForm";
            this.Opacity = 0.8D;
            this.Text = "Анкера";
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.maxDiv_numericUpDown)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button addOneItem_button;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.NumericUpDown maxDiv_numericUpDown;
        private System.Windows.Forms.Button getYAxis_button;
    }
}