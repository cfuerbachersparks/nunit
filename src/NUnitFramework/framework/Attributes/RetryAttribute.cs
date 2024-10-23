// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace NUnit.Framework
{
    /// <summary>
    /// Specifies that a test method should be rerun on failure up to the specified
    /// maximum number of times.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class RetryAttribute : NUnitAttribute, IRepeatTest
    {
        private readonly int _tryCount;
        private readonly bool _stopOnSuccess;

        /// <summary>
        /// Construct a <see cref="RetryAttribute" />
        /// </summary>
        /// <param name="tryCount">The maximum number of times the test should be run if it fails</param>
        /// <param name="stopOnSuccess">Whether to stop when a test passes or not</param>
        public RetryAttribute(int tryCount, bool stopOnSuccess = true)
        {
            _tryCount = tryCount;
            _stopOnSuccess = stopOnSuccess;
        }

        #region IRepeatTest Members

        /// <summary>
        /// Wrap a command and return the result.
        /// </summary>
        /// <param name="command">The command to be wrapped</param>
        /// <returns>The wrapped command</returns>
        public TestCommand Wrap(TestCommand command)
        {
            return new RetryCommand(command, _tryCount, _stopOnSuccess);
        }

        #endregion

        #region Nested RetryCommand Class

        /// <summary>
        /// The test command for the <see cref="RetryAttribute"/>
        /// </summary>
        public class RetryCommand : DelegatingTestCommand
        {
            private readonly int _tryCount;
            private readonly bool _stopOnSuccess;

            /// <summary>
            /// Initializes a new instance of the <see cref="RetryCommand"/> class.
            /// </summary>
            /// <param name="innerCommand">The inner command.</param>
            /// <param name="tryCount">The maximum number of repetitions</param>
            /// <param name="stopOnSuccess">Whether to stop when a test passes or not</param>
            public RetryCommand(TestCommand innerCommand, int tryCount, bool stopOnSuccess)
                : base(innerCommand)
            {
                _tryCount = tryCount;
                _stopOnSuccess = stopOnSuccess;
            }

            /// <summary>
            /// Runs the test, saving a TestResult in the supplied TestExecutionContext.
            /// </summary>
            /// <param name="context">The context in which the test should run.</param>
            /// <returns>A TestResult</returns>
            public override TestResult Execute(TestExecutionContext context)
            {
                int count = _tryCount;

                while (count-- > 0)
                {
                    try
                    {
                        context.CurrentResult = innerCommand.Execute(context);
                    }
                    // Commands are supposed to catch exceptions, but some don't
                    // and we want to look at restructuring the API in the future.
                    catch (Exception ex)
                    {
                        if (context.CurrentResult is null)
                            context.CurrentResult = context.CurrentTest.MakeTestResult();
                        context.CurrentResult.RecordException(ex);
                    }

                    if (_stopOnSuccess && context.CurrentResult.ResultState != ResultState.Failure)
                        break;

                    // Clear result for retry
                    if (count > 0)
                    {
                        context.CurrentResult = context.CurrentTest.MakeTestResult();
                        context.CurrentRepeatCount++; // increment Retry count for next iteration. will only happen if we are guaranteed another iteration
                    }
                }

                return context.CurrentResult;
            }
        }

        #endregion
    }
}
