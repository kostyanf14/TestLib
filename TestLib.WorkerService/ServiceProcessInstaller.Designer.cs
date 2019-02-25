namespace TestLib.WorkerService
{
	partial class ServiceProcessInstaller
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
			this.TestLibServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
			this.TestLibServiceInstaller = new System.ServiceProcess.ServiceInstaller();
			// 
			// TestLibServiceProcessInstaller
			// 
			this.TestLibServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
			this.TestLibServiceProcessInstaller.Password = null;
			this.TestLibServiceProcessInstaller.Username = null;
			// 
			// TestLibServiceInstaller
			// 
			this.TestLibServiceInstaller.DelayedAutoStart = true;
			this.TestLibServiceInstaller.Description = "TestLib Worker Service";
			this.TestLibServiceInstaller.DisplayName = "TestLib.Worker";
			this.TestLibServiceInstaller.ServiceName = "TestLib.WorkerService";
			this.TestLibServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
			// 
			// ServiceProcessInstaller
			// 
			this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.TestLibServiceProcessInstaller,
            this.TestLibServiceInstaller});

		}

		#endregion

		private System.ServiceProcess.ServiceProcessInstaller TestLibServiceProcessInstaller;
		private System.ServiceProcess.ServiceInstaller TestLibServiceInstaller;
	}
}