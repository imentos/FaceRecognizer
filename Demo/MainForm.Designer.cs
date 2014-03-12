namespace MultiFaceRec
{
    partial class FrmPrincipal
    {
        /// <summary>
        /// Variable del diseñador requerida.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén utilizando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben eliminar; false en caso contrario, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido del método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.trainButton = new System.Windows.Forms.Button();
            this.personToTrainText = new System.Windows.Forms.TextBox();
            this.nameLabel = new System.Windows.Forms.Label();
            this.loggerLabel = new System.Windows.Forms.Label();
            this.indicatorLabel = new System.Windows.Forms.Label();
            this.timeLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // trainButton
            // 
            this.trainButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.trainButton.Location = new System.Drawing.Point(444, 372);
            this.trainButton.Name = "trainButton";
            this.trainButton.Size = new System.Drawing.Size(156, 29);
            this.trainButton.TabIndex = 3;
            this.trainButton.Text = "Train";
            this.trainButton.UseVisualStyleBackColor = true;
            this.trainButton.Click += new System.EventHandler(this.trainButton_Click);
            // 
            // personToTrainText
            // 
            this.personToTrainText.AccessibleDescription = "Enter the name";
            this.personToTrainText.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.personToTrainText.Location = new System.Drawing.Point(242, 372);
            this.personToTrainText.Name = "personToTrainText";
            this.personToTrainText.Size = new System.Drawing.Size(156, 29);
            this.personToTrainText.TabIndex = 7;
            // 
            // nameLabel
            // 
            this.nameLabel.Font = new System.Drawing.Font("Verdana", 72F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nameLabel.ForeColor = System.Drawing.Color.Black;
            this.nameLabel.Location = new System.Drawing.Point(45, 61);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Size = new System.Drawing.Size(752, 154);
            this.nameLabel.TabIndex = 16;
            this.nameLabel.Text = "Nobody";
            this.nameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // loggerLabel
            // 
            this.loggerLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.loggerLabel.Font = new System.Drawing.Font("Times New Roman", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.loggerLabel.ForeColor = System.Drawing.Color.Blue;
            this.loggerLabel.Location = new System.Drawing.Point(242, 234);
            this.loggerLabel.Name = "loggerLabel";
            this.loggerLabel.Size = new System.Drawing.Size(358, 121);
            this.loggerLabel.TabIndex = 17;
            // 
            // indicatorLabel
            // 
            this.indicatorLabel.BackColor = System.Drawing.Color.Red;
            this.indicatorLabel.Location = new System.Drawing.Point(399, 21);
            this.indicatorLabel.Name = "indicatorLabel";
            this.indicatorLabel.Size = new System.Drawing.Size(44, 40);
            this.indicatorLabel.TabIndex = 18;
            // 
            // timeLabel
            // 
            this.timeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.timeLabel.Location = new System.Drawing.Point(625, 236);
            this.timeLabel.Name = "timeLabel";
            this.timeLabel.Size = new System.Drawing.Size(100, 23);
            this.timeLabel.TabIndex = 19;
            // 
            // FrmPrincipal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(837, 436);
            this.Controls.Add(this.timeLabel);
            this.Controls.Add(this.indicatorLabel);
            this.Controls.Add(this.loggerLabel);
            this.Controls.Add(this.personToTrainText);
            this.Controls.Add(this.trainButton);
            this.Controls.Add(this.nameLabel);
            this.Name = "FrmPrincipal";
            this.Text = "Facial Recognizer";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button trainButton;
        private System.Windows.Forms.TextBox personToTrainText;
        private System.Windows.Forms.Label nameLabel;
        private System.Windows.Forms.Label loggerLabel;
        private System.Windows.Forms.Label indicatorLabel;
        private System.Windows.Forms.Label timeLabel;
    }
}

