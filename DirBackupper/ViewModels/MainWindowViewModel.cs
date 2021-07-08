using DirBackupper.Models;
using DirBackupper.Utils;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Dialogs;
using Prism.Interactivity;
using Prism.Services.Dialogs;
using System.Windows;

namespace DirBackupper.ViewModels
{
	public class MainWindowViewModel : BindableBase, IDisposable
	{
		private BackupperModel _model = new BackupperModel();
		private Progress<Models.Modules.ProgressInfo> _progressInfo = new Progress<Models.Modules.ProgressInfo>();
		private ReactiveProperty<bool> _isCopyWorking = new ReactiveProperty<bool>( false );

		public SoftwareSettingsViewModel SoftwareSettings { get; protected set; }
		public MessageViewModel Message { get; protected set; }
		public BackupDirSelectionViewModel BackupDir { get; protected set; }
		public BackupSettingsViewModel BackupSettings { get; protected set; }

		public AsyncReactiveCommand BackupExecuteCommand { get; }

		public AsyncReactiveCommand RestoreExecuteCommand { get; }

		public ReactiveCommand AbortExecuteCommand { get; }

		public ReactiveProperty<int> Progress { get; } = new ReactiveProperty<int>( 0 );

		public ReactiveProperty<bool> ProgressWorking { get; } = new ReactiveProperty<bool>( false );

		public ReactiveProperty<string> ProgressMessage { get; } = new ReactiveProperty<string>( "..." );

		public ReactiveProperty<string> ProcessingRatio { get; } = new ReactiveProperty<string>();

		public ReactiveProperty<string> Help { get; } = new ReactiveProperty<string>();

		public MainWindowViewModel()
		{
			SoftwareSettings = new SoftwareSettingsViewModel( _model.SystemSettings );
			Message = new MessageViewModel( _model.MessageInfo );
			BackupDir = new BackupDirSelectionViewModel( _model.BackupExecution );
			BackupSettings = new BackupSettingsViewModel( _model.BackupExecution );

			Observable.FromEventPattern<Models.Modules.ProgressInfo>( h => _progressInfo.ProgressChanged += h, h => _progressInfo.ProgressChanged -= h )
				.Subscribe( info =>
				 {
					 var p = info.EventArgs;
					 Progress.Value = (int)Math.Floor( p.Ratio * 100f );
					 ProgressMessage.Value = p.Message;
					 ProcessingRatio.Value = p.Ratio > 0.001f ? $"{p.ProcessingFiles.Key} / {p.ProcessingFiles.Value}" : string.Empty;
				 } );

			BackupExecuteCommand = _isCopyWorking.Select( f => !f ).ToAsyncReactiveCommand();
			BackupExecuteCommand.Subscribe( async () =>
			 {
				 MessageBox.Show( "Backup is currently disable.", "Information", MessageBoxButton.OK, MessageBoxImage.Information );
				 return;
				 await _model.BackupExecution.ExecuteBackup( _progressInfo );
			 } );

			RestoreExecuteCommand = _isCopyWorking.Select( f => !f ).ToAsyncReactiveCommand();
			RestoreExecuteCommand.Subscribe( async () =>
			{
				MessageBox.Show( "Backup is currently disable.", "Information", MessageBoxButton.OK, MessageBoxImage.Information );
				return;
				var result = await _model.BackupExecution.ExecuteRestore( _progressInfo );
			} );

			AbortExecuteCommand = _isCopyWorking.ToReactiveCommand();
			AbortExecuteCommand.Subscribe( () =>
			 {
				 if ( MessageBox.Show( "Are you sure do you want to stop backup/restore processing?", "Abort Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning ) == MessageBoxResult.Yes )
				 {
					 _model.BackupExecution.Abort();
				 }
			 } );

			Help.Value = string.Join( Tools.NewLine, new[]
			{
				"Contact: https://twitter.com/yakumo_crow"
			} );

			// Notify to complete initialize
			ChangeSystemSettings( SaveOrLoad.Load );
			ChangeMessage( "Ready.", MessageStatus.Info );
		}

		public enum SaveOrLoad { Save, Load }
		public void ChangeSystemSettings(SaveOrLoad save)
			=> ( save == SaveOrLoad.Save ? _model.SystemSettings.SaveSystemSettings() : _model.SystemSettings.LoadSystemSettings() ).Await( false );
		public void ChangeMessage(string message, MessageStatus status) => _model?.MessageInfo.ChangeMessage( message, status );

		public void Dispose()
		{
			ChangeSystemSettings( SaveOrLoad.Save );
		}
	}

	public sealed class MainWindowViewModelSample : MainWindowViewModel
	{
		public MainWindowViewModelSample() : base()
		{
			SoftwareSettings = new SoftwareSettingsViewModelSample();
			Message = new MessageViewModelSample();
			BackupDir = new BackupDirSelectionViewModelSample();
			BackupSettings = new BackupSettingsViewModelSample();
		}
	}
}
