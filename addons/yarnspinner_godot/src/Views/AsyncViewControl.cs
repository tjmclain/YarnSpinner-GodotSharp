using System;
using System.Collections.Generic;
using System.Data;
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
		protected CancellationTokenSource InternalTokenSource { get; set; } = null;

		protected bool IsCancellationRequested
			=> InternalTokenSource != null && InternalTokenSource.IsCancellationRequested;

		#region Godot.Control

		protected override void Dispose(bool disposing)
		{
			SafeDisposeInternalTokenSource();
			base.Dispose(disposing);
		}

		#endregion Godot.Control

		protected virtual CancellationToken GetInternalCancellationToken()
		{
			InternalTokenSource ??= new CancellationTokenSource();
			return InternalTokenSource.Token;
		}

		protected static async Task WaitForCancellation(CancellationToken token)
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

			GD.Print("AsyncViewControl.WaitForCancellation: Completed");
		}

		protected virtual CancellationTokenSource CreateLinkedTokenSource(CancellationToken otherToken)
		{
			return CancellationTokenSource.CreateLinkedTokenSource(
				GetInternalCancellationToken(),
				otherToken
			);
		}

		protected virtual async Task WaitForInternalCancellation()
		{
			var token = GetInternalCancellationToken();
			await WaitForCancellation(token);
		}

		protected virtual bool SafeCancelInternalTokenSource()
		{
			if (InternalTokenSource == null)
			{
				return false;
			}

			if (InternalTokenSource.IsCancellationRequested)
			{
				return false;
			}

			InternalTokenSource.Cancel();
			return true;
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

		// https://medium.com/@cilliemalan/how-to-await-a-cancellation-token-in-c-cbfc88f28fa2
		protected readonly struct CancellationTokenAwaiter : INotifyCompletion, ICriticalNotifyCompletion
		{
			private readonly CancellationToken _token;

			public CancellationTokenAwaiter(CancellationToken cancellationToken)
			{
				_token = cancellationToken;
			}

			// called by compiler generated/.net internals to check if the task has completed.
			public readonly bool IsCompleted => _token.IsCancellationRequested;

			public readonly object GetResult()
			{
				// simply return the value of IsCompleted. Don't throw an exception because I don't
				// want to mandate 'try / catch' statements around calls that 'await' this object.
				return IsCompleted;
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
		}
	}
}
