using DirBackupper.Models;
using Microsoft.WindowsAPICodePack.Dialogs;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;

namespace DirBackupper.ViewModels
{
	public class BackupDirSelectionViewModel : BindableBase
	{
		private BackupExecution _model = null;

		public ReactiveProperty<string> SourcePath { get; }

		public ReactiveCommand SelectSourceDirectory { get; } = new ReactiveCommand();

		public ReactiveProperty<string> DestPath { get; }

		public ReactiveCommand SelectDestinationDirectory { get; } = new ReactiveCommand();

		public BackupDirSelectionViewModel(BackupExecution model)
		{
			_model = model;
			SourcePath = _model.ToReactivePropertyAsSynchronized( m => m.SourceDirectoryPath );
			SelectSourceDirectory.Subscribe( _ =>
			 {
				 using ( var dialog = new CommonOpenFileDialog()
				 {
					 Title = "Choose backup source directory",
					 IsFolderPicker = true,
					 InitialDirectory = SourcePath.Value
				 } )
				 {
					 if ( dialog.ShowDialog() == CommonFileDialogResult.Ok )
						 _model.SourceDirectoryPath = dialog.FileName;
				 }
			 } );
			DestPath = _model.ToReactivePropertyAsSynchronized( m => m.DestinationFilePath );
			SelectDestinationDirectory.Subscribe( _ =>
			{
				using ( var dialog = new CommonOpenFileDialog()
				{
					Title = "Choose backup destination directory",
					IsFolderPicker = true,
					InitialDirectory = DestPath.Value
				} )
				{
					if ( dialog.ShowDialog() == CommonFileDialogResult.Ok )
						_model.DestinationFilePath = dialog.FileName;
				}
			} );
		}
	}

	public sealed class BackupDirSelectionViewModelSample : BackupDirSelectionViewModel
	{
		public BackupDirSelectionViewModelSample() : base(new BackupExecution()
		{
			SourceDirectoryPath = @"C:\\Users\Xiangling\Recipes\",
			DestinationFilePath = @"\\192.168.10.2\Storage\Xiangling\Recipes\"
		} )
		{ }
	}
}
