namespace TestLib.WorkerService
{
	partial class WorkerService
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
			this.TestLibServiceInstaller = new System.ServiceProcess.ServiceInstaller();
			// 
			// TestLibServiceInstaller
			// 
			this.TestLibServiceInstaller.DelayedAutoStart = true;
			this.TestLibServiceInstaller.Description = "TestLib Codelabs worker service";
			this.TestLibServiceInstaller.DisplayName = "TestLib.Worker";
			this.TestLibServiceInstaller.ServiceName = "TestLib.WorkerService";
			this.TestLibServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
			// 
			// WorkerService
			// 
			this.ServiceName = "TestLib.WorkerService";

		}

		#endregion

		private System.ServiceProcess.ServiceInstaller TestLibServiceInstaller;
	}
}
