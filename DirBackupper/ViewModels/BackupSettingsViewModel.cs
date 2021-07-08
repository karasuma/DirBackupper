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

		public BackupSettingsViewModel(BackupExecution model)
		{
			_model = model;
			TemporaryPath = _model.ToReactivePropertyAsSynchronized( m => m.TemporaryFilePath );
			AllowOverwrite = _model.ToReactivePropertyAsSynchronized( m => m.AllowOverwrite );
		}
	}

	public sealed class BackupSettingsViewModelSample : BackupSettingsViewModel
	{
		public BackupSettingsViewModelSample() : base(new BackupExecution()
		{
			TemporaryFilePath = @"X:\\Workspace\Backupper\Stockpile\",
			AllowOverwrite = true
		} )
		{ }
	}
}
