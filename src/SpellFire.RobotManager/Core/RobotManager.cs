using System;
using SpellFire.WowRuntime.Core;

namespace SpellFire.RobotManager.Core
{
    public sealed class RobotManager : IRobotManager
    {
        private IProduct currentProduct;
        private ProductContext currentContext;

        public IProduct CurrentProduct
        {
            get { return currentProduct; }
        }

        public ProductState State
        {
            get { return currentProduct == null ? ProductState.Stopped : currentProduct.State; }
        }

        public RobotManagerResult Start(IWowRuntime runtime, IProduct product)
        {
            if (runtime == null)
            {
                return RobotManagerResult.Fail(RobotManagerStatus.InvalidRuntime, string.Empty, "Runtime must not be null.");
            }

            if (product == null)
            {
                return RobotManagerResult.Fail(RobotManagerStatus.InvalidProduct, string.Empty, "Product must not be null.");
            }

            if (currentProduct != null)
            {
                return RobotManagerResult.Fail(RobotManagerStatus.ProductAlreadyRunning, currentProduct.Name, "A product is already loaded.");
            }

            currentContext = new ProductContext(runtime);
            currentProduct = product;
            return InvokeProduct("start", () => product.Start(currentContext));
        }

        public RobotManagerResult PulseOnce()
        {
            if (currentProduct == null)
            {
                return RobotManagerResult.Fail(RobotManagerStatus.NoProduct, string.Empty, "No product is loaded.");
            }

            if (currentProduct.State == ProductState.Paused)
            {
                return RobotManagerResult.Fail(RobotManagerStatus.Paused, currentProduct.Name, "Product is paused.");
            }

            if (currentProduct.State == ProductState.Stopped)
            {
                return RobotManagerResult.Fail(RobotManagerStatus.Stopped, currentProduct.Name, "Product is stopped.");
            }

            if (currentProduct.State == ProductState.Faulted)
            {
                return RobotManagerResult.Fail(RobotManagerStatus.Faulted, currentProduct.Name, "Product is faulted.");
            }

            return InvokeProduct("pulse", () => currentProduct.Pulse(currentContext));
        }

        public RobotManagerResult Pause()
        {
            if (currentProduct == null)
            {
                return RobotManagerResult.Fail(RobotManagerStatus.NoProduct, string.Empty, "No product is loaded.");
            }

            if (currentProduct.State == ProductState.Faulted)
            {
                return RobotManagerResult.Fail(RobotManagerStatus.Faulted, currentProduct.Name, "Product is faulted.");
            }

            return InvokeProduct("pause", () => currentProduct.Pause(currentContext));
        }

        public RobotManagerResult Resume()
        {
            if (currentProduct == null)
            {
                return RobotManagerResult.Fail(RobotManagerStatus.NoProduct, string.Empty, "No product is loaded.");
            }

            if (currentProduct.State == ProductState.Faulted)
            {
                return RobotManagerResult.Fail(RobotManagerStatus.Faulted, currentProduct.Name, "Product is faulted.");
            }

            return InvokeProduct("resume", () => currentProduct.Resume(currentContext));
        }

        public RobotManagerResult Stop()
        {
            if (currentProduct == null)
            {
                return RobotManagerResult.Fail(RobotManagerStatus.NoProduct, string.Empty, "No product is loaded.");
            }

            RobotManagerResult result = InvokeProduct("stop", () => currentProduct.Stop(currentContext));
            currentProduct = null;
            currentContext = null;
            return result;
        }

        private RobotManagerResult InvokeProduct(string operation, Action action)
        {
            try
            {
                action();
                return RobotManagerResult.Ok(RobotManagerStatus.Ready, currentProduct.Name, operation);
            }
            catch (Exception ex)
            {
                var faultable = currentProduct as IProductFaultSink;
                if (faultable != null)
                {
                    faultable.MarkFaulted();
                }

                return RobotManagerResult.Fail(RobotManagerStatus.ProductError, currentProduct == null ? string.Empty : currentProduct.Name, ex.GetType().Name + ": " + ex.Message);
            }
        }
    }
}
