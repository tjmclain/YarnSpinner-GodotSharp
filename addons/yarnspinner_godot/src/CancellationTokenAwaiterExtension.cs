using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Yarn.GodotSharp
{
	// https://medium.com/@cilliemalan/how-to-await-a-cancellation-token-in-c-cbfc88f28fa2
	public readonly struct CancellationTokenAwaiter : INotifyCompletion, ICriticalNotifyCompletion
	{
		public CancellationTokenAwaiter(CancellationToken cancellationToken)
		{
			_cancellationToken = cancellationToken;
		}

		private readonly CancellationToken _cancellationToken;

		public readonly object GetResult()
		{
			// this is called by compiler generated methods when the task has completed. Instead of
			// returning a result, we just throw an exception.
			if (IsCompleted)
			{
				throw new OperationCanceledException();
			}
			else
			{
				throw new InvalidOperationException("The cancellation token has not yet been cancelled.");
			}
		}

		// called by compiler generated/.net internals to check if the task has completed.
		public readonly bool IsCompleted => _cancellationToken.IsCancellationRequested;

		// The compiler will generate stuff that hooks in here. We hook those methods directly into
		// the cancellation token.
		public readonly void OnCompleted(Action continuation) =>
			_cancellationToken.Register(continuation);

		public readonly void UnsafeOnCompleted(Action continuation) =>
			_cancellationToken.Register(continuation);

		public CancellationTokenAwaiter GetAwaiter()
		{
			return this;
		}
	}
}
