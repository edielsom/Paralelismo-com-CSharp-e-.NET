using System;
using System.Threading;
using System.Threading.Tasks;

namespace ByteBank.View.Ultis
{
    public class ByteBankProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;
        private readonly TaskScheduler _taskScheduler;

        // Construtor que recebe uma Action, pois a Thread não retorna valor
        public ByteBankProgress(Action<T> handler)
        {
            //Recupera o TaskScheduler da thread principal.
            _taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            //Acão
            _handler = handler;
        }
        public void Report(T value)
        {
            Task.Factory.StartNew(
                () => _handler(value),
                CancellationToken.None,
                TaskCreationOptions.None,
                _taskScheduler
                );
        }
    }
}
