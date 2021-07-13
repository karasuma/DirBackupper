using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirBackupper.Models.Modules
{
	public struct ProgressInfo
	{
		public float Ratio { get => ProcessingFiles.Value > 0 ? ( (float)ProcessingFiles.Key / (float)ProcessingFiles.Value ) : 0f; }
		public KeyValuePair<int, int> ProcessingFiles { get; }
		public string Message { get; }

		public ProgressInfo(KeyValuePair<int, int> processings, string message)
		{
			ProcessingFiles = processings;
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
