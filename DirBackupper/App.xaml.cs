using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DirBackupper.Utils;

namespace DirBackupper
{
	/// <summary>
	/// App.xaml の相互作用ロジック
	/// </summary>
	public partial class App : Application
	{
		private static Window Window = null;
		private static Mutex Mutex = null;

		private void SafeShutdown(int exitcode, bool forceShutdown = true, bool releaseMutex = false)
		{
			if( Mutex == null )
				return;

			if( releaseMutex )
			{
				try
				{
					Mutex.ReleaseMutex();
				}
				catch( ObjectDisposedException ) { } // 既に解放されているのにReleaseしたという例外なら無視
			}

			Mutex.Close();
			if( forceShutdown )
				this.Shutdown( exitcode );
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			DispatcherUnhandledException += App_DispatcherUnhandledException;

			var isContinueProcess = true;
			Mutex = new Mutex( false, "DirectoryBackupper::_::YakumoKarasuma" );

			if( !Mutex.WaitOne( 9, false ) )
				isContinueProcess = false;

			if( Window == null && isContinueProcess )
			{
				Window = new MainWindow();
				Window.Show();
			}

			if( !isContinueProcess )
				SafeShutdown( 0 );

			base.OnStartup( e );
		}

		protected override void OnExit(ExitEventArgs e)
		{
			if( Window != null )
				Window.Close();
			SafeShutdown( 0, false, true );

			AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
			DispatcherUnhandledException -= App_DispatcherUnhandledException;

			base.OnExit( e );
		}

		private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			try
			{
				var message = string.Join( Tools.NewLine, new[] {
					"Unhandled Exception was thrown on UI thread!",
					"Please check newest log text.",
					Logger.LogFilePath,
					"",
					e.Exception.ToString().GetFirstLine()
				} );
				Logger.Log( e.Exception.ToString(), Logger.LogStates.Fatal );
				MessageBox.Show( message, "Unhandled Exception (on UI thread)", MessageBoxButton.OK, MessageBoxImage.Error );
				SafeShutdown( e.Exception.HResult );
			}
			catch { } // ここで出た例外はどうしようもないので全無視
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var exception = e.ExceptionObject as Exception;

			try
			{
				var message = string.Join( Tools.NewLine, new[] {
					"Unhandled Exception was thrown!",
					"Please check newest log text.",
					Logger.LogFilePath,
					"",
					exception.ToString().GetFirstLine()
				} );
				Logger.Log( exception.ToString(), Logger.LogStates.Fatal );
				MessageBox.Show( message, "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error );
				SafeShutdown( exception.HResult );
			}
			catch { } // ここで出た例外はどうしようもないので全無視
		}
	}
}
