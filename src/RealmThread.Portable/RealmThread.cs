using System;
using System.Threading.Tasks;

namespace SushiHangover
{
	/// <summary>
	/// A Realm Action Pump
	/// </summary>
	public class RealmThread : IDisposable
	{
		/// <summary>
		/// Gets the managed thread identifier.
		/// </summary>
		/// <value>The managed thread identifier.</value>
		int ManagedThreadId
		{
			get 
			{ 
				PCLHelpers.ThrowProxyShouldNeverBeUsed();
				return int.MinValue;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:SushiHangover.RealmThread"/> is in transaction.
		/// </summary>
		/// <value><c>true</c> if in transaction; otherwise, <c>false</c>.</value>
		public bool InTransaction
		{
			get { 
				PCLHelpers.ThrowProxyShouldNeverBeUsed();
				return false;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SushiHangover.RealmThread"/> class.
		/// </summary>
		/// <param name="realmConfig">RealmConfiguration</param>
		public RealmThread(Realms.RealmConfiguration realmConfig) : this(realmConfig, false)
		{
			PCLHelpers.ThrowProxyShouldNeverBeUsed();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SushiHangover.RealmThread"/> class.
		/// </summary>
		/// <param name="realmConfig">RealmConfiguration</param>
		/// <param name="autoCommmit">If set to <c>true</c> auto commmit open transaction on Dispose</param>
		public RealmThread(Realms.RealmConfiguration realmConfig, bool autoCommmit)
		{
			PCLHelpers.ThrowProxyShouldNeverBeUsed();
		}

		/// <summary>
		/// Invokes a "Fire & Forget" Action on a dedicated Realm thread.
		/// </summary>
		/// <param name="action">Action.</param>
		public void BeginInvoke(Action<Realms.Realm> action)
		{
			PCLHelpers.ThrowProxyShouldNeverBeUsed();
		}

		/// <summary>
		/// Invoke the specified action on a dedicated Realm thread.
		/// </summary>
		/// <param name="action">Action.</param>
		public void Invoke(Action<Realms.Realm> action)
		{
			PCLHelpers.ThrowProxyShouldNeverBeUsed();
		}

		/// <summary>
		/// Invoke an Task on a dedicated Realm thread.
		/// </summary>
		/// <returns>Task</returns>
		/// <param name="func">Func.</param>
		public Task InvokeAsync(Func<Realms.Realm, Task> func)
		{
			PCLHelpers.ThrowProxyShouldNeverBeUsed();
			return Task.FromResult(false);
		}

		/// <summary>
		/// Begin a Realm transaction on this thread.
		/// </summary>
		public void BeginTransaction()
		{
			PCLHelpers.ThrowProxyShouldNeverBeUsed();
		}

		/// <summary>
		/// Commits the Realm transaction that is active on this thread.
		/// </summary>
		public void CommitTransaction()
		{
			PCLHelpers.ThrowProxyShouldNeverBeUsed();
		}

		/// <summary>
		/// Rollbacks the Ream transaction that is active on this thread.
		/// </summary>
		public void RollbackTransaction()
		{
			PCLHelpers.ThrowProxyShouldNeverBeUsed();
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
