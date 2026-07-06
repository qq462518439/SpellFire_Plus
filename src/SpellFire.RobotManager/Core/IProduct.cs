namespace SpellFire.RobotManager.Core
{
    public interface IProduct
    {
        string Name { get; }

        ProductState State { get; }

        void Start(ProductContext context);

        void Pulse(ProductContext context);

        void Pause(ProductContext context);

        void Resume(ProductContext context);

        void Stop(ProductContext context);
    }
}
