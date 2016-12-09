using Android.App;
using Android.Content.PM;
using Android.OS;

namespace RealmThread.NugetTest.Droid
{
	[Activity(Label = "RealmThread.NugetTest.Droid", Icon = "@drawable/icon", Theme = "@style/MyTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : Xamarin.Forms.Platform.Android.FormsAppCompatActivity
	{
		protected override void OnCreate(Bundle savedInstanceState)
		{
			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;

			base.OnCreate(savedInstanceState);

			Xamarin.Forms.Forms.Init(this, savedInstanceState);

			LoadApplication(new App());
		}
	}
}
