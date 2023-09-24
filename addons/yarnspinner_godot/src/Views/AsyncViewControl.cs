using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotSharp.Views
{
	[GlobalClass]
	public partial class AsyncViewControl : Control
	{
		#region Properties

		protected CancellationTokenSource InternalTokenSource { get; set; } = null;

		#endregion Properties

		#region Protected Methods

		protected virtual CancellationToken GetInternalCancellationToken()
		{
			InternalTokenSource ??= new CancellationTokenSource();
			return InternalTokenSource.Token;
		}

		protected virtual CancellationTokenSource CreateLinkedTokenSource(CancellationToken otherToken)
		{
			return CancellationTokenSource.CreateLinkedTokenSource(
				GetInternalCancellationToken(),
				otherToken
			);
		}

		protected virtual async Task WaitForCancellation()
		{
			var token = GetInternalCancellationToken();
			await WaitForCancellation(token);
		}

		protected virtual async Task WaitForCancellation(CancellationToken token)
		{
			if (!token.CanBeCanceled)
			{
				return;
			}

			if (token.IsCancellationRequested)
			{
				return;
			}

			await new CancellationTokenAwaiter(token);
		}

		protected virtual void SafeCancelInternalTokenSource()
		{
			if (InternalTokenSource == null)
			{
				return;
			}

			InternalTokenSource.Cancel();
		}

		protected virtual void SafeDisposeInternalTokenSource()
		{
			if (InternalTokenSource == null)
			{
				return;
			}

			InternalTokenSource.Dispose();
			InternalTokenSource = null;
		}

		#endregion Protected Methods

		#region Structs

		// https://medium.com/@cilliemalan/how-to-await-a-cancellation-token-in-c-cbfc88f28fa2
		protected readonly struct CancellationTokenAwaiter : INotifyCompletion, ICriticalNotifyCompletion
		{
			#region Fields

			private readonly CancellationToken _token;

			#endregion Fields

			#region Public Constructors

			public CancellationTokenAwaiter(CancellationToken cancellationToken)
			{
				_token = cancellationToken;
			}

			#endregion Public Constructors

			#region Properties

			// called by compiler generated/.net internals to check if the task has completed.
			public readonly bool IsCompleted => _token.IsCancellationRequested;

			#endregion Properties

			#region Public Methods

			public readonly object GetResult()
			{
				// this is called by compiler generated methods when the task has completed. Instead
				// of returning a result, we just throw an exception.
				if (IsCompleted)
				{
					throw new OperationCanceledException();
				}
				else
				{
					throw new InvalidOperationException("The cancellation token has not yet been cancelled.");
				}
			}

			// The compiler will generate stuff that hooks in here. We hook those methods directly
			// into the cancellation token.
			public readonly void OnCompleted(Action continuation) =>
				_token.Register(continuation);

			public readonly void UnsafeOnCompleted(Action continuation) =>
				_token.Register(continuation);

			public CancellationTokenAwaiter GetAwaiter()
			{
				return this;
			}

			#endregion Public Methods
		}

		#endregion Structs
	}
}
