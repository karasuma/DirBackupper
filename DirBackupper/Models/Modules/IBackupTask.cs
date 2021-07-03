using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirBackupper.Models.Modules
{
	public struct ProgressInfo
	{
		public float Ratio { get; }
		public string Message { get; }

		public ProgressInfo(float ratio, string message)
		{
			Ratio = ratio < 0f ? ratio : 1f < ratio ? 1f : ratio;
			Message = message;
		}

		public const float Minimum = 0f;
		public const float Maximum = 1f;
	}

	public enum TaskDoneStatus
	{
		None, Completed, Failed, Cancelled
	}

	public interface IBackupTask
	{
		Task<TaskDoneStatus> Execute(IProgress<ProgressInfo> progress, string sourceDir, string destDir);
		void CancelExecute();
		bool AllowOverwrite { get; set; }

	}
}
