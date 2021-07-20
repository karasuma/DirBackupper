using Prism.Mvvm;

namespace DirBackupper.Models
{
	public enum MessageStatus
	{
		Info, Working, Warning, Error
	}

	public class MessageInfo : BindableBase
	{
		private string _message = "Ready.";
		public string Message
		{
			get => _message;
			set => SetProperty( ref _message, value );
		}

		private MessageStatus _status = MessageStatus.Error;
		public MessageStatus Status
		{
			get => _status;
			set => SetProperty( ref _status, value );
		}

		public void ChangeMessage(string msg, MessageStatus status)
		{
			Message = msg;
			Status = status;
		}
	}
}
