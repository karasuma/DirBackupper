using DirBackupper.Models;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace DirBackupper.ViewModels
{
	public class BackupSettingsViewModel : BindableBase
	{
		private BackupExecution _model = null;

		public ReactiveProperty<string> TemporaryPath { get; }

		public ReactiveProperty<bool> AllowOverwrite { get; }

		public ReactiveProperty<uint> BufferLengthMB { get; }

		public ReactiveProperty<bool> CompressBackup { get; }

		public ReactiveProperty<bool> CompressInDest { get; }

		public BackupSettingsViewModel(BackupExecution model)
		{
			_model = model;
			TemporaryPath = _model.ToReactivePropertyAsSynchronized( m => m.TemporaryFilePath );
			AllowOverwrite = _model.ToReactivePropertyAsSynchronized( m => m.AllowOverwrite );
			BufferLengthMB = _model.ToReactivePropertyAsSynchronized( m => m.BufferLengthMByte );
			CompressBackup = _model.ToReactivePropertyAsSynchronized( m => m.IsCompress );
			CompressInDest = _model.ToReactivePropertyAsSynchronized( m => m.CompressInDest );
		}
	}

	public sealed class BackupSettingsViewModelSample : BackupSettingsViewModel
	{
		public BackupSettingsViewModelSample() : base(new BackupExecution()
		{
			TemporaryFilePath = @"X:\\Workspace\Backupper\Stockpile\",
			AllowOverwrite = true,
			BufferLengthMByte = 256,
			IsCompress = true,
			CompressInDest = true
		} )
		{ }
	}
}
