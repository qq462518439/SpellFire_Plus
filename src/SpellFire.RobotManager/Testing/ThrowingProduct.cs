using System;
using SpellFire.RobotManager.Core;

namespace SpellFire.RobotManager.Testing
{
    public sealed class ThrowingProduct : IProduct, IProductFaultSink
    {
        private readonly string throwOnOperation;

        public ThrowingProduct(string throwOnOperation)
        {
            this.throwOnOperation = string.IsNullOrWhiteSpace(throwOnOperation) ? "pulse" : throwOnOperation;
            Name = "ThrowingProduct";
            State = ProductState.Created;
        }

        public string Name { get; }

        public ProductState State { get; private set; }

        public void Start(ProductContext context)
        {
            ThrowIf("start");
            State = ProductState.Running;
        }

        public void Pulse(ProductContext context)
        {
            ThrowIf("pulse");
        }

        public void Pause(ProductContext context)
        {
            ThrowIf("pause");
            State = ProductState.Paused;
        }

        public void Resume(ProductContext context)
        {
            ThrowIf("resume");
            State = ProductState.Running;
        }

        public void Stop(ProductContext context)
        {
            State = ProductState.Stopped;
        }

        public void MarkFaulted()
        {
            State = ProductState.Faulted;
        }

        private void ThrowIf(string operation)
        {
            if (string.Equals(throwOnOperation, operation, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Synthetic " + operation + " failure.");
            }
        }
    }
}
