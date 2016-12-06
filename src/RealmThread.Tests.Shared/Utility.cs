using System;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;

namespace SushiHangover.Tests
{
	static class Utility
	{
		public const int COUNT = 1;

		public static void DeleteDirectory(string directoryPath)
		{
			if (!Directory.Exists(directoryPath)) return;
			foreach (string directory in Directory.GetDirectories(directoryPath))
			{
				DeleteDirectory(directory);
			}

			try
			{
				Directory.Delete(directoryPath, true);
			}
			catch (IOException)
			{
				Console.Error.WriteLine("***** Failed to clean up!! *****");
				try
				{
					Directory.Delete(directoryPath, true);
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine("***** Failed to clean up!! *****");
					Console.Error.WriteLine(ex);
				}
			}
			catch (UnauthorizedAccessException)
			{
				Console.Error.WriteLine("***** Failed to clean up!! *****");
				try
				{
					Directory.Delete(directoryPath, true);
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine("***** Failed to clean up!! *****");
					Console.Error.WriteLine(ex);
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("***** Failed to clean up!! *****");
				Console.Error.WriteLine(ex.Message);
			}

			// From http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true/329502#329502
			//try 
			//         {
			//             var di = new DirectoryInfo(directoryPath);
			//             var files = di.EnumerateFiles();
			//             var dirs = di.EnumerateDirectories();

			//             foreach (var file in files)
			//             {
			//                 File.SetAttributes(file.FullName, FileAttributes.Normal);
			//                 (new Action(file.Delete)).Retry();
			//             }

			//             foreach (var dir in dirs)
			//             {
			//                 DeleteDirectory(dir.FullName);
			//             }

			//             File.SetAttributes(directoryPath, FileAttributes.Normal);
			//             Directory.Delete(directoryPath, false);
			//         } 
			//         catch (Exception ex) 
			//         {
			//             Console.Error.WriteLine("***** Failed to clean up!! *****");
			//             Console.Error.WriteLine(ex);
			//         }
		}

		public static IDisposable WithEmptyDirectory(out string directoryPath)
		{
			var di = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
			if (di.Exists)
			{
				DeleteDirectory(di.FullName);
			}

			di.Create();

			directoryPath = di.FullName;
			return Disposable.Create(() =>
			{
				DeleteDirectory(di.FullName);
			});
		}

		public static void Retry(this Action block, int retries = 2)
		{
			while (true)
			{
				try
				{
					block();
					return;
				}
				catch (Exception)
				{
					if (retries == 0)
					{
						throw;
					}
					retries--;
					Thread.Sleep(10);
				}
			}
		}
	}
}
