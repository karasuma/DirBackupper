using Prism.Mvvm;

namespace DirBackupper.Models
{
	public class BackupperModel : BindableBase
	{
		private readonly BackupExecution _backupExecution;
		private readonly SystemSettings _systemSettings;
		private readonly MessageInfo _messageInfo;

		public BackupExecution BackupExecution { get => _backupExecution; }
		public SystemSettings SystemSettings { get => _systemSettings; }
		public MessageInfo MessageInfo { get => _messageInfo; }

		public BackupperModel() : base()
		{
			_backupExecution = new BackupExecution();
			_systemSettings = new SystemSettings( _backupExecution );
			_messageInfo = new MessageInfo();
		}
	}
}
