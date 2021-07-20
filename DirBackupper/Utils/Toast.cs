using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.IO;

namespace DirBackupper.Utils
{
	public static class Toast
	{
		public static void Pop(string caption, params string[] messages)
		{
			new ToastContentBuilder()
				.AddText( caption )
				.AddText( string.Join( Tools.NewLine, messages ) )
				.Show();
		}
	}
}
