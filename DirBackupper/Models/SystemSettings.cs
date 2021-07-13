using Prism.Mvvm;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DirBackupper.Models
{
	public class SystemSettings : BindableBase
	{
		private bool _allowMultipleStartup = false;
		public bool AllowMultipleStartup
		{
			get => _allowMultipleStartup;
			set => SetProperty( ref _allowMultipleStartup, value );
		}

		private static string _setupFilePath = Path.GetFullPath( @".\\Setting.json" );
		public string SetupFilePath
		{
			get => _setupFilePath;
			set => SetProperty( ref _setupFilePath, value );
		}

		[Serializable]
		public class SystemSettingsPOCOs
		{
			public string SourceFilePath { get; set; }
			public string DestinationFilePath { get; set; }
			public string TemporaryFilePath { get; set; }
			public bool AllowOverwrite { get; set; }
			public uint BufferSize { get; set; }
			public bool IsCompress { get; set; }
		}

		private BackupExecution _backupExecution = null;

		public SystemSettings(BackupExecution backup) : base()
		{
			_backupExecution = backup;
		}

		public async Task SaveSystemSettings()
		{
			var poco = new SystemSettingsPOCOs()
			{
				SourceFilePath = _backupExecution.SourceDirectoryPath,
				DestinationFilePath = _backupExecution.DestinationFilePath,
				TemporaryFilePath = _backupExecution.TemporaryFilePath,
				AllowOverwrite = _backupExecution.AllowOverwrite,
				BufferSize = _backupExecution.BufferLengthMByte,
				IsCompress = _backupExecution.IsCompress
			};

			using ( var writer = new FileStream( SetupFilePath, FileMode.Create, FileAccess.Write ) )
				await JsonSerializer.SerializeAsync( writer, poco );
		}

		public async Task LoadSystemSettings()
		{
			var poco = default( SystemSettingsPOCOs );
			using ( var reader = new FileStream( SetupFilePath, FileMode.Open, FileAccess.Read ) )
				poco = await JsonSerializer.DeserializeAsync<SystemSettingsPOCOs>( reader );

			_backupExecution.SourceDirectoryPath = poco.SourceFilePath;
			_backupExecution.DestinationFilePath = poco.DestinationFilePath;
			_backupExecution.TemporaryFilePath = poco.TemporaryFilePath;
			_backupExecution.AllowOverwrite = poco.AllowOverwrite;
			_backupExecution.BufferLengthMByte = poco.BufferSize;
			_backupExecution.IsCompress = poco.IsCompress;
		}
	}
}
