using DirBackupper.Models;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Windows.Media;

namespace DirBackupper.ViewModels
{
	public class MessageViewModel
	{
		private MessageInfo _model = null;

		public ReactiveProperty<MessageStatus> MessageBackground { get; }

		public ReactiveProperty<string> Message { get; }

		public MessageViewModel(MessageInfo model)
		{
			_model = model;
			MessageBackground = _model.ToReactivePropertyAsSynchronized( m => m.Status );
			Message = _model.ToReactivePropertyAsSynchronized( m => m.Message );
		}
	}

	public sealed class MessageViewModelSample : MessageViewModel
	{
		public MessageViewModelSample() : base( new MessageInfo()
		{
			Message = "Ready to execute!",
			Status = MessageStatus.Info
		} )
		{ }
	}
}
