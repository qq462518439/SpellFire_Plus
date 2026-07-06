using SpellFire.RobotManager.Core;

namespace SpellFire.RobotManager.Testing
{
    public sealed class NoopProduct : IProduct, IProductFaultSink
    {
        public NoopProduct()
            : this("NoopProduct")
        {
        }

        public NoopProduct(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "NoopProduct" : name;
            State = ProductState.Created;
        }

        public string Name { get; }

        public ProductState State { get; private set; }

        public int PulseCount { get; private set; }

        public long LastPulseUtcTicks { get; private set; }

        public void Start(ProductContext context)
        {
            State = ProductState.Running;
        }

        public void Pulse(ProductContext context)
        {
            PulseCount++;
            LastPulseUtcTicks = System.DateTime.UtcNow.Ticks;
        }

        public void Pause(ProductContext context)
        {
            State = ProductState.Paused;
        }

        public void Resume(ProductContext context)
        {
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
    }
}
