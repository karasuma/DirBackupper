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
using System.IO;
using System.Reactive;

namespace DirBackupper.ViewModels
{
	public class MainWindowViewModel : BindableBase, IDisposable
	{
		private BackupperModel _model = new BackupperModel();
		private Progress<Models.Modules.ProgressInfo> _progressInfo = new Progress<Models.Modules.ProgressInfo>();
		private ReactiveProperty<bool> _isCopyWorking = new ReactiveProperty<bool>( false );
		private ReactiveProperty<bool> _isAbortRunning = new ReactiveProperty<bool>(false);

		public SoftwareSettingsViewModel SoftwareSettings { get; protected set; }
		public MessageViewModel Message { get; protected set; }
		public BackupDirSelectionViewModel BackupDir { get; protected set; }
		public BackupSettingsViewModel BackupSettings { get; protected set; }

		public AsyncReactiveCommand BackupExecuteCommand { get; }

		public AsyncReactiveCommand RestoreExecuteCommand { get; }

		public AsyncReactiveCommand AbortExecuteCommand { get; }

		public ReactiveProperty<int> Progress { get; } = new ReactiveProperty<int>( 0 );

		public ReactiveProperty<bool> ProgressWorking { get; } = new ReactiveProperty<bool>( false );

		public ReactiveProperty<string> ProgressMessage { get; } = new ReactiveProperty<string>( "..." );

		public ReactiveProperty<string> ProcessingRatio { get; } = new ReactiveProperty<string>();

		public ReactiveProperty<string> Help { get; } = new ReactiveProperty<string>();

		public ReadOnlyReactiveProperty<bool> IsEditable { get; }

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
				if ( SoftwareSettings.IsDevelopmentMode.Value && SoftwareSettings.ButtonExecutionTesting.Value )
				{
					_isCopyWorking.Value = true;
					ChangeMessage( "Operation working... (TEST)", MessageStatus.Working );
					await _model.BackupExecution.DummyExecute( _progressInfo );
					ChangeMessage( "Operation Complete (TEST)", MessageStatus.Info );
					_isCopyWorking.Value = false;
				}
				else if ( MessageBox.Show( $"Copy files from source to destination.{Tools.NewLine}Are you sure?", "Backup confirm", MessageBoxButton.OKCancel, MessageBoxImage.Information ) == MessageBoxResult.OK )
				{
					_isCopyWorking.Value = true;
					ChangeMessage( "Backup operation working...", MessageStatus.Working, Logger.LogStates.Info );
					var result = await _model.BackupExecution.ExecuteBackup( _progressInfo );
					if ( result == Models.Modules.TaskDoneStatus.Completed )
						ChangeMessage( "Backup operation was successfully completed!", MessageStatus.Info, Logger.LogStates.Info );
					else if ( result == Models.Modules.TaskDoneStatus.Failed )
						ChangeMessage( "Backup operation failed.", MessageStatus.Error, Logger.LogStates.Error );
					_isCopyWorking.Value = false;
				}
			} );

			RestoreExecuteCommand = _isCopyWorking.Select( f => !f ).ToAsyncReactiveCommand();
			RestoreExecuteCommand.Subscribe( async () =>
			{
				if ( SoftwareSettings.IsDevelopmentMode.Value && SoftwareSettings.ButtonExecutionTesting.Value )
				{
					_isCopyWorking.Value = true;
					ChangeMessage( "Operation working... (TEST)", MessageStatus.Working );
					await _model.BackupExecution.DummyExecute( _progressInfo );
					ChangeMessage( "Operation Complete (TEST)", MessageStatus.Info );
					_isCopyWorking.Value = false;
				}
				else if ( MessageBox.Show( $"Copy files from destination to source.{Tools.NewLine}Are you sure?", "Backup confirm", MessageBoxButton.OKCancel, MessageBoxImage.Information ) == MessageBoxResult.OK )
				{
					_isCopyWorking.Value = true;
					ChangeMessage( "Restore operation working...", MessageStatus.Warning, Logger.LogStates.Warn );
					var result = await _model.BackupExecution.ExecuteRestore( _progressInfo );
					if ( result == Models.Modules.TaskDoneStatus.Completed )
						ChangeMessage( "Restore operation was successfully completed!", MessageStatus.Info, Logger.LogStates.Info );
					_isCopyWorking.Value = false;
				}
			} );

			AbortExecuteCommand = _isCopyWorking.CombineLatest( _isAbortRunning, (copying, aborting) => copying && !aborting ).ToAsyncReactiveCommand();
			AbortExecuteCommand.Subscribe( async () =>
			 {
				 if ( MessageBox.Show( "Are you sure do you want to stop backup/restore processing?", "Abort Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning ) == MessageBoxResult.Yes )
				 {
					 _isAbortRunning.Value = true;
					 ChangeMessage( "Backup/Restore operation aborting...", MessageStatus.Warning, Logger.LogStates.Warn );
					 _model.BackupExecution.Abort();
					 while ( _isCopyWorking.Value ) await Task.Yield();
					 ChangeMessage( "Backup/Restore operation was aborted.", MessageStatus.Warning, Logger.LogStates.Warn );
					 _isAbortRunning.Value = false;
				 }
			 } );

			Help.Value = string.Join( Tools.NewLine, new[]
			{
				"Contact: https://twitter.com/yakumo_crow"
			} );

			IsEditable = _isCopyWorking.Select( x => !x ).ToReadOnlyReactiveProperty();

			// Notify to complete initialize
			ChangeSystemSettings( SaveOrLoad.Load );
			ChangeMessage( "Ready.", MessageStatus.Info );
		}

		public enum SaveOrLoad { Save, Load }
		public void ChangeSystemSettings(SaveOrLoad save)
			=> ( save == SaveOrLoad.Save ? _model.SystemSettings.SaveSystemSettings() : _model.SystemSettings.LoadSystemSettings() ).Await( false );
		public void ChangeMessage(string message, MessageStatus status) => _model?.MessageInfo.ChangeMessage( message, status );
		public void ChangeMessage(string message, MessageStatus status, Logger.LogStates state)
		{
			ChangeMessage( message, status );
			var msg = string.Join( Tools.NewLine, new[]
			{
			 message,
			 "Source: " + _model.BackupExecution.SourceDirectoryPath,
			 "Dest  : " + _model.BackupExecution.DestinationFilePath
			} );
			Logger.Log( msg, state );
		}

		public void Dispose()
		{
			_model.BackupExecution.Abort();
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
