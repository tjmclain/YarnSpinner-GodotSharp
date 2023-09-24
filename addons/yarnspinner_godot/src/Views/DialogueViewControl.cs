using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotSharp.Views
{
	public partial class DialogueViewControl : Control
	{
		protected CancellationTokenSource CancellationTokenSource { get; set; } = null;

		protected CancellationToken GetCancellationToken()
		{
			if (CancellationTokenSource == null)
			{
				GD.PushError("CancellationTokenSource == null");
				return CancellationToken.None;
			}

			return CancellationTokenSource.Token;
		}

		protected virtual async Task WaitForCancellation()
		{
			var token = GetCancellationToken();
			if (!token.CanBeCanceled)
			{
				return;
			}

			await new CancellationTokenAwaiter(token);
		}

		protected virtual void CancelTokenSource()
		{
			if (CancellationTokenSource == null)
			{
				return;
			}

			CancellationTokenSource.Cancel();
		}

		protected virtual void CancelAndDisposeTokenSource()
		{
			if (CancellationTokenSource == null)
			{
				return;
			}

			CancellationTokenSource.Cancel();
			CancellationTokenSource.Dispose();
			CancellationTokenSource = null;
		}
	}
}
