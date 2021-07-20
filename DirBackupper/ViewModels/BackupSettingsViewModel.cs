using DirBackupper.Models;
using Prism.Mvvm;
using System.Reactive;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace DirBackupper.ViewModels
{
	public class BackupSettingsViewModel : BindableBase
	{
		protected BackupExecution _model = null;

		public ReactiveProperty<string> TemporaryPath { get; }

		public ReactiveProperty<bool> AllowOverwrite { get; }

		public ReactiveProperty<uint> BufferLengthMB { get; }

		public ReactiveProperty<bool> CompressBackup { get; }

		public ReactiveProperty<bool> CompressInDest { get; }

		public ReadOnlyReactiveCollection<string> IgnoreList { get; }

		public ReactiveProperty<string> SelectedIgnore { get; } = new ReactiveProperty<string>();

		public ReactiveProperty<string> IgnoreInputBox { get; } = new ReactiveProperty<string>();

		public ReactiveCommand AddButton { get; }

		public ReactiveCommand RemoveButton { get; }

		public ReactiveCommand SelectTempPath { get; } = new ReactiveCommand();

		public BackupSettingsViewModel(BackupExecution model)
		{
			_model = model;
			TemporaryPath = _model.ToReactivePropertyAsSynchronized( m => m.TemporaryFilePath );
			AllowOverwrite = _model.ToReactivePropertyAsSynchronized( m => m.AllowOverwrite );
			BufferLengthMB = _model.ToReactivePropertyAsSynchronized( m => m.BufferLengthMByte );
			CompressBackup = _model.ToReactivePropertyAsSynchronized( m => m.IsCompress );
			CompressInDest = _model.ToReactivePropertyAsSynchronized( m => m.CompressInDest );

			IgnoreList = _model.IgnoreList.ToReadOnlyReactiveCollection();

			AddButton = IgnoreInputBox.Select( s => !string.IsNullOrEmpty( s ) ).ToReactiveCommand();
			AddButton.WithSubscribe( () => Task.Run( () =>
			{
				if ( !IgnoreList.Contains( IgnoreInputBox.Value ) )
					_model.IgnoreList.Add( IgnoreInputBox.Value );
				IgnoreInputBox.Value = string.Empty;
			} ).ConfigureAwait( false ) );

			RemoveButton = SelectedIgnore.Select( s => !string.IsNullOrEmpty( s ) ).ToReactiveCommand();
			RemoveButton.WithSubscribe( () => Task.Run( () => _model.IgnoreList.Remove( SelectedIgnore.Value ) ).ConfigureAwait( false ) );

			SelectTempPath.Subscribe( () =>
			{
				using ( var dialog = new CommonOpenFileDialog()
				{
					Title = "Choose temporary directory",
					IsFolderPicker = true,
					InitialDirectory = TemporaryPath.Value
				} )
				{
					if ( dialog.ShowDialog() == CommonFileDialogResult.Ok )
						_model.TemporaryFilePath = dialog.FileName;
				}
			} );
		}
	}

	public sealed class BackupSettingsViewModelSample : BackupSettingsViewModel
	{
		public BackupSettingsViewModelSample() : base( new BackupExecution()
		{
			TemporaryFilePath = @"X:\\Workspace\Backupper\Stockpile\",
			AllowOverwrite = true,
			BufferLengthMByte = 256,
			IsCompress = true,
			CompressInDest = true,
		} )
		{
			_model.IgnoreList.AddRange( new[] { "Thumbs.db", "ImportantFile" } );
		}
	}
}
