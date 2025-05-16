namespace WOWCAM.Core.Parts.Logic.System
{
    public sealed class DefaultReliableFileOperations : IReliableFileOperations
    {
        // This central business logic helper is used for many internal file operations, across the business logic, to improve their reliability.
        // All the .NET I/O file operations (code) are done for sure, but the hardware buffers (or virus scan, or whatever) may not finished yet.
        // Therefore give those operations some time to finish their business. There is no better way, since this is not under the app's control.

        private const int DelayInMilliseconds = 250;

        public Task WaitAsync(CancellationToken cancellationToken = default)
        {
            return Task.Delay(DelayInMilliseconds, cancellationToken);
        }

        public async Task WaitBeforeAsync(Action fileOperations, CancellationToken cancellationToken = default)
        {
            await Task.Delay(DelayInMilliseconds, cancellationToken).ConfigureAwait(false);

            fileOperations();
        }

        public Task WaitAfterAsync(Action fileOperations, CancellationToken cancellationToken = default)
        {
            fileOperations();

            return Task.Delay(DelayInMilliseconds, cancellationToken);
        }
    }
}
