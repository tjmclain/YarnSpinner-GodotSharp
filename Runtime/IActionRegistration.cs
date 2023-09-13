using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;

namespace Yarn.Godot
{
	public interface IActionRegistration
	{
		/// <summary>
		/// Adds a command handler. Dialogue will pause execution after the
		/// command is called.
		/// </summary>
		/// <remarks>
		/// <para>When this command handler has been added, it can be called
		/// from your Yarn scripts like so:</para>
		///
		/// <code lang="yarn">
		/// &lt;&lt;commandName param1 param2&gt;&gt;
		/// </code>
		///
		/// <para>If <paramref name="handler"/> is a method that returns a <see
		/// cref="Task"/>, when the command is run, the <see
		/// cref="DialogueRunner"/> will wait for the returned coroutine to stop
		/// before delivering any more content.</para>
		/// </remarks>
		/// <param name="commandName">The name of the command.</param>
		/// <param name="handler">The <see cref="CommandHandler"/> that will be
		/// invoked when the command is called.</param>
		void AddCommandHandler(string commandName, Delegate handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		/// <param name="methodInfo">The method that will be invoked when the
		/// command is called.</param>
		void AddCommandHandler(string commandName, MethodInfo methodInfo);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		void AddCommandHandler(string commandName, Func<Task> handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		void AddCommandHandler<T1>(string commandName, Func<T1, Task> handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		void AddCommandHandler<T1, T2>(string commandName, Func<T1, T2, Task> handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		void AddCommandHandler<T1, T2, T3>(string commandName, Func<T1, T2, T3, Task> handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		void AddCommandHandler<T1, T2, T3, T4>(string commandName, Func<T1, T2, T3, T4, Task> handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, Func<T1, T2, T3, T4, T5, Task> handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, Func<T1, T2, T3, T4, T5, T6, Task> handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		void AddCommandHandler(string commandName, Action handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		void AddCommandHandler<T1>(string commandName, Action<T1> handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		void AddCommandHandler<T1, T2>(string commandName, Action<T1, T2> handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		void AddCommandHandler<T1, T2, T3>(string commandName, Action<T1, T2, T3> handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		void AddCommandHandler<T1, T2, T3, T4>(string commandName, Action<T1, T2, T3, T4> handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, Action<T1, T2, T3, T4, T5> handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, Action<T1, T2, T3, T4, T5, T6> handler);

		/// <summary>
		/// Removes a command handler.
		/// </summary>
		/// <param name="commandName">The name of the command to
		/// remove.</param>
		void RemoveCommandHandler(string commandName);

		/// <summary>
		/// Add a new function that returns a value, so that it can be
		/// called from Yarn scripts.
		/// </summary>
		/// <remarks>
		/// <para>When this function has been registered, it can be called from
		/// your Yarn scripts like so:</para>
		///
		/// <code lang="yarn">
		/// &lt;&lt;if myFunction(1, 2) == true&gt;&gt;
		///     myFunction returned true!
		/// &lt;&lt;endif&gt;&gt;
		/// </code>
		///
		/// <para>The <c>call</c> command can also be used to invoke the function:</para>
		///
		/// <code lang="yarn">
		/// &lt;&lt;call myFunction(1, 2)&gt;&gt;
		/// </code>
		/// </remarks>
		/// <param name="implementation">The <see cref="Delegate"/> that
		/// should be invoked when this function is called.</param>
		/// <seealso cref="Library"/>
		void AddFunction(string name, Delegate implementation);

		/// <inheritdoc cref="AddFunction(string, Delegate)" />
		/// <typeparam name="TResult">The type of the value that the function should return.</typeparam>
		void AddFunction<TResult>(string name, Func<TResult> implementation);

		/// <inheritdoc cref="AddFunction{TResult}(string, Func{TResult})" />
		/// <typeparam name="T1">The type of the first parameter to the function.</typeparam>
		void AddFunction<T1, TResult>(string name, Func<T1, TResult> implementation);

		/// <inheritdoc cref="AddFunction{T1,TResult}(string, Func{T1,TResult})" />
		/// <typeparam name="T2">The type of the second parameter to the function.</typeparam>
		void AddFunction<T1, T2, TResult>(string name, Func<T1, T2, TResult> implementation);

		/// <inheritdoc cref="AddFunction{T1,T2,TResult}(string, Func{T1,T2,TResult})" />
		/// <typeparam name="T3">The type of the third parameter to the function.</typeparam>
		void AddFunction<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> implementation);

		/// <inheritdoc cref="AddFunction{T1,T2,T3,TResult}(string, Func{T1,T2,T3,TResult})" />
		/// <typeparam name="T4">The type of the fourth parameter to the function.</typeparam>
		void AddFunction<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> implementation);

		/// <inheritdoc cref="AddFunction{T1,T2,T3,T4,TResult}(string, Func{T1,T2,T3,T4,TResult})" />
		/// <typeparam name="T5">The type of the fifth parameter to the function.</typeparam>
		void AddFunction<T1, T2, T3, T4, T5, TResult>(string name, Func<T1, T2, T3, T4, T5, TResult> implementation);

		/// <inheritdoc cref="AddFunction{T1,T2,T3,T4,T5,TResult}(string, Func{T1,T2,T3,T4,T5,TResult})" />
		/// <typeparam name="T6">The type of the sixth parameter to the function.</typeparam>
		void AddFunction<T1, T2, T3, T4, T5, T6, TResult>(string name, Func<T1, T2, T3, T4, T5, T6, TResult> implementation);

		/// <summary>
		/// Remove a registered function.
		/// </summary>
		/// <remarks>
		/// After a function has been removed, it cannot be called from
		/// Yarn scripts.
		/// </remarks>
		/// <param name="name">The name of the function to remove.</param>
		/// <seealso cref="AddFunction{TResult}(string, Func{TResult})"/>
		void RemoveFunction(string name);
	}
}