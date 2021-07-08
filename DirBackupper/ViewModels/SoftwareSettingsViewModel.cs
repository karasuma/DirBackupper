using DirBackupper.Models;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace DirBackupper.ViewModels
{
	public class SoftwareSettingsViewModel : BindableBase
	{
		private SystemSettings _model = null;

		public ReactiveProperty<string> SetupFilePath { get; }

		public ReactiveProperty<bool> AllowMultipleSetup { get; }

		public ReactiveProperty<bool> IsDevelopmentMode { get; } = new ReactiveProperty<bool>( false );

		public ReactiveProperty<bool> ButtonExecutionTesting { get; } = new ReactiveProperty<bool>( false );

		public SoftwareSettingsViewModel(SystemSettings model)
		{
			_model = model;
			SetupFilePath = _model.ToReactivePropertyAsSynchronized( m => m.SetupFilePath );
			AllowMultipleSetup = _model.ToReactivePropertyAsSynchronized( m => m.AllowMultipleStartup );
		}
	}

	public sealed class SoftwareSettingsViewModelSample : SoftwareSettingsViewModel
	{
		public SoftwareSettingsViewModelSample() : base( new SystemSettings( new BackupExecution() )
		{
			AllowMultipleStartup = true,
			SetupFilePath = @"X:\\Workspace\Backupper\Settings.json"
		} )
		{ }
	}
}
