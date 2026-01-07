using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using DGScopeProfileManager.Services;
using DGScopeProfileManager.Models;

namespace DGScopeProfileManager;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	protected override void OnStartup(StartupEventArgs e)
	{
		var args = System.Environment.GetCommandLineArgs();

		// Headless command to apply current default settings to all profiles
		if (args.Any(a => a.Equals("--apply-defaults-to-all", StringComparison.OrdinalIgnoreCase)) ||
			args.Any(a => a.Equals("/applyall", StringComparison.OrdinalIgnoreCase)))
		{
			try
			{
				var persistence = new SettingsPersistenceService();
				var appSettings = persistence.LoadSettings();

				if (string.IsNullOrWhiteSpace(appSettings.DgScopeFolderPath))
				{
					Console.WriteLine("DGScope folder path not configured in settings.");
					Shutdown(2);
					return;
				}

				var pref = appSettings.DefaultSettings.ToPrefSetSettings();
				var scanner = new FacilityScanner();
				var facilities = scanner.ScanFacilities(appSettings.DgScopeFolderPath);
				int count = 0;
				foreach (var facility in facilities)
				{
					var service = new DgScopeProfileService(facility.Path);
					foreach (var profile in facility.Profiles)
					{
						service.ApplyPrefSetSettings(profile, pref);
						count++;
					}
				}
				Console.WriteLine($"Applied defaults to {count} profiles.");
				Shutdown(0);
				return;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error applying defaults: {ex.Message}");
				Shutdown(1);
				return;
			}
		}

		base.OnStartup(e);
		var main = new MainWindow();
		main.Show();
	}
}

