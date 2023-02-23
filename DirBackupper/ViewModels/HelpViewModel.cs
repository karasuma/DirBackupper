using DirBackupper.Utils;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirBackupper.ViewModels
{
	public class HelpViewModel : BindableBase
	{
		public ReactiveProperty<string> Help { get; } = new ReactiveProperty<string>();

		public HelpViewModel()
		{
			var tab = "   ";
			Help.Value = string.Join( Tools.NewLine,
				"How to use",
				"・Tabs",
				$"{tab}・Directory",
				$"{tab}{tab}Specify the directories you want to backup/restore.",
				$"{tab}{tab}ex:",
				$"{tab}{tab}  Backup/restore source: C:\\Target\\Dir",
				$"{tab}{tab}  Destination source   : \\192.168.x.y\\Storage\\External",
				$"{tab}{tab}  Copy files in the directory of 'Dir' into the directory 'External'.",
				$"{tab}・Backup Settings",
				$"{tab}{tab}Configure backup settings.",
				$"{tab}・Schedule Settings",
				$"{tab}{tab}Backup scheduling hasn't developed yet ;(",
				"",
				"・Buttons",
				$"{tab}Backup",
				$"{tab}{tab}Start backup.",
				$"{tab}Restore",
				$"{tab}{tab}Start restore.",
				$"{tab}Abort",
				$"{tab}Abort backup/restore when it's running."

			);
		}
	}

	public sealed class HelpViewModelSample : HelpViewModel
	{
		public HelpViewModelSample() : base()
		{
			Help.Value = "Directory backupper";
		}
	}
}
