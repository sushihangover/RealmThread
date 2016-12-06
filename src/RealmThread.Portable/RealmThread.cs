using System;
using System.Threading.Tasks;

namespace SushiHangover
{
	/// <summary>
	/// A Realm Action Pump
	/// </summary>
	public class RealmThread : IDisposable
	{
		int ManagedThreadId
		{
			get 
			{ 
				PCLHelpers.ThrowProxyShouldNeverBeUsed();
				return int.MinValue;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SushiHangover.RealmThread"/> class.
		/// </summary>
		/// <param name="realm">Realm.</param>
		public RealmThread(Realms.Realm realm)
		{
			PCLHelpers.ThrowProxyShouldNeverBeUsed();
		}

		/// <summary>
		/// Invokes a "Fire & Forget" Action on an independent Realm thread.
		/// </summary>
		/// <param name="action">Action.</param>
		public void BeginInvoke(Action<Realms.Realm> action)
		{
			PCLHelpers.ThrowProxyShouldNeverBeUsed();
		}

		/// <summary>
		/// Invoke the specified action on an independent Realm thread.
		/// </summary>
		/// <param name="action">Action.</param>
		public void Invoke(Action<Realms.Realm> action)
		{
			PCLHelpers.ThrowProxyShouldNeverBeUsed();
		}

		/// <summary>
		/// Invoke an Task on an independent Realm thread.
		/// </summary>
		/// <returns>Task</returns>
		/// <param name="func">Func.</param>
		public Task InvokeAsync(Func<Realms.Realm, Task> func)
		{
			PCLHelpers.ThrowProxyShouldNeverBeUsed();
			return Task.FromResult(false);
		}

		/// <summary>
		/// Releases all resource used by the <see cref="T:SushiHangover.RealmThread"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="T:SushiHangover.RealmThread"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="T:SushiHangover.RealmThread"/> in an unusable state. After
		/// calling <see cref="Dispose"/>, you must release all references to the <see cref="T:SushiHangover.RealmThread"/> so
		/// the garbage collector can reclaim the memory that the <see cref="T:SushiHangover.RealmThread"/> was occupying.</remarks>
		public void Dispose()
		{
			PCLHelpers.ThrowProxyShouldNeverBeUsed();
		}
	}
}
