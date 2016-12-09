using System;

namespace SushiHangover
{
	/// <summary>
	/// Realm thread exception.
	/// </summary>
	class RealmThreadException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:SushiHangover.RealmThreadException"/> class.
		/// </summary>
		public RealmThreadException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SushiHangover.RealmThreadException"/> class.
		/// </summary>
		/// <param name="message">Message.</param>
		public RealmThreadException(string message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SushiHangover.RealmThreadException"/> class.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="innerException">Inner exception.</param>
		public RealmThreadException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}